using System.Text.Json;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using RPGBattleMaker.Application.Interfaces;
using RPGBattleMaker.Domain.Entities;

namespace RPGBattleMaker.Infrastructure.AI;

public sealed class LocalBattleNarrator : IBattleNarrator, IDisposable
{
    private const string ModelFileName = "Llama-3.2-3B-Instruct-Q4_K_M.gguf";

    private readonly SemaphoreSlim _generationLock = new(1, 1);
    private LLamaWeights? _model;
    private string? _loadedModelPath;
    private bool _initializationAttempted;

    public async Task<BattleStory?> GenerateAsync(BattleNarrativeContext context)
    {
        await _generationLock.WaitAsync();

        try
        {
            if (!await EnsureModelLoadedAsync())
                return null;

            string prompt = BuildPrompt(context);

            using CancellationTokenSource firstTimeout =
                new(TimeSpan.FromSeconds(15));

            string? response = await GenerateTextAsync(
                prompt,
                firstTimeout.Token);

            WriteDiagnostic("first-response", response);

            BattleStory? story = ParseResponse(response);
            if (story is not null)
                return story;

            using CancellationTokenSource retryTimeout =
                new(TimeSpan.FromSeconds(15));

            response = await GenerateTextAsync(
                prompt +
                "\n\nATENÇÃO: a resposta anterior foi inválida. " +
                "Responda agora SOMENTE com JSON válido exatamente neste formato: " +
                "{\"title\":\"título curto\",\"narrative\":\"2 ou 3 frases\"}.",
                retryTimeout.Token);

            WriteDiagnostic("retry-response", response);

            return ParseResponse(response);
        }
        catch (Exception exception)
        {
            WriteDiagnostic("exception", exception.ToString());
            return null;
        }
        finally
        {
            _generationLock.Release();
        }
    }

    private async Task<bool> EnsureModelLoadedAsync()
    {
        if (_model is not null)
            return true;

        if (_initializationAttempted)
            return false;

        _initializationAttempted = true;

        string configuredPath =
            Environment.GetEnvironmentVariable("RPGBATTLE_LLM_MODEL") ?? string.Empty;

        string modelPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "Models", ModelFileName)
            : configuredPath;

        if (!File.Exists(modelPath))
            return false;

