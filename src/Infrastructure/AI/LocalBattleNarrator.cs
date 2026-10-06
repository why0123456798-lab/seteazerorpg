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

    public async Task<BattleStory?> GenerateInitialAsync(
        int level,
        IReadOnlyCollection<string> previousStories)
    {
        string previousText = previousStories.Count == 0
            ? "Esta é a primeira cena da jornada. Ainda não existe história anterior."
            : string.Join(
                Environment.NewLine,
                previousStories.TakeLast(8).Select((story, index) =>
                    $"{index + 1}. {story}"));

        BattleStory? story = await GeneratePromptAsync($"""
            ABERTURA DO NÍVEL {level}

            Esta não é uma nova história. Continue a mesma jornada exatamente de onde a narrativa anterior terminou.
            Crie a cena introdutória do novo combate de um roguelike medieval.
            A cena deve preparar o jogador para rolar o primeiro D20 deste nível.
            Mantenha personagens, acontecimentos, ameaças e clima narrativo já estabelecidos sempre que fizer sentido.
            Não apague, reinicie ou contradiga a história anterior.
            Não invente uma nova missão separada.
            Termine com uma sensação clara de que o combate deste novo nível está prestes a começar.

            HISTÓRIA ANTERIOR:
            {previousText}

            Continue a partir do último acontecimento narrado.
            """);

        return story ?? new BattleStory(
            level == 1 ? "A Batalha se Aproxima" : "A Jornada Continua",
            level == 1
                ? "O ar pesa enquanto a estrada desaparece sob sombras e o silêncio da região anuncia perigo. Os heróis se preparam para agir, e o próximo lançamento do D20 decidirá o primeiro movimento da batalha."
                : "A jornada avança para um novo confronto, carregando as marcas dos acontecimentos anteriores. Os heróis se aproximam do próximo campo de batalha, onde uma nova rolagem decidirá o rumo da aventura.");
    }

    public async Task<BattleStory?> GenerateAsync(BattleNarrativeContext context)
    {
        BattleStory? story = await GeneratePromptAsync(BuildPrompt(context));

        return story ?? new BattleStory(
            $"A Batalha Continua",
            $"{context.Hero.Name} enfrenta as consequências da rodada marcada como {context.Outcome.ToLowerInvariant()}. A equipe sente a tensão aumentar enquanto o combate avança para o próximo momento decisivo.");
    }

    public async Task<BattleStory?> GenerateConclusionAsync(
        int level,
        bool success,
        int successes,
        int failures,
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        IReadOnlyCollection<string> previousStories,
        IReadOnlyCollection<string> recentEvents)
    {
        string previousText = previousStories.Count == 0
            ? "Nenhuma narrativa anterior."
            : string.Join(
                Environment.NewLine,
                previousStories.TakeLast(6).Select((story, index) =>
                    $"{index + 1}. {story}"));

        string eventsText = recentEvents.Count == 0
            ? "Nenhum evento relevante."
            : string.Join(
                Environment.NewLine,
                recentEvents.TakeLast(4).Select((eventStory, index) =>
                    $"{index + 1}. {eventStory}"));

        string teamText = team.Count == 0
            ? "A equipe não possui heróis vivos."
            : string.Join(
                Environment.NewLine,
                team.Select(agent =>
                    $"- {agent.Name} | classe: {agent.Type} | HP: {agent.CurrentLife}/{agent.MaxLife}"));

        string result = success
            ? $"""
              SUCESSO DO NÍVEL
              A equipe concluiu o nível com {successes} sucesso(s) e {failures} falha(s).
              A história deve terminar este capítulo como uma vitória da party.
              Mostre a equipe superando o conflito, sofrendo as consequências apropriadas,
              e encerrando este capítulo com uma sensação clara de conquista e progresso.
              Não trate isso como o fim da aventura inteira: deixe uma continuidade natural
              para o próximo nível.
              """
            : $"""
              DERROTA / GAME OVER
              A equipe fracassou no nível com {successes} sucesso(s) e {failures} falha(s).
              A história deve encerrar a aventura derrotando a party.
              Mostre as consequências do fracasso, a derrota dos heróis e o fim da jornada.
              Não transforme a derrota em vitória ou em um simples recuo.
              """;

        string prompt = $"""
            CONCLUSÃO NARRATIVA DO NÍVEL {level}

            Você está escrevendo a conclusão dramática do capítulo atual de uma única aventura.
            Não crie uma história nova. Continue exatamente o que já foi contado.

            {result}

            HISTÓRIA ANTERIOR:
            {previousText}

            EVENTOS DA JORNADA:
            {eventsText}

            ESTADO FINAL DA PARTY:
            {teamText}

            Escreva uma conclusão satisfatória, cinematográfica e coerente.
            Retome acontecimentos importantes já estabelecidos e dê sensação de fechamento.
            Em caso de sucesso, a party deve claramente vencer o conflito deste nível.
            Em caso de fracasso, a party deve claramente ser derrotada e a aventura terminar.
            Não invente números, recompensas ou mecânicas que não foram informadas.
            """;

        const string conclusionSystemPrompt =
            "Você é o narrador principal de uma aventura de roguelike medieval chamada RPG Battle Maker. " +
            "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional. " +
            "O JSON deve conter exatamente duas propriedades: title e narrative. " +
            "As duas propriedades devem ser strings. " +
            "title deve ser curto e dramático. " +
            "Para uma conclusão, escreva uma narrativa cinematográfica de aproximadamente 5 a 8 frases, " +
            "com fechamento emocional e continuidade coerente. " +
            "Não invente números ou resultados mecânicos.";

        BattleStory? story = await GeneratePromptAsync(
            prompt,
            maxTokens: 420,
            temperature: 0.32f,
            systemPromptOverride: conclusionSystemPrompt,
            maxNarrativeLength: 1800);

        return story ?? new BattleStory(
            success ? $"Vitória no Nível {level}" : "A Queda da Party",
            success
                ? "Com o conflito finalmente superado, a party permanece de pé e o nível termina em vitória. As marcas da batalha seguem com os heróis enquanto a jornada continua para o próximo desafio."
                : "A batalha chega ao fim com a party derrotada pelas consequências do confronto. Sem forças para continuar, a jornada termina aqui.");
    }

    public async Task<Event?> GenerateEventAsync(
        Event mechanicsTemplate,
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        int level,
        int gold,
        IReadOnlyCollection<string> previousBattleStories,
        IReadOnlyCollection<string> recentEvents)
    {
        await _generationLock.WaitAsync();

        try
        {
            if (!await EnsureModelLoadedAsync())
                return null;

            string prompt = BuildEventPrompt(
                team,
                items,
                level,
                gold,
                previousBattleStories,
                recentEvents);

            const string systemPrompt =
                "Você é o bardo narrador de eventos de um roguelike medieval chamado RPG Battle Maker. " +
                "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional. " +
                "O JSON deve conter exatamente cinco propriedades: title, description, optionA, optionB, optionC. " +
                "Todas devem ser strings. Cada opção deve ser uma única frase curta descrevendo uma ação do jogador. " +
                "Não revele os resultados mecânicos das escolhas. " +
                "Escreva tudo em português do Brasil.";

            return await GenerateWithRetryAsync(
                prompt,
                systemPrompt,
                maxTokens: 220,
                temperature: 0.2f,
                diagnosticPrefix: "event",
                parser: response => ParseEventResponse(
                    response,
                    mechanicsTemplate.Id));
        }
        catch (Exception exception)
        {
            WriteDiagnostic("event-exception", exception.ToString());
            return null;
        }
        finally
        {
            _generationLock.Release();
        }
    }

    private async Task<BattleStory?> GeneratePromptAsync(
        string prompt,
        int maxTokens = 220,
        float temperature = 0.35f,
        string? systemPromptOverride = null,
        int maxNarrativeLength = 500)
    {
        await _generationLock.WaitAsync();

        try
        {
            if (!await EnsureModelLoadedAsync())
                return null;

            string systemPrompt = systemPromptOverride ??
                "Você é o narrador de combate de um roguelike medieval chamado RPG Battle Maker. " +
                "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional. " +
                "O JSON deve conter exatamente duas propriedades: title e narrative. " +
                "As duas propriedades devem ter APENAS strings como valores. " +
                "title deve ser curto. narrative deve ter de 2 a 3 frases. " +
                "Não invente números, dano, cura, ouro, DC, XP ou resultados diferentes dos informados no estado. " +
                "Se houve falha, descreva claramente que o herói ou grupo foi prejudicado, sem inventar um valor de dano. " +
                "Se houve crítico, destaque o impacto extraordinário. " +
                "A narrativa deve continuar naturalmente a partir da história anterior quando houver uma. " +
                "Escreva tudo em português do Brasil.";

            return await GenerateWithRetryAsync(
                prompt,
                systemPrompt,
                maxTokens,
                temperature,
                diagnosticPrefix: "story",
                parser: response => ParseResponse(response, maxNarrativeLength));
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

    private async Task<T?> GenerateWithRetryAsync<T>(
        string prompt,
        string systemPrompt,
        int maxTokens,
        float temperature,
        string diagnosticPrefix,
        Func<string?, T?> parser)
        where T : class
    {
        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using CancellationTokenSource timeout =
                new(TimeSpan.FromSeconds(attempt == 1 ? 15 : 8));

            string attemptPrompt = attempt == 1
                ? prompt
                : prompt +
                  "\n\nATENÇÃO: a resposta anterior foi inválida ou incompleta. " +
                  "Gere novamente do zero. Responda SOMENTE com JSON válido e completo, " +
                  "sem texto antes ou depois do JSON.";

            string? response = await GenerateTextAsync(
                attemptPrompt,
                timeout.Token,
                systemPrompt,
                maxTokens,
                temperature);

            WriteDiagnostic(
                $"{diagnosticPrefix}-attempt-{attempt}",
                response);

            T? result = parser(response);

            if (result is not null)
                return result;

            if (attempt < maxAttempts)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(250 * attempt));
            }
        }

        return null;
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
        CancellationToken cancellationToken,
        string? systemPromptOverride = null,
        int maxTokens = 150,
        float temperature = 0.35f)
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

                string systemPrompt = systemPromptOverride ??
                    "Você é o narrador de combate de um roguelike medieval chamado RPG Battle Maker. " +
                    "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional. " +
                    "O JSON deve conter exatamente duas propriedades: title e narrative. " +
                    "As duas propriedades devem ter APENAS strings como valores. " +
                    "title deve ser curto. narrative deve ter de 2 ou 3 frases. " +
                    "Não invente números, dano, cura, ouro, DC, XP ou resultados diferentes dos informados no estado. " +
                    "Se houve falha, descreva claramente que o herói ou grupo foi prejudicado, sem inventar um valor de dano. " +
                    "Se houve crítico, destaque o impacto extraordinário. " +
                    "A narrativa deve continuar naturalmente a partir da história anterior quando houver uma. " +
                    "Escreva tudo em português do Brasil.";

                string modelPrompt =
                    "<|start_header_id|>system<|end_header_id|>\n" +
                    systemPrompt +
                    "\n<|eot_id|>" +
                    "<|start_header_id|>user<|end_header_id|>\n" +
                    prompt +
                    "\nRetorne somente o JSON completo.\n" +
                    "<|eot_id|>" +
                    "<|start_header_id|>assistant<|end_header_id|>\n";

                InferenceParams inferenceParams = new()
                {
                    MaxTokens = maxTokens,
                    SamplingPipeline = new DefaultSamplingPipeline
                    {
                        Temperature = temperature,
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
            ? "Esta é a abertura da jornada; ainda não existe história anterior."
            : string.Join(
                Environment.NewLine,
                context.PreviousStories.TakeLast(4).Select((story, index) =>
                    $"{index + 1}. {story}"));

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

    private static string BuildEventPrompt(
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        int level,
        int gold,
        IReadOnlyCollection<string> previousBattleStories,
        IReadOnlyCollection<string> recentEvents)
    {
        string teamText = team.Count == 0
            ? "Nenhum herói restante."
            : string.Join(
                Environment.NewLine,
                team.Select(agent =>
                    $"- {agent.Name} | classe: {agent.Type} | HP: {agent.CurrentLife}/{agent.MaxLife} | ATK: {agent.BaseAttack} | DEF: {agent.BaseDefense} | PER: {agent.BaseSkill}"));

        string itemText = items.Count == 0
            ? "Nenhuma relíquia."
            : string.Join(
                Environment.NewLine,
                items.Select(item => $"- {item.Name}"));

        string battleStoryText = previousBattleStories.Count == 0
            ? "Nenhuma narrativa de batalha anterior."
            : string.Join(
                Environment.NewLine,
                previousBattleStories.TakeLast(5).Select((story, index) =>
                    $"{index + 1}. {story}"));

        string recentText = recentEvents.Count == 0
            ? "Nenhum evento anterior. Este é o primeiro evento da jornada."
            : string.Join(
                Environment.NewLine,
                recentEvents.TakeLast(4).Select((eventStory, index) =>
                    $"{index + 1}. {eventStory}"));

        return $"""
            GERE O PRÓXIMO EVENTO DA MESMA AVENTURA

            Estado:
            - Andar: {level}/5
            - Ouro: {gold}

            Esquadrão:
            {teamText}

            Relíquias:
            {itemText}

            NARRATIVA PRINCIPAL DA BATALHA:
            {battleStoryText}

            HISTÓRIA DOS EVENTOS ANTERIORES:
            {recentText}

            REGRA PRINCIPAL DE CONTINUIDADE:
            Este evento acontece imediatamente depois da última cena da narrativa principal.
            O evento NÃO é uma história separada ou genérica.
            Deve parecer uma consequência natural do que acabou de acontecer com a party.
            Retome personagens, locais, inimigos, objetivos, ameaças, problemas ou elementos
            narrativos já apresentados sempre que houver algo relevante.
            A descrição do evento deve deixar claro por que este encontro está acontecendo agora.
            Não contradiga a história anterior e não reinicie a aventura com uma situação aleatória.
            Caso a conclusão do nível tenha criado uma consequência, use essa consequência como ponto
            de partida para o evento.
            
            Estrutura das escolhas:
            - Opção 1: ação física ou ofensiva
            - Opção 2: perícia ou astúcia
            - Opção 3: cautela, recuo ou observação

            Crie uma situação de fantasia medieval interessante e diretamente ligada à história acima.
            Não revele os resultados mecânicos.
            Cada opção deve ser uma frase curta e clara.
            """;
    }

    private static BattleStory? ParseResponse(
        string? response,
        int maxNarrativeLength = 500)
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
                Clean(narrative, maxNarrativeLength));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Event? ParseEventResponse(string? response, int mechanicsId)
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

            return new Event(
                mechanicsId,
                Clean(title, 70),
                Clean(description, 420),
                Clean(optionA, 100),
                Clean(optionB, 100),
                Clean(optionC, 100));
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
