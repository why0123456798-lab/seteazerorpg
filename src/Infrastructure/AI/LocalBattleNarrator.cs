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

    // Mantemos o contexto do modelo maior, mas não deixamos a memória narrativa
    // crescer indefinidamente. Quando a história bruta passa desse limite,
    // os capítulos antigos são compactados em uma crônica curta.
    private const int NarrativeMemoryThresholdChars = 7000;
    private const int RecentStoriesToKeep = 2;
    private const int RecentEventsToKeep = 1;

    private readonly SemaphoreSlim _generationLock = new(1, 1);
    private readonly SemaphoreSlim _memoryLock = new(1, 1);
    private LLamaWeights? _model;
    private LLamaContext? _context;
    private string? _loadedModelPath;
    private bool _initializationAttempted;
    private AIHardwareProfile? _hardwareProfile;

    private string _adventureChronicle = string.Empty;
    private int _summarizedStoryCount;
    private int _summarizedEventCount;

    public async Task<BattleStory?> GenerateInitialAsync(
        int level,
        IReadOnlyCollection<string> previousStories,
        IReadOnlyCollection<string> recentEvents)
    {
        string narrativeMemory = await PrepareNarrativeMemoryAsync(
            previousStories,
            recentEvents);

        const string introductionSystemPrompt =
            "Você é o mestre de um rpg de mesa de uma aventura de roguelike medieval chamada RPG Battle Maker. " +
            "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional. " +
            "O JSON deve conter exatamente três propriedades: title, narrative e theme. " +
            "As três propriedades devem ser strings. " +
            "theme deve ser exatamente uma destas opções: Ataque, Defesa ou Perícia. " +
            "Escolha o tema mais apropriado para o conflito apresentado na própria história. " +
            "title deve ser curto e evocativo. " +
            "Para uma introdução de nível, escreva uma narrativa rica de aproximadamente 8 a 10 frases, com detalhes concretos e progressão causal. " +
            "Não economize palavras quando um detalhe ajudar a construir o mundo, os personagens ou a tensão. " +
            "Crie uma história cativante de como a party chegou até aquele local, o que aconteceu no caminho, o que percebe no ambiente e como a ameaça se revela. " +
            "Em níveis posteriores, continue diretamente a história anterior " +
            "e preserve personagens, lugares, ameaças e consequências já estabelecidas. " +
            "Não invente números ou resultados mecânicos.";

        BattleStory? story = await GeneratePromptAsync(
            $"""
            ABERTURA DO NÍVEL {level}

            Esta é uma continuação da mesma aventura.
            Antes de chegar ao combate, dê ao jogador um contexto maior sobre como a party chegou até aqui lembre-se você é um mestre de um rpg de mesa contando a história.
            Siga obrigatoriamente esta ordem causal:
            1. o que a party estava fazendo ou perseguindo;
            2. como e por que chegou ao local atual;
            3. o que encontrou ali e qual é a ameaça;
            4. por que o conflito começa agora.
            Não comece pela atmosfera. O leitor precisa entender primeiro por que a party está ali.
            Não crie uma missão paralela desconectada.
            Termine com a party diante do confronto, pronta para rolar o primeiro D20.

            MEMÓRIA DA AVENTURA:
            {narrativeMemory}

            Continue exatamente a partir do último acontecimento narrado.
            """,
            maxTokens: 560,
            temperature: 0.28f,
            systemPromptOverride: introductionSystemPrompt,
            maxNarrativeLength: 1800,
            maxAttempts: 2,
            requireTheme: true);

        if (story is not null)
            return story;

        // Última tentativa exclusiva da introdução: reduzimos o pedido ao essencial
        // para que uma resposta AI completa tenha prioridade sobre qualquer fallback fixo.
        const string compactIntroductionSystemPrompt =
            "Você é o mestre de um rpg de mesa de uma aventura medieval. " +
            "Responda SOMENTE com JSON válido contendo title, narrative e theme. " +
            "theme deve ser exatamente Ataque, Defesa ou Perícia e deve combinar com o conflito narrado. " +
            "Escreva aproximadamente 7 a 9 frases em português do Brasil, com narrativa rica e progressão causal. " +
            "Inclua objetivo da party, caminho até o local, detalhes relevantes do ambiente, acontecimentos encontrados pelo caminho, sinais da ameaça, descoberta do conflito e o motivo pelo qual ele começa agora. " +
            "Não comece com atmosfera genérica e não invente números ou mecânicas.";

        BattleStory? compactStory = await GeneratePromptAsync(
            $"""
            ABERTURA DO NÍVEL {level}

            MEMÓRIA DA AVENTURA:
            {narrativeMemory}

            Escreva uma introdução causal em aproximadamente 7 a 9 frases, podendo aumentar quando isso enriquecer a história sem repetir informações.
            """,
            maxTokens: 520,
            temperature: 0.25f,
            systemPromptOverride: compactIntroductionSystemPrompt,
            maxNarrativeLength: 1800,
            maxAttempts: 2,
            requireTheme: true);

        if (compactStory is not null)
            return compactStory;

        // Não inventamos uma narrativa local. Se o modelo realmente não respondeu,
        // o caller recebe null e pode informar que a geração falhou.
        return null;
    }

    public async Task<BattleStory?> GenerateAsync(BattleNarrativeContext context)
    {
        string narrativeMemory = await PrepareNarrativeMemoryAsync(
            context.PreviousStories,
            Array.Empty<string>());

        BattleStory? story = await GeneratePromptAsync(
            BuildPrompt(context, narrativeMemory),
            maxTokens: 600,
            temperature: 0.32f,
            maxNarrativeLength: 1600,
            rejectInjuryOnSuccess: context.Outcome.Equals("SUCESSO", StringComparison.OrdinalIgnoreCase),
            requireDamageConsequence: context.Outcome.Equals("FALHA", StringComparison.OrdinalIgnoreCase) &&
                                      context.OutcomeDetail.Contains("dano", StringComparison.OrdinalIgnoreCase),
            requireTheme: true);

        if (story is not null && !HasExcessiveNarrativeOverlap(story.Narrative, context.PreviousStories))
            return story;

        if (story is not null)
        {
            WriteDiagnostic(
                "story-repetition-rejected",
                $"Narrativa rejeitada por repetição excessiva. level={context.Level}; round={context.Round}");
        }

        BattleStory? freshStory = await GeneratePromptAsync(
            BuildPrompt(context, narrativeMemory) + """

            ATENÇÃO: a primeira tentativa foi considerada repetitiva.
            Escreva uma cena completamente nova que avance a situação.
            Não reutilize a estrutura ou as frases da última narrativa.
            """,
            maxTokens: 650,
            temperature: 0.38f,
            maxNarrativeLength: 1700,
            rejectInjuryOnSuccess: context.Outcome.Equals("SUCESSO", StringComparison.OrdinalIgnoreCase),
            requireDamageConsequence: context.Outcome.Equals("FALHA", StringComparison.OrdinalIgnoreCase) &&
                                      context.OutcomeDetail.Contains("dano", StringComparison.OrdinalIgnoreCase),
            requireTheme: true);

        if (freshStory is not null && !HasExcessiveNarrativeOverlap(freshStory.Narrative, context.PreviousStories))
            return freshStory;

        WriteDiagnostic(
            "story-fallback",
            $"Geração inválida após retries. level={context.Level}; round={context.Round}; hero={context.Hero.Name}; outcome={context.Outcome}");

        return new BattleStory(
            context.Outcome.Equals("SUCESSO", StringComparison.OrdinalIgnoreCase)
                ? "Avanço da Party"
                : "A Batalha Continua",
            context.Outcome.Equals("SUCESSO", StringComparison.OrdinalIgnoreCase)
                ? $"{context.Hero.Name} executa a ação com sucesso e a equipe ganha terreno no confronto. A ameaça é pressionada enquanto a party avança para o próximo momento decisivo."
                : $"{context.Hero.Name} falha na ação e é atingido no confronto, sofrendo as consequências do golpe. A equipe sente a tensão aumentar enquanto o combate avança para o próximo momento decisivo.",
            context.Theme);
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
        string narrativeMemory = await PrepareNarrativeMemoryAsync(
            previousStories,
            recentEvents);

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
              Mostre a equipe superando o conflito e encerrando este capítulo com uma sensação clara de conquista e progresso.
              EM CASO DE SUCESSO, NÃO diga que a party foi ferida, machucada, atacada, sofreu dano, sangrou ou perdeu vida.
              A vitória deve ser narrada como uma consequência positiva da ação da party, podendo mostrar a ameaça sendo derrotada ou afastada.
              Não trate isso como o fim da aventura inteira: deixe uma continuidade natural
              para o próximo nível.
              """
            : $"""
              DERROTA / GAME OVER
              A equipe fracassou no nível com {successes} sucesso(s) e {failures} falha(s).
              A história deve encerrar a aventura derrotando a party.
              Mostre as consequências do fracasso, a derrota dos heróis e o fim da jornada pode ser qualquer tipo de derrota, morte dos heróis, objetivo não cumprido.
              Não transforme a derrota em vitória ou em um simples recuo.
              """;

        string prompt = $"""
            CONCLUSÃO NARRATIVA DO NÍVEL {level}

            Você está escrevendo a conclusão dramática do capítulo atual de uma única aventura.
            Não crie uma história nova. Continue exatamente o que já foi contado como um mestre de rpg de mesa faria.

            {result}

            MEMÓRIA DA AVENTURA:
            {narrativeMemory}

            ESTADO FINAL DA PARTY:
            {teamText}

            Escreva uma conclusão satisfatória e coerente podendo ser uma conclusão desse capítulo ou criando um gancho para as próximas aventuras.
            O resultado mecânico acima é ABSOLUTO e tem prioridade sobre qualquer interpretação da memória narrativa.
            Em caso de sucesso, a party deve claramente vencer o conflito deste nível mesmo que o objetivo final não tenha aindo sido alcançado.
            Em caso de sucesso, NÃO descreva ferimentos, dano, sangue, perda de vida ou a party sendo atacada. Ferimentos pertencem exclusivamente às rodadas que tiveram falha ou crítico negativo.
            Em caso de fracasso, a party deve claramente ser derrotada e a aventura terminar.
            O texto NÃO pode transformar a derrota em vitória mesmo que a memória anterior contenha cenas de sucesso.
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
            maxNarrativeLength: 1800,
            rejectInjuryOnSuccess: success);

        if (story is not null && (success || !ContainsVictoryConsequence(story.Narrative)))
            return story;

        if (!success && story is not null && ContainsVictoryConsequence(story.Narrative))
        {
            WriteDiagnostic(
                "conclusion-victory-rejected",
                $"Conclusão de vitória rejeitada porque o resultado mecânico é DERROTA. level={level}; successes={successes}; failures={failures}");
        }

        // Retry with the already compacted narrative memory so the final scene still gets a fresh AI ending.
        string compactPrompt = $"""
            CONCLUSÃO FINAL DO NÍVEL {level}

            Resultado obrigatório: {(success ? "VITÓRIA DA PARTY" : "DERROTA DA PARTY")}.
            Sucessos: {successes}.
            Falhas: {failures}.

            Escreva o desfecho definitivo deste combate.
            A história termina agora e deve fechar o arco deste nível.
            {(success
                ? "A party derrota a ameaça e conclui o objetivo do nível com uma vitória."
                : "A party é derrotada pela ameaça e a jornada termina aqui.")}
            Não deixe o combate em aberto e não anuncie um próximo momento decisivo.

            MEMÓRIA COMPACTADA DA AVENTURA:
            {narrativeMemory}

            ESTADO DA PARTY:
            {teamText}
            """;

        story = await GeneratePromptAsync(
            compactPrompt,
            maxTokens: 320,
            temperature: 0.28f,
            systemPromptOverride: conclusionSystemPrompt,
            maxNarrativeLength: 1400,
            rejectInjuryOnSuccess: success);

        if (story is not null && (success || !ContainsVictoryConsequence(story.Narrative)))
            return story;

        return new BattleStory(
            success ? $"Vitória no Nível {level}" : "A Queda da Party",
            success
                ? "A conclusão narrativa automática falhou, mas o sistema confirmou a vitória da party neste nível."
                : "A conclusão narrativa automática falhou, mas o sistema confirmou a derrota da party neste nível.");
    }

    public async Task<Event?> GenerateEventAsync(
        int mechanicsId,
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        int level,
        int gold,
        IReadOnlyCollection<string> previousBattleStories,
        IReadOnlyCollection<string> recentEvents)
    {
        string narrativeMemory = await PrepareNarrativeMemoryAsync(
            previousBattleStories,
            recentEvents);

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
                mechanicsId,
                narrativeMemory);

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
                    mechanicsId));
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
        int maxNarrativeLength = 500,
        int maxAttempts = 3,
        bool rejectInjuryOnSuccess = false,
        bool requireDamageConsequence = false,
        bool requireTheme = false)
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
                "Em caso de FALHA, descreva claramente que o herói ou grupo foi prejudicado, sem inventar um valor de dano. " +
                "Quando a falha tiver causado dano ao herói em destaque, é OBRIGATÓRIO narrar que esse próprio herói foi atingido, ferido ou sofreu uma consequência física concreta causada pelo conflito. " +
                "Não diga que ele se defendeu com sucesso, desviou completamente ou saiu ileso quando o estado mecânico informa que ele sofreu dano. " +
                "Em caso de SUCESSO, é PROIBIDO dizer que a party foi ferida, machucada, sofreu dano, sangrou ou perdeu vida. " +
                "Em caso de SUCESSO, descreva apenas avanço, defesa bem-sucedida, domínio da situação, derrota do inimigo ou outra consequência positiva coerente. " +
                "Se houve crítico, destaque o impacto extraordinário. " +
                "A narrativa deve continuar naturalmente a partir da história anterior quando houver uma. " +
                "Escreva tudo em português do Brasil.";

            if (requireDamageConsequence)
            {
                systemPrompt +=
                    " ATENÇÃO: esta falha causou dano ao herói em destaque. " +
                    "A narrativa SOMENTE pode ser aceita se mostrar esse herói sendo atingido, ferido ou sofrendo uma consequência física concreta. " +
                    "Não basta dizer que a situação ficou perigosa; o dano precisa aparecer na ação narrada. " +
                    "Não atribua esse dano a outro personagem.";
            }

            if (requireTheme)
            {
                systemPrompt +=
                    " O JSON também deve conter exatamente a propriedade theme. " +
                    "theme deve ser exatamente Ataque, Defesa ou Perícia e deve representar o melhor tema para a PRÓXIMA rodada, " +
                    "com base na situação que você acabou de narrar. Não escolha aleatoriamente.";
            }

            return await GenerateWithRetryAsync(
                prompt,
                systemPrompt,
                maxTokens,
                temperature,
                diagnosticPrefix: "story",
                parser: response =>
                {
                    BattleStory? parsed = ParseResponse(response, maxNarrativeLength, requireTheme);

                    if (parsed is null)
                        return null;

                    if (rejectInjuryOnSuccess && ContainsInjuryConsequence(parsed.Narrative))
                        return null;

                    if (requireDamageConsequence && !ContainsDamageConsequence(parsed.Narrative))
                        return null;

                    return parsed;
                },
                maxAttempts: maxAttempts);
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
        Func<string?, T?> parser,
        int maxAttempts = 3)
        where T : class
    {

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            int timeoutSeconds = maxTokens <= 200
                ? (attempt == 1 ? 25 : 15)
                : (attempt == 1 ? 35 : 18);

            using CancellationTokenSource timeout =
                new(TimeSpan.FromSeconds(timeoutSeconds));

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
        {
            WriteDiagnostic("model-missing", modelPath);
            return false;
        }

        _loadedModelPath = modelPath;

        foreach (AIHardwareProfile profile in GetHardwareProfilesToTry())
        {
            try
            {
                ConfigureBackend(profile.Backend);

                ModelParams parameters = new(modelPath)
                {
                    ContextSize = profile.ContextSize,
                    GpuLayerCount = profile.GpuLayerCount,
                    MainGpu = 0,
                    BatchSize = profile.BatchSize,
                    UBatchSize = profile.UBatchSize,
                    FlashAttention = profile.FlashAttention,
                    OpOffload = profile.Backend != AIBackend.Cpu
                };

                WriteDiagnostic(
                    "hardware-profile",
                    $"{profile.Description} | backend={profile.Backend} | context={profile.ContextSize} | batch={profile.BatchSize} | ubatch={profile.UBatchSize} | gpuLayers={profile.GpuLayerCount} | flash={profile.FlashAttention}");

                _model = await Task.Run(() => LLamaWeights.LoadFromFile(parameters));

                // Reutilizamos o contexto nativo entre as gerações para evitar
                // recriar a infraestrutura CUDA/Vulkan a cada narrativa.
                _context = await Task.Run(() => _model.CreateContext(parameters));

                _hardwareProfile = profile;
                return true;
            }
            catch (Exception exception)
            {
                WriteDiagnostic(
                    $"model-load-failed-{profile.Backend}",
                    exception.ToString());

                _model?.Dispose();
                _model = null;
            }
        }

        _loadedModelPath = null;
        return false;
    }

    private IEnumerable<AIHardwareProfile> GetHardwareProfilesToTry()
    {
        string? forcedBackend = Environment
            .GetEnvironmentVariable("RPGBATTLE_LLM_BACKEND")
            ?.Trim()
            .ToLowerInvariant();

        if (forcedBackend == "cuda")
        {
            AIHardwareProfile detected = AIHardwareProfile.Detect();
            if (detected.Backend == AIBackend.Cuda)
                yield return detected;
            yield return AIHardwareProfile.CreateVulkan();
            yield return AIHardwareProfile.CreateCpu();
            yield break;
        }

        if (forcedBackend == "vulkan")
        {
            yield return AIHardwareProfile.CreateVulkan();
            yield return AIHardwareProfile.CreateCpu();
            yield break;
        }

        if (forcedBackend == "cpu")
        {
            yield return AIHardwareProfile.CreateCpu();
            yield break;
        }

        AIHardwareProfile automatic = AIHardwareProfile.Detect();
        yield return automatic;

        if (automatic.Backend != AIBackend.Vulkan)
            yield return AIHardwareProfile.CreateVulkan();

        yield return AIHardwareProfile.CreateCpu();
    }

    private static void ConfigureBackend(AIBackend backend)
    {
        switch (backend)
        {
            case AIBackend.Cuda:
                NativeLibraryConfig.All
                    .WithCuda(true)
                    .WithVulkan(false)
                    .WithAutoFallback(false);
                break;

            case AIBackend.Vulkan:
                NativeLibraryConfig.All
                    .WithCuda(false)
                    .WithVulkan(true)
                    .WithAutoFallback(false);
                break;

            default:
                NativeLibraryConfig.All
                    .WithCuda(false)
                    .WithVulkan(false)
                    .WithAutoFallback(true);
                break;
        }
    }

    private async Task<string?> GenerateTextAsync(
        string prompt,
        CancellationToken cancellationToken,
        string? systemPromptOverride = null,
        int maxTokens = 150,
        float temperature = 0.35f)
    {
        if (_model is null || _context is null)
            return null;

        try
        {
            return await Task.Run(async () =>
            {
                // InteractiveExecutor é stateful e mantém contadores/histórico
                // internos entre chamadas. Por isso criamos um executor novo para
                // cada geração, mas reutilizamos o LLamaContext nativo.
                _context.NativeHandle.MemoryClear();
                InteractiveExecutor executor = new(_context);

                string systemPrompt = systemPromptOverride ??
                    "Você é o narrador de combate de um roguelike medieval chamado RPG Battle Maker. " +
                    "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional. " +
                    "O JSON deve conter exatamente duas propriedades: title e narrative. " +
                    "As duas propriedades devem ter APENAS strings como valores. " +
                    "title deve ser curto. narrative deve ter aproximadamente 4 a 6 frases, desenvolvendo a cena em vez de apenas resumir o resultado. " +
                    "Descreva ações, ambiente, reação dos inimigos e consequências narrativas coerentes com o resultado informado. " +
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

    private async Task<string> PrepareNarrativeMemoryAsync(
        IReadOnlyCollection<string> stories,
        IReadOnlyCollection<string> events)
    {
        await _memoryLock.WaitAsync();

        try
        {
            // O mesmo narrator é singleton. Quando as listas voltam a zero,
            // significa que uma nova partida começou e a crônica antiga deve ser descartada.
            if (stories.Count == 0 && events.Count == 0)
            {
                _adventureChronicle = string.Empty;
                _summarizedStoryCount = 0;
                _summarizedEventCount = 0;
            }
            else if (stories.Count < _summarizedStoryCount ||
                     events.Count < _summarizedEventCount)
            {
                _adventureChronicle = string.Empty;
                _summarizedStoryCount = 0;
                _summarizedEventCount = 0;
            }

            int storyBoundary = Math.Max(0, stories.Count - RecentStoriesToKeep);
            int eventBoundary = Math.Max(0, events.Count - RecentEventsToKeep);

            int rawCharacters = stories.Sum(story => story.Length) +
                                events.Sum(eventStory => eventStory.Length);

            bool hasUnsummarizedArchive =
                storyBoundary > _summarizedStoryCount ||
                eventBoundary > _summarizedEventCount;

            bool shouldCompact =
                rawCharacters > NarrativeMemoryThresholdChars &&
                hasUnsummarizedArchive;

            if (shouldCompact)
            {
                string olderStories = string.Join(
                    Environment.NewLine,
                    stories
                        .Skip(_summarizedStoryCount)
                        .Take(Math.Max(0, storyBoundary - _summarizedStoryCount))
                        .Select(story => $"- {story}"));

                string olderEvents = string.Join(
                    Environment.NewLine,
                    events
                        .Skip(_summarizedEventCount)
                        .Take(Math.Max(0, eventBoundary - _summarizedEventCount))
                        .Select(eventStory => $"- {eventStory}"));

                string archive = $"""
                    CRÔNICA EXISTENTE:
                    {_adventureChronicle}

                    NOVAS CENAS PARA INCORPORAR:
                    {olderStories}

                    NOVOS EVENTOS PARA INCORPORAR:
                    {olderEvents}
                    """;

                const string summarySystemPrompt =
                    "Você mantém a memória de uma única aventura de roguelike medieval. " +
                    "Responda SOMENTE com um objeto JSON válido, sem markdown e sem texto adicional. " +
                    "O JSON deve conter exatamente duas propriedades: title e narrative. " +
                    "title deve ser a string Crônica da Aventura. " +
                    "narrative deve ser uma crônica compacta e contada como um mestre de um rpg de mesa, preservando personagens, lugares, " +
                    "inimigos, objetivos, relações, ameaças e consequências importantes. " +
                    "Elimine detalhes repetidos. " +
                    "Escreva em português do Brasil.";

                BattleStory? summary = await GeneratePromptAsync(
                    $"""
                    Atualize a crônica da aventura usando a memória existente e os novos acontecimentos abaixo.

                    {archive}

                    Preserve os fatos importantes para que uma cena futura consiga continuar a história sem
                    precisar das cenas antigas completas. Escreva de forma compacta, mas não apague fatos
                    relevantes para personagens, lugares, ameaças e consequências.
                    """,
                    maxTokens: 280,
                    temperature: 0.15f,
                    systemPromptOverride: summarySystemPrompt,
                    maxNarrativeLength: 1600);

                if (summary is not null)
                {
                    _adventureChronicle = summary.Narrative;
                    _summarizedStoryCount = storyBoundary;
                    _summarizedEventCount = eventBoundary;

                    WriteDiagnostic(
                        "memory-compaction",
                        $"Resumo atualizado. Histórias compactadas: {_summarizedStoryCount}. Eventos compactados: {_summarizedEventCount}.");
                }
            }

            if (!string.IsNullOrWhiteSpace(_adventureChronicle))
            {
                string recentStories = stories.Count == 0
                    ? "Nenhuma cena recente."
                    : string.Join(
                        Environment.NewLine,
                        stories.TakeLast(RecentStoriesToKeep).Select((story, index) =>
                            $"{index + 1}. {story}"));

                string recentEvents = events.Count == 0
                    ? "Nenhum evento recente."
                    : string.Join(
                        Environment.NewLine,
                        events.TakeLast(RecentEventsToKeep).Select((eventStory, index) =>
                            $"{index + 1}. {eventStory}"));

                return $"""
                    CRÔNICA DA AVENTURA:
                    {_adventureChronicle}

                    ÚLTIMAS CENAS:
                    {recentStories}

                    EVENTOS MAIS RECENTES:
                    {recentEvents}
                    """;
            }

            if (stories.Count == 0 && events.Count == 0)
                return "Esta é a primeira cena da jornada; ainda não existe história anterior.";

            // Antes da primeira compactação, ainda temos espaço para enviar o histórico completo.
            return $"""
                HISTÓRIA ATUAL DA AVENTURA:
                {string.Join(Environment.NewLine, stories.Select((story, index) => $"{index + 1}. {story}"))}

                EVENTOS DA JORNADA:
                {(events.Count == 0 ? "Nenhum evento anterior." : string.Join(
                    Environment.NewLine,
                    events.Select((eventStory, index) => $"{index + 1}. {eventStory}")))}
                """;
        }
        catch (Exception exception)
        {
            WriteDiagnostic("memory-compaction-exception", exception.ToString());

            return $"""
                ÚLTIMAS CENAS CONHECIDAS:
                {string.Join(Environment.NewLine, stories.TakeLast(RecentStoriesToKeep).Select((story, index) => $"{index + 1}. {story}"))}

                EVENTOS RECENTES CONHECIDOS:
                {(events.Count == 0 ? "Nenhum evento anterior." : string.Join(
                    Environment.NewLine,
                    events.TakeLast(RecentEventsToKeep).Select((eventStory, index) => $"{index + 1}. {eventStory}")))}
                """;
        }
        finally
        {
            _memoryLock.Release();
        }
    }

    private static string BuildPrompt(
        BattleNarrativeContext context,
        string narrativeMemory)
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

        return $"""
            CONTINUAÇÃO DA BATALHA

            Andar: {context.Level}/5
            Rodada: {context.Round}/5
            Tema usado nesta rodada: {context.Theme}
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

            Memória narrativa:
            {narrativeMemory}

            A última cena da memória JÁ ACONTECEU e não deve ser recontada.
            Continue a história a partir do ponto exato em que ela terminou.
            Mostre uma NOVA consequência causada exclusivamente pelo resultado desta rodada.
            Não repita frases, acontecimentos, diálogos, descobertas ou consequências que já foram narrados.
            Se os inimigos já foram derrotados, não diga novamente que eles foram derrotados; mostre o que acontece DEPOIS da derrota.
            Se um objeto, mensagem ou local já foi descoberto, não o descubra novamente; faça a história avançar a partir dessa descoberta.
            A cada rodada a situação deve mudar de forma perceptível.
            Não crie uma nova missão ou um novo cenário sem motivo.

            PROFUNDIDADE DA CENA:
            Escreva aproximadamente 6 a 9 frases, com uma narrativa rica e cinematográfica.
            Não transforme a rodada em um simples resumo do resultado do dado.
            Desenvolva a cena mostrando a ação do herói, a reação dos adversários ou do ambiente,
            a consequência concreta do resultado e como a situação fica diferente para a próxima rodada.
            Quando fizer sentido, inclua detalhes visuais, sons, tensão, diálogo curto ou reação de outro membro da party.
            Varie a estrutura das cenas: algumas podem enfatizar ação, outras descoberta, estratégia,
            reação emocional, perigo ambiental ou mudança na posição dos combatentes.
            Evite começar repetidamente com o nome do herói e evite estruturas idênticas entre rodadas.
            O resultado mecânico define o que aconteceu, mas a narrativa deve mostrar COMO isso aconteceu.

            REGRA DE DANO NARRATIVO:
            Se a Consequência mecânica informar que o herói em destaque sofreu dano nesta rodada,
            essa consequência é OBRIGATÓRIA na narrativa. Mostre o próprio {context.Hero.Name} sendo atingido,
            ferido ou sofrendo uma consequência física concreta causada pela falha.
            Pode ser um golpe, corte, queimadura, impacto, queda, explosão ou outra consequência física coerente com a cena.
            Não diga que {context.Hero.Name} se defendeu, desviou completamente ou saiu ileso nessa situação.
            Não transfira o dano para outro membro da party.
            Não é necessário informar a quantidade de HP perdida.

            PRÓXIMO TEMA:
            Ao final da resposta, escolha o tema que melhor combina com a situação criada para a PRÓXIMA rodada.
            Não precisa manter o mesmo tema desta rodada. A próxima rodada pode ser Ataque, Defesa ou Perícia,
            conforme a evolução natural da história.
            O campo theme do JSON deve conter SOMENTE um destes valores: Ataque, Defesa ou Perícia.
            """;
    }

    private static string BuildEventPrompt(
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        int level,
        int gold,
        int mechanicsId,
        string narrativeMemory)
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

        return $"""
            GERE O PRÓXIMO EVENTO DA MESMA AVENTURA

            Estado:
            - Andar: {level}/5
            - Ouro: {gold}
            - Perfil mecânico interno do evento: {mechanicsId}

            O perfil mecânico é apenas um contrato interno para o C# resolver a consequência
            das escolhas. Não o mencione ao jogador e não deixe que ele limite a ambientação.
            O texto, título e as três opções devem ser originais e derivados da história atual.

            Esquadrão:
            {teamText}

            Relíquias:
            {itemText}

            MEMÓRIA DA AVENTURA:
            {narrativeMemory}

            REGRA PRINCIPAL DE CONTINUIDADE:
            Este evento acontece imediatamente depois da última cena da narrativa principal.
            O evento NÃO é uma história separada ou genérica.
            Deve parecer uma consequência natural do que acabou de acontecer com a party contada como um mestre de um rpg de mesa.
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

    private static bool HasExcessiveNarrativeOverlap(
        string narrative,
        IReadOnlyCollection<string> previousStories)
    {
        string latest = previousStories.LastOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(latest) || string.IsNullOrWhiteSpace(narrative))
            return false;

        static HashSet<string> BuildShingles(string text)
        {
            string[] words = System.Text.RegularExpressions.Regex
                .Replace(text.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var shingles = new HashSet<string>();
            for (int i = 0; i + 3 < words.Length; i++)
                shingles.Add($"{words[i]} {words[i + 1]} {words[i + 2]} {words[i + 3]}");

            return shingles;
        }

        HashSet<string> current = BuildShingles(narrative);
        HashSet<string> previous = BuildShingles(latest);

        if (current.Count == 0 || previous.Count == 0)
            return false;

        int common = current.Intersect(previous).Count();
        double overlap = common / (double)Math.Min(current.Count, previous.Count);

        return overlap >= 0.45;
    }

    private static string? NormalizeTheme(string? theme)
    {
        return theme?.Trim().ToLowerInvariant() switch
        {
            "ataque" => Agent.Ataque,
            "defesa" => Agent.Defesa,
            "perícia" => Agent.Pericia,
            "pericia" => Agent.Pericia,
            _ => null
        };
    }

    private static bool ContainsVictoryConsequence(string narrative)
    {
        string normalized = narrative.ToLowerInvariant();

        string[] forbidden =
        {
            "vitória",
            "vitoriosa",
            "vitorioso",
            "triunfo",
            "venceu",
            "vencem",
            "derrotou os",
            "derrota dos inimigos",
            "ameaça foi derrotada",
            "emergiu vitoriosa",
            "continuou sua jornada",
            "pronta para enfrentar"
        };

        return forbidden.Any(normalized.Contains);
    }

    private static bool ContainsInjuryConsequence(string narrative)
    {
        string normalized = narrative.ToLowerInvariant();

        string[] forbidden =
        {
            "ferid",
            "machuc",
            "ferimento",
            "dano",
            "sangr",
            "lesão",
            "lesionado",
            "perdeu vida",
            "perde vida",
            "recebeu dano",
            "sofreu dano"
        };

        return forbidden.Any(normalized.Contains);
    }

    private static bool ContainsDamageConsequence(string narrative)
    {
        string normalized = narrative.ToLowerInvariant();

        string[] concreteConsequences =
        {
            "ating",
            "golpe",
            "acert",
            "ferid",
            "machuc",
            "ferimento",
            "cort",
            "queim",
            "sangr",
            "lesão",
            "lesionado",
            "impacto",
            "derrub",
            "arremess",
            "explosão",
            "explod",
            "cravou",
            "perfur",
            "rasg"
        };

        return concreteConsequences.Any(normalized.Contains);
    }

    private static BattleStory? ParseResponse(
        string? response,
        int maxNarrativeLength = 500,
        bool requireTheme = false)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        string? jsonObject = ExtractFirstJsonObject(response);

        if (jsonObject is null)
            return null;

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(jsonObject);

            JsonElement root = document.RootElement;

            string title = ReadJsonText(root, "title");
            string narrative = ReadJsonText(root, "narrative");
            string? theme = requireTheme
                ? NormalizeTheme(ReadJsonText(root, "theme"))
                : null;

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(narrative) ||
                (requireTheme && theme is null))
            {
                return null;
            }

            return new BattleStory(
                Clean(title, 70),
                Clean(narrative, maxNarrativeLength),
                theme);
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

        string? jsonObject = ExtractFirstJsonObject(response);

        if (jsonObject is null)
            return null;

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(jsonObject);

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

    private static string? ExtractFirstJsonObject(string response)
    {
        int start = response.IndexOf('{');
        if (start < 0)
            return null;

        int depth = 0;
        bool inString = false;
        bool escaped = false;

        for (int i = start; i < response.Length; i++)
        {
            char c = response[i];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '\"')
                    inString = false;

                continue;
            }

            if (c == '\"')
            {
                inString = true;
                continue;
            }

            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;

                if (depth == 0)
                    return response[start..(i + 1)];
            }
        }

        return null;
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
        _memoryLock.Dispose();
        _model?.Dispose();
    }
}