        try
        {
            _loadedModelPath = modelPath;

            NativeLibraryConfig.All
                .WithVulkan()
                .WithAutoFallback(false);

            ModelParams parameters = new(modelPath)
            {
                ContextSize = 1024,
                GpuLayerCount = 99
            };

            _model = await Task.Run(() => LLamaWeights.LoadFromFile(parameters));
            return true;
        }
        catch (Exception exception)
        {
            WriteDiagnostic("model-load-exception", exception.ToString());
            _loadedModelPath = null;
            _model?.Dispose();
            _model = null;
            return false;
        }
    }

    private async Task<string?> GenerateTextAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        if (_model is null || _loadedModelPath is null)
            return null;

        try
        {
            ModelParams parameters = new(_loadedModelPath)
            {
                ContextSize = 1024,
                GpuLayerCount = 99
            };

            return await Task.Run(async () =>
            {
                using LLamaContext context = _model.CreateContext(parameters);
                InteractiveExecutor executor = new(context);

                string modelPrompt =
                    "<|start_header_id|>system<|end_header_id|>\n" +
                    "Você é o narrador de combate de um roguelike medieval chamado RPG Battle Maker.\n" +
                    "Sua função é continuar uma única história de batalha, mantendo continuidade entre as rodadas.\n" +
                    "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional.\n" +
                    "O JSON deve conter exatamente duas propriedades: title e narrative.\n" +
                    "As duas propriedades devem ter APENAS strings como valores.\n" +
                    "title deve ser curto. narrative deve ter de 2 a 3 frases.\n" +
                    "Não invente números, dano, cura, ouro, DC, XP ou resultados diferentes dos informados no estado.\n" +
                    "Se houve falha, descreva claramente que o herói ou grupo foi prejudicado, sem inventar um valor de dano.\n" +
                    "Se houve crítico, destaque o impacto extraordinário.\n" +
                    "A narrativa deve continuar naturalmente a partir da história anterior.\n" +
                    "Escreva tudo em português do Brasil.\n" +
                    "<|eot_id|>" +
                    "<|start_header_id|>user<|end_header_id|>\n" +
                    prompt +
                    "\nRetorne somente o JSON completo.\n" +
                    "<|eot_id|>" +
                    "<|start_header_id|>assistant<|end_header_id|>\n";

                InferenceParams inferenceParams = new()
                {
                    MaxTokens = 150,
                    SamplingPipeline = new DefaultSamplingPipeline
                    {
                        Temperature = 0.35f,
                        TopP = 0.9f,
                        Seed = (uint)Random.Shared.Next()
                    }
                };

                List<string> chunks = new();

                await foreach (string text in executor.InferAsync(
                    modelPrompt,
                    inferenceParams,
                    cancellationToken))
                {
                    chunks.Add(text);
                }

                return string.Concat(chunks);
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            WriteDiagnostic("inference-exception", exception.ToString());
            return null;
        }
    }

    private static string BuildPrompt(BattleNarrativeContext context)
    {
        string teamText = context.Team.Count == 0
            ? "Nenhum herói restante."
            : string.Join(
                Environment.NewLine,
                context.Team.Select(agent =>
                    $"- {agent.Name} | classe: {agent.Type} | HP: {agent.CurrentLife}/{agent.MaxLife}"));

        string itemText = context.Items.Count == 0
            ? "Nenhuma relíquia."
            : string.Join(
                Environment.NewLine,
                context.Items.Select(item => $"- {item.Name}"));

        string previousText = context.PreviousStories.Count == 0
            ? "Esta é a abertura da batalha; ainda não existe história anterior."
            : string.Join(
                Environment.NewLine,
                context.PreviousStories.TakeLast(5).Select((story, index) =>
                    $"{index + 1}. {story}"));

        if (context.IsInitial)
        {
            return $"""
                INÍCIO DA BATALHA

                Andar: {context.Level}/5
                Tema do teste: {context.Theme}
                Herói em destaque: {context.Hero.Name} ({context.Hero.Type})
                Equipe:
                {teamText}

                Relíquias:
                {itemText}

                História anterior:
                {previousText}

                Crie a cena inicial da batalha. Apresente o ambiente, a ameaça e o objetivo imediato do grupo.
                Termine a narrativa de modo que o jogador esteja pronto para rolar o primeiro dado.
                """;
        }

        return $"""
            CONTINUAÇÃO DA BATALHA

            Andar: {context.Level}/5
            Rodada: {context.Round}/5
            Tema do teste: {context.Theme}
            Herói em destaque: {context.Hero.Name} ({context.Hero.Type})
            D20: {context.D20}
            Total: {context.Total}
            DC: {context.Dc}
            Resultado mecânico: {context.Outcome}
            Consequência mecânica: {context.OutcomeDetail}

            Equipe atual:
            {teamText}

            Relíquias:
            {itemText}

            História anterior:
            {previousText}

            Continue exatamente essa mesma história.
            Mostre como a ação da rodada mudou a situação da batalha.
            Não crie uma nova missão ou um novo cenário sem motivo.
            """;
    }

    private static BattleStory? ParseResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        int start = response.IndexOf('{');
        int end = response.LastIndexOf('}');

        if (start < 0 || end <= start)
            return null;

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(response[start..(end + 1)]);

            JsonElement root = document.RootElement;

            string title = ReadJsonText(root, "title");
            string narrative = ReadJsonText(root, "narrative");

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(narrative))
            {
                return null;
            }

            return new BattleStory(
                Clean(title, 70),
                Clean(narrative, 500));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ReadJsonText(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value))
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    break;
                }
            }
        }

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (string preferred in new[] { "text", "description", "title" })
            {
                if (value.TryGetProperty(preferred, out JsonElement nested) &&
                    nested.ValueKind == JsonValueKind.String)
                {
                    return nested.GetString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private static string Clean(string value, int maxLength)
    {
        string cleaned = value
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        return cleaned.Length > maxLength
            ? cleaned[..maxLength].TrimEnd() + "..."
            : cleaned;
    }

    private static void WriteDiagnostic(string stage, string? content)
    {
        try
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "battle_ai_diagnostic.log");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            File.AppendAllText(
                path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {stage}\n" +
                $"{content ?? "<null>"}\n---\n");
        }
        catch
        {
            // Diagnóstico nunca pode interromper o jogo.
        }
    }

    public void Dispose()
    {
        _generationLock.Dispose();
        _model?.Dispose();
    }
}
