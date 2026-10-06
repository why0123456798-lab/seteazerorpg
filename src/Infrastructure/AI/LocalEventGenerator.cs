using System.Text.Json;
using System.Text.Json.Serialization;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using RPGBattleMaker.Application.Interfaces;
using RPGBattleMaker.Domain.Entities;

namespace RPGBattleMaker.Infrastructure.AI;

public sealed class LocalEventGenerator : ILocalEventGenerator, IDisposable
{
    private const string ModelFileName = "Llama-3.2-3B-Instruct-Q4_K_M.gguf";

    private readonly SemaphoreSlim _generationLock = new(1, 1);
    private LLamaWeights? _model;
    private string? _loadedModelPath;
    private bool _initializationAttempted;

    public async Task<Event?> GenerateAsync(
        Event mechanicsTemplate,
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        int level,
        int gold,
        IReadOnlyCollection<string> recentEvents)
    {
        await _generationLock.WaitAsync();

        try
        {
            if (!await EnsureModelLoadedAsync())
                return null;

            string prompt = BuildPrompt(team, items, level, gold, recentEvents);

            GeneratedEventResponse? generated = null;
            string? response = null;

            using (CancellationTokenSource firstTimeout =
                new(TimeSpan.FromSeconds(30)))
            {
                response = await GenerateTextAsync(
                    prompt,
                    firstTimeout.Token);
                generated = ParseResponse(response);
                WriteDiagnostic("primeira", response);
            }

            if (generated is null)
            {
                using CancellationTokenSource retryTimeout =
                    new(TimeSpan.FromSeconds(20));

                response = await GenerateTextAsync(
                    BuildPrompt(team, items, level, gold, recentEvents, true),
                    retryTimeout.Token);

                generated = ParseResponse(response);
                WriteDiagnostic("retry", response);
            }

            if (generated is null)
            {
                Console.Error.WriteLine("[LocalEventGenerator] segunda resposta inválida:");
                Console.Error.WriteLine(response ?? "<null>");
            }

            if (generated is null)
                return null;

            return new Event(
                mechanicsTemplate.Id,
                generated.Title,
                generated.Description,
                generated.OptionA,
                generated.OptionB,
                generated.OptionC);
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

        string configuredPath = Environment.GetEnvironmentVariable("RPGBATTLE_LLM_MODEL") ?? string.Empty;

        string modelPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(
                AppContext.BaseDirectory,
                "Models",
                ModelFileName)
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
        catch
        {
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
                    "Você é o bardo contando uma historia de um roguelike medieval chamado RPG Battle Maker.\n" +
                    "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional.\n" +
                    "O JSON deve conter exatamente estas cinco propriedades: title, description, optionA, optionB, optionC.\n" +
                    "IMPORTANTE: cada uma das cinco propriedades deve ter APENAS uma STRING como valor.\n" +
                    "Nunca coloque objetos, arrays, subopções ou propriedades dentro de optionA, optionB ou optionC.\n" +
                    "Cada opção deve ser uma única frase curta que descreve a ação do jogador.\n" +
                    "Escreva tudo em português do Brasil.\n" +
                    "Nunca invente recompensas, dano, cura, ouro, DC ou XP.\n" +
                    "<|eot_id|>" +
                    "<|start_header_id|>user<|end_header_id|>\n" +
                    prompt +
                    "\nRetorne somente o JSON completo.\n" +
                    "<|eot_id|>" +
                    "<|start_header_id|>assistant<|end_header_id|>\n";

            InferenceParams inferenceParams = new()
            {
                MaxTokens = 180,
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = 0.2f,
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
            Console.Error.WriteLine($"[LocalEventGenerator] {exception.Message}");
            WriteDiagnostic("inference-exception", exception.ToString());
            return null;
        }
    }

    private static void WriteDiagnostic(string stage, string? content)
    {
        try
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "ai_diagnostic.log");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            File.AppendAllText(
                path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {stage}\n{content ?? "<null>"}\n---\n");
        }
        catch
        {
            // Diagnóstico nunca pode derrubar a execução.
        }
    }

    private static string BuildPrompt(
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        int level,
        int gold,
        IReadOnlyCollection<string> recentEvents,
        bool strictRetry = false)
    {
        string teamText = team.Count == 0
            ? "Nenhum herói."
            : string.Join(
                Environment.NewLine,
                team.Select(agent =>
                    $"- {agent.Name} | classe: {agent.Type} | HP: {agent.CurrentLife}/{agent.MaxLife} | ATK: {agent.BaseAttack} | DEF: {agent.BaseDefense} | PER: {agent.BaseSkill}"));

        string itemText = items.Count == 0
            ? "Nenhuma relíquia."
            : string.Join(
                Environment.NewLine,
                items.Select(item => $"- {item.Name}"));

        string recentText = recentEvents.Count == 0
            ? "Nenhum evento recente."
            : string.Join(
                Environment.NewLine,
                recentEvents.TakeLast(8).Select(name => $"- {name}"));

        return $"""
            Gere um novo evento para o jogo.

            Estado da run:
            - Andar: {level}/5
            - Ouro: {gold}

            Esquadrão:
            {teamText}

            Relíquias:
            {itemText}

            Eventos recentes que devem ser evitados:
            {recentText}

            Estrutura mecânica das escolhas:
            - Escolha 1: ação física/ofensiva
            - Escolha 2: perícia/astúcia
            - Escolha 3: cautela/recuo

            Não copie textos de outros eventos.
            Não revele os resultados mecânicos das escolhas.
            Crie uma situação de fantasia medieval que combine com o estado atual da run.
            {(strictRetry ? "ATENÇÃO: responda exclusivamente com o objeto JSON, sem markdown e sem texto antes ou depois." : string.Empty)}
            """;
    }

    private static GeneratedEventResponse? ParseResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        int start = response.IndexOf('{');
        int end = response.LastIndexOf('}');

        if (start < 0 || end <= start)
            return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                response[start..(end + 1)]);

            JsonElement root = document.RootElement;

            string title = ReadJsonText(root, "title");
            string description = ReadJsonText(root, "description");
            string optionA = ReadJsonText(root, "optionA");
            string optionB = ReadJsonText(root, "optionB");
            string optionC = ReadJsonText(root, "optionC");

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(optionA) ||
                string.IsNullOrWhiteSpace(optionB) ||
                string.IsNullOrWhiteSpace(optionC))
            {
                return null;
            }

            return new GeneratedEventResponse
            {
                Title = Clean(title, 70),
                Description = Clean(description, 420),
                OptionA = Clean(optionA, 100),
                OptionB = Clean(optionB, 100),
                OptionC = Clean(optionC, 100)
            };
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
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
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
            foreach (string preferred in new[] { "action", "text", "title", "description" })
            {
                if (value.TryGetProperty(preferred, out JsonElement nested) &&
                    nested.ValueKind == JsonValueKind.String)
                {
                    return nested.GetString() ?? string.Empty;
                }
            }

            string[] parts = value.EnumerateObject()
                .Select(p => p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : null)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray()!;

            return string.Join(" — ", parts);
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return string.Join(
                " ",
                value.EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)
                    .Where(v => !string.IsNullOrWhiteSpace(v)));
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

    public void Dispose()
    {
        _generationLock.Dispose();
        _model?.Dispose();
    }

    private sealed class GeneratedEventResponse
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("optionA")]
        public string OptionA { get; set; } = string.Empty;

        [JsonPropertyName("optionB")]
        public string OptionB { get; set; } = string.Empty;

        [JsonPropertyName("optionC")]
        public string OptionC { get; set; } = string.Empty;
    }
}
