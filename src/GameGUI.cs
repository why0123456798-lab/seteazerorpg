using CsvHelper;
using System.Drawing.Drawing2D;
using System.Globalization;

public class GameGUI : Form
{
    private string csvPath;
    private string baseDir;
    private List<Agent> allAgents = new List<Agent>();
    private Dictionary<string, Image> imageCache = new Dictionary<string, Image>();
    private Random random = new Random();

    // Estado do Jogo
    private List<Agent> team = new List<Agent>();
    private int gold;
    private int currentLevel;
    private string mode;
    private Dictionary<string, int> roundBonuses = new Dictionary<string, int>();
    private List<Agent> market = new List<Agent>();
    private int rerollCount = 1;

    // Estado da Batalha/Missão
    private string currentTheme;
    private int dc;
    private Dictionary<string, int> shields = new Dictionary<string, int>();
    private int sucessos;
    private int falhas;
    private int testeNum;
    private Agent bestHero;
    private int bestVal;

    // Componentes da Interface Dinâmica
    private Panel mainPanel;
    private Dictionary<int, Color> rarityColors;

    // Controles específicos de telas para atualização
    private RichTextBox logTxt;
    private Button btnRoll;
    private PictureBox pbBattleHero;

    public GameGUI(string csvPath)
    {
        this.csvPath = csvPath;
        this.Text = "RPG Autobattler Roguelike";
        this.Size = new Size(970, 740);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = ColorTranslator.FromHtml("#1e1e1e");

        rarityColors = new Dictionary<int, Color>
            {
                { 1, ColorTranslator.FromHtml("#b0b0b0") },
                { 2, ColorTranslator.FromHtml("#4caf50") },
                { 3, ColorTranslator.FromHtml("#2196f3") },
                { 4, ColorTranslator.FromHtml("#9c27b0") },
                { 5, ColorTranslator.FromHtml("#ff9800") }
            };

        baseDir = AppDomain.CurrentDomain.BaseDirectory;
        LoadDatabase();

        mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        this.Controls.Add(mainPanel);

        ResetGameState();
        CreateModeSelectionScreen();
    }

    private void LoadDatabase()
    {
        string fullPath = Path.Combine(baseDir, csvPath);
        if (!File.Exists(fullPath))
        {
            MessageBox.Show($"Arquivo {csvPath} não encontrado no diretório atual.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(0);
        }

        try
        {
            using (var reader = new StreamReader(fullPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<dynamic>().ToList();
                foreach (var row in records)
                {
                    allAgents.Add(new Agent(row));
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao ler o CSV: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(0);
        }
    }

    private string MappingTypes(string agentType)
    {
        if (agentType == AgentType.Lutador) return "⚔️";
        if (agentType == AgentType.Defensor) return "🛡️";
        if (agentType == AgentType.Especialista) return "🎯";
        if (agentType == AgentType.Suporte) return "❤️";
        return agentType;
    }

    private void ResetGameState()
    {
        foreach (var a in allAgents) a.ResetStatus();
        team.Clear();
        gold = 10;
        currentLevel = 1;
        mode = "Difícil";
        rerollCount = 1;
        roundBonuses = new Dictionary<string, int> { { Agent.Ataque, 0 }, { Agent.Defesa, 0 }, { "Escudo", 0 }, { "DC_Reduction", 0 } };
        market.Clear();
    }

    private void ClearScreen()
    {
        mainPanel.Controls.Clear();
    }

    // ==========================================
    // TELA 1: SELEÇÃO DE MODO
    // ==========================================
    private void CreateModeSelectionScreen()
    {
        ClearScreen();

        Panel centerFrame = new Panel { Size = new Size(400, 400), BackColor = Color.Transparent };
        centerFrame.Location = new Point((mainPanel.Width - centerFrame.Width) / 2, (mainPanel.Height - centerFrame.Height) / 2);
        centerFrame.Anchor = AnchorStyles.None;
        mainPanel.Controls.Add(centerFrame);

        Label lblTitle = new Label
        {
            Text = "RPG AUTOBATTLER ROGUELIKE",
            Font = new Font("Arial", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Size = new Size(400, 60),
            TextAlign = ContentAlignment.MiddleCenter,
            Top = 20
        };
        centerFrame.Controls.Add(lblTitle);

        Button btnNormal = new Button
        {
            Text = "Modo Clássico (Normal)",
            Font = new Font("Arial", 12, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#0a6d27"),
            ForeColor = Color.White,
            Size = new Size(300, 60),
            Left = 50,
            Top = 130,
            FlatStyle = FlatStyle.Flat
        };
        btnNormal.Click += (s, e) => StartGame("Normal");
        centerFrame.Controls.Add(btnNormal);

        Button btnHard = new Button
        {
            Text = "Modo Difícil\n(Permadeath e Críticos 2x)",
            Font = new Font("Arial", 12, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#f44336"),
            ForeColor = Color.White,
            Size = new Size(300, 60),
            Left = 50,
            Top = 210,
            FlatStyle = FlatStyle.Flat
        };
        btnHard.Click += (s, e) => StartGame("Difícil");
        centerFrame.Controls.Add(btnHard);
    }

    private void StartGame(string chosenMode)
    {
        mode = chosenMode;
        market = RollMarket();
        CreateShopScreen();
    }

    private List<Agent> GetMarketPool()
    {
        var teamNames = team.Select(a => a.Name).ToList();
        return allAgents.Where(a => !teamNames.Contains(a.Name)).ToList();
    }

    private List<Agent> RollMarket()
    {
        var pool = GetMarketPool();
        if (pool.Count == 0) return new List<Agent>();

        var rarityWeights = new Dictionary<int, double> { { 1, 0.40 }, { 2, 0.30 }, { 3, 0.15 }, { 4, 0.10 }, { 5, 0.05 } };
        List<Agent> marketSlots = new List<Agent>();
        int slotsNecessarios = Math.Min(4, pool.Count);

        while (marketSlots.Count < slotsNecessarios)
        {
            var opcoesDisponiveis = pool.Where(a => !marketSlots.Contains(a)).ToList();
            if (opcoesDisponiveis.Count == 0) break;

            var pesos = opcoesDisponiveis.Select(a => rarityWeights.ContainsKey(a.Rarity) ? rarityWeights[a.Rarity] : 0.0).ToList();
            if (pesos.Sum() == 0) pesos = Enumerable.Repeat(1.0, opcoesDisponiveis.Count).ToList();

            // Roleta ponderada simples
            double sum = pesos.Sum();
            double r = random.NextDouble() * sum;
            double currentSum = 0;
            Agent chosen = opcoesDisponiveis.Last();

            for (int i = 0; i < opcoesDisponiveis.Count; i++)
            {
                currentSum += pesos[i];
                if (r <= currentSum)
                {
                    chosen = opcoesDisponiveis[i];
                    break;
                }
            }
            marketSlots.Add(chosen);
        }
        return marketSlots;
    }

    // Redimensiona imagens igual à lógica do Pillow
    private Image GetAgentImage(Agent agent, Size size)
    {
        string cacheKey = $"{agent.Name}_{size.Width}x{size.Height}";
        if (imageCache.ContainsKey(cacheKey)) return imageCache[cacheKey];

        string imgPath = Path.Combine(baseDir, "Imagens", agent.ImageFilename + ".png");
        if (!File.Exists(imgPath)) imgPath = Path.Combine(baseDir, "Imagens", agent.ImageFilename + ".jpg");

        if (File.Exists(imgPath))
        {
            try
            {
                Image rawImg = Image.FromFile(imgPath);
                Bitmap resized = new Bitmap(size.Width, size.Height);
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(rawImg, 0, 0, size.Width, size.Height);
                }
                rawImg.Dispose();
                imageCache[cacheKey] = resized;
                return resized;
            }
            catch { }
        }

        // Placeholder caso a imagem falte
        Bitmap placeholder = new Bitmap(size.Width, size.Height);
        using (Graphics g = Graphics.FromImage(placeholder))
        {
            g.Clear(ColorTranslator.FromHtml("#555555"));
        }
        imageCache[cacheKey] = placeholder;
        return placeholder;
    }

    // ==========================================
    // TELA 2: LOJA / MERCADO
    // ==========================================
    private void CreateShopScreen()
    {
        ClearScreen();

        // Painel Superior
        Panel topFrame = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ColorTranslator.FromHtml("#2d2d2d") };
        mainPanel.Controls.Add(topFrame);

        Label infoLbl = new Label
        {
            Text = $"Missão Atual: Nível {currentLevel}  |  Modo: {mode}  |  💰 Ouro: {gold}g",
            Font = new Font("Arial", 12, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };
        topFrame.Controls.Add(infoLbl);

        Button btnMission = new Button
        {
            Text = "⚔️ IR PARA MISSÃO",
            Font = new Font("Arial", 10, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#ff5722"),
            ForeColor = Color.White,
            Size = new Size(160, 40),
            Location = new Point(topFrame.Width - 180, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat
        };
        btnMission.Click += (s, e) => CheckGoToMission();
        topFrame.Controls.Add(btnMission);

        Button btnReroll = new Button
        {
            Text = $"🔄 Reroll Loja ({rerollCount}g)",
            Font = new Font("Arial", 10, FontStyle.Regular),
            BackColor = ColorTranslator.FromHtml("#795548"),
            ForeColor = Color.White,
            Size = new Size(160, 40),
            Location = new Point(topFrame.Width - 350, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat
        };
        btnReroll.Click += (s, e) => RerollShop();
        topFrame.Controls.Add(btnReroll);

        // Conteineres do Mercado e Time
        TableLayoutPanel mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, Top = 60 };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        mainPanel.Controls.Add(mainLayout);
        mainLayout.BringToFront();

        // Lado Esquerdo: Mercado
        GroupBox marketFrame = new GroupBox { Text = " Mercado (4 slots) ", Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(marketFrame, 0, 0);

        TableLayoutPanel marketGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2, Padding = new Padding(10) };
        marketGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        marketGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        marketGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        marketGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        marketFrame.Controls.Add(marketGrid);

        for (int i = 0; i < market.Count; i++)
        {
            int index = i;
            Agent agent = market[i];

            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#2a2a2a"), Margin = new Padding(5) };
            marketGrid.Controls.Add(card, i % 2, i / 2);

            if (agent == null)
            {
                Label lbl = new Label { Text = " COMPRADO ", Font = new Font("Arial", 11, FontStyle.Italic), ForeColor = Color.Gray, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                card.Controls.Add(lbl);
            }
            else
            {
                Color rarityColor = rarityColors.ContainsKey(agent.Rarity) ? rarityColors[agent.Rarity] : Color.White;

                Label titleLbl = new Label
                {
                    Text = $"{agent.Name}\n({MappingTypes(agent.Type)})",
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    ForeColor = rarityColor,
                    TextAlign = ContentAlignment.TopCenter,
                    Dock = DockStyle.Top,
                    Height = 35
                };
                card.Controls.Add(titleLbl);

                PictureBox pb = new PictureBox
                {
                    Image = GetAgentImage(agent, new Size(90, 90)),
                    Size = new Size(90, 90),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Left = (card.Width - 90) / 2,
                    Top = 40,
                    Anchor = AnchorStyles.Top
                };
                card.Controls.Add(pb);

                string statsStr = mode == "Difícil" ?
                    $"🪙 {agent.Rarity}g\n⚔️ ATK: ?  🛡️ DEF: ?\n🎯 PER: ?  ❤️ HP: {agent.MaxLife}" :
                    $"🪙 {agent.Rarity}g\n⚔️ ATK: {agent.BaseAttack}  🛡️ DEF: {agent.BaseDefense}\n🎯 PER: {agent.BaseSkill}  ❤️ HP: {agent.MaxLife}";

                Label statsLbl = new Label
                {
                    Text = statsStr,
                    Font = new Font("Arial", 9),
                    ForeColor = ColorTranslator.FromHtml("#bbbbbb"),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(0, 135),
                    Width = card.Width,
                    Height = 55,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                card.Controls.Add(statsLbl);

                var synergyStr = agent.GetSynergyName(agent);

                Label synergyLbl = new Label
                {
                    Text = synergyStr, 
                    Font = new Font("Arial", 9, FontStyle.Italic), 
                    ForeColor = ColorTranslator.FromHtml("#ff9800"), 
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(0, 195),
                    Width = card.Width,
                    Height = 30,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                card.Controls.Add(synergyLbl);

                Button btnBuy = new Button
                {
                    Text = "Comprar",
                    BackColor = ColorTranslator.FromHtml("#4caf50"),
                    ForeColor = Color.White,
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    Dock = DockStyle.Bottom,
                    Height = 30,
                    FlatStyle = FlatStyle.Flat
                };
                btnBuy.Click += (s, e) => BuyAgent(index);
                card.Controls.Add(btnBuy);
            }
        }

        // Lado Direito: Sua Equipe
        GroupBox teamFrame = new GroupBox { Text = " Sua Equipe (Mín 1 / Máx 5) ", Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(teamFrame, 1, 0);

        FlowLayoutPanel teamList = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true, Padding = new Padding(10) };
        teamFrame.Controls.Add(teamList);

        if (team.Count == 0)
        {
            Label emptyLbl = new Label { Text = "(Nenhum herói no time)", ForeColor = Color.Gray, Font = new Font("Arial", 11), AutoSize = true, Margin = new Padding(0, 50, 0, 0) };
            teamList.Controls.Add(emptyLbl);
        }

        for (int i = 0; i < team.Count; i++)
        {
            int index = i;
            Agent agent = team[i];

            Panel card = new Panel { Size = new Size(360, 60), BackColor = ColorTranslator.FromHtml("#333333"), Margin = new Padding(0, 5, 0, 5) };
            teamList.Controls.Add(card);

            PictureBox pb = new PictureBox { Image = GetAgentImage(agent, new Size(45, 45)), Size = new Size(45, 45), Location = new Point(5, 7) };
            card.Controls.Add(pb);

            Color rarityColor = rarityColors.ContainsKey(agent.Rarity) ? rarityColors[agent.Rarity] : Color.White;
            Label titleLbl = new Label { Text = $"{agent.Name} ({MappingTypes(agent.Type)})", Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = rarityColor, Location = new Point(55, 5), AutoSize = true };
            card.Controls.Add(titleLbl);

            string hpStr = $"❤️ Vida: {agent.CurrentLife}/{agent.MaxLife}";
            Label statsLbl = new Label { Text = $"{hpStr} | ⚔️ ATK:{agent.BaseAttack} 🛡️ DEF:{agent.BaseDefense} 🎯 PER:{agent.BaseSkill}", Font = new Font("Arial", 8.5f), ForeColor = Color.White, Location = new Point(55, 28), AutoSize = true };
            card.Controls.Add(statsLbl);

            string synergyName = agent.GetSynergyName(agent);
            Label synergyLbl = new Label { Text = $"{synergyName}", Font = new Font("Arial", 8.5f), ForeColor = Color.Orange, Location = new Point(55, 43), AutoSize = true };
            card.Controls.Add(synergyLbl);

            Button btnSell = new Button
            {
                Text = $"+{Math.Ceiling(agent.Rarity / 2.0)}g",
                BackColor = ColorTranslator.FromHtml("#f44336"),
                ForeColor = Color.White,
                Font = new Font("Arial", 8, FontStyle.Bold),
                Size = new Size(55, 30),
                Location = new Point(300, 15),
                FlatStyle = FlatStyle.Flat
            };
            btnSell.Click += (s, e) => SellAgent(index);
            card.Controls.Add(btnSell);
        }
    }

    private void RerollShop()
    {
        if (gold == rerollCount && team.Count < 1)
        {
            MessageBox.Show("É necessário ter moedas suficiente para um personagem!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (gold >= rerollCount)
        {
            gold -= rerollCount;
            rerollCount++;
            market = RollMarket();
            CreateShopScreen();
        }
        else
        {
            MessageBox.Show("Moedas insuficientes para Reroll!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BuyAgent(int idx)
    {
        Agent agent = market[idx];
        if (agent == null) return;

        if (gold >= agent.Rarity)
        {
            if (team.Count < 5)
            {
                gold -= agent.Rarity;
                team.Add(agent);
                market[idx] = null;
                CreateShopScreen();
            }
            else
            {
                MessageBox.Show("Equipe cheia (máximo 5 personagens)!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show("Moedas insuficientes!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SellAgent(int idx)
    {
        Agent removed = team[idx];
        team.RemoveAt(idx);
        gold += (int)Math.Ceiling(removed.Rarity / 2.0);
        CreateShopScreen();
    }

    private void CheckGoToMission()
    {
        if (team.Count > 5)
        {
            MessageBox.Show("Sua equipe excede o limite máximo de 5 personagens!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else if (team.Count == 0)
        {
            MessageBox.Show("Você não pode ir para a missão com o time vazio!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            StartMissionPhase();
        }
    }

    #region Batalha
    // ==========================================
    // TELA 3: TELA DE BATALHA / MISSÃO
    // ==========================================
    private void StartMissionPhase()
    {
        ClearScreen();

        string[] themes = { Agent.Ataque, Agent.Defesa, Agent.Pericia };
        currentTheme = themes[random.Next(themes.Length)];

        Dictionary<int, int> dcTable = mode == "Difícil" ?
            new Dictionary<int, int> { { 1, 13 }, { 2, 16 }, { 3, 19 }, { 4, 23 }, { 5, 27 } } :
            new Dictionary<int, int> { { 1, 10 }, { 2, 13 }, { 3, 16 }, { 4, 20 }, { 5, 25 } };

        int baseDc = dcTable[currentLevel];
        dc = Math.Max(1, baseDc - roundBonuses["DC_Reduction"]);

        shields = team.ToDictionary(a => a.Name, a => roundBonuses["Escudo"]);
        sucessos = 0;
        falhas = 0;
        testeNum = 1;

        Panel battleFrame = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#121212"), Padding = new Padding(20) };
        mainPanel.Controls.Add(battleFrame);

        Label lblMTitle = new Label
        {
            Text = $"🚨 MISSÃO NÍVEL {currentLevel} | TEMA: {currentTheme.ToUpper()} 🚨",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#ff9800"),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 30
        };
        battleFrame.Controls.Add(lblMTitle);

        Label lblDcInfo = new Label
        {
            Text = $"Dificuldade Alvo (DC): {dc}  (DC Base: {baseDc})",
            Font = new Font("Arial", 11),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 25
        };
        battleFrame.Controls.Add(lblDcInfo);

        pbBattleHero = new PictureBox
        {
            Size = new Size(80, 80),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Location = new Point((this.Width - 80) / 2, 80),
            Anchor = AnchorStyles.Top
        };
        battleFrame.Controls.Add(pbBattleHero);

        logTxt = new RichTextBox
        {
            BackColor = ColorTranslator.FromHtml("#1e1e1e"),
            Font = new Font("Consolas", 10),
            ReadOnly = true,
            Location = new Point(20, 180),
            Width = this.Width - 60,
            Height = 240,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        battleFrame.Controls.Add(logTxt);

        btnRoll = new Button
        {
            Text = "🎲 ROLAR DADO (TESTE 1/5)",
            Font = new Font("Arial", 12, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#e91e63"),
            ForeColor = Color.White,
            Size = new Size(400, 50),
            Location = new Point((this.Width - 400) / 2, 440),
            Anchor = AnchorStyles.Top,
            FlatStyle = FlatStyle.Flat
        };
        btnRoll.Click += (s, e) => NextTestRoll();
        battleFrame.Controls.Add(btnRoll);

        AppendLog("Fase preparada. Identificando combatentes viáveis...", Color.White);
        SetupNextCombatantInfo();
    }

    private void AppendLog(string text, Color color)
    {
        logTxt.SelectionStart = logTxt.TextLength;
        logTxt.SelectionLength = 0;
        logTxt.SelectionColor = color;
        logTxt.AppendText(text + "\n");
        logTxt.SelectionColor = logTxt.ForeColor;
        logTxt.ScrollToCaret();
    }

    private void SetupNextCombatantInfo()
    {
        var aliveHeroes = team.Where(a => a.CurrentLife > 0).ToList();
        if (aliveHeroes.Count == 0 || falhas >= 3)
        {
            EndMissionCalculations();
            return;
        }

        bestHero = null;
        bestVal = -999;

        foreach (var a in aliveHeroes)
        {
            int val = a.GetAttr(currentTheme, team) + a.GetSynergyBonus(aliveHeroes);
            if (currentTheme == Agent.Ataque) val += roundBonuses[Agent.Ataque];
            else if (currentTheme == Agent.Defesa) val += roundBonuses[Agent.Defesa];

            if (val > bestVal)
            {
                bestVal = val;
                bestHero = a;
            }
        }

        btnRoll.Text = $"🎲 ROLAR PARA {bestHero.Name.ToUpper()} (Total: {bestVal}) [Teste {testeNum}/5]";
        pbBattleHero.Image = GetAgentImage(bestHero, new Size(80, 80));
    }

    private void NextTestRoll()
    {
        if (btnRoll.Text == "VER RESULTADO FINAL")
        {
            EndMissionCalculations();
            return;
        }

        int d20 = random.Next(1, 21);
        int total = bestVal + d20;

        AppendLog($"\n--- TESTE {testeNum}/5 ({bestHero.Name}) ---", Color.White);
        AppendLog($"Resultado do dado: {d20} | Total: {total} vs Alvo {dc}", Color.White);

        if (mode == "Difícil" && d20 == 20)
        {
            AppendLog("🌟 CRÍTICO POSITIVO! Contando como 2 SUCESSOS!", Color.Green);
            sucessos += 2;
        }
        else if (mode == "Difícil" && d20 == 1)
        {
            AppendLog("💀 CRÍTICO NEGATIVO! 1 Falha Crítica anotada.", Color.Red);
            falhas += 2;
            ApplyGuiDamage(bestHero, currentLevel * 2);
        }
        else if (total >= dc)
        {
            AppendLog("🟢 SUCESSO!", Color.Green);
            sucessos += 1;
        }
        else
        {
            AppendLog("🔴 FALHA!", Color.Red);
            falhas += 1;
            ApplyGuiDamage(bestHero, currentLevel);
        }

        if (!bestHero.Fatigue.ContainsKey(currentTheme)) bestHero.Fatigue[currentTheme] = 0;
        bestHero.Fatigue[currentTheme] += 1;

        testeNum++;

        if (falhas >= 3 || testeNum > 5 || team.Count(a => a.CurrentLife > 0) == 0)
        {
            btnRoll.Text = "VER RESULTADO FINAL";
            btnRoll.BackColor = ColorTranslator.FromHtml("#2196f3");
        }
        else
        {
            SetupNextCombatantInfo();
        }
    }

    private void ApplyGuiDamage(Agent hero, int dano)
    {
        if (shields.ContainsKey(hero.Name) && shields[hero.Name] > 0)
        {
            if (shields[hero.Name] >= dano)
            {
                shields[hero.Name] -= dano;
                AppendLog($"🛡️ Escudo absorveu o golpe completamente. Restante: {shields[hero.Name]}", Color.Cyan);
                return;
            }
            else
            {
                dano -= shields[hero.Name];
                AppendLog($"🛡️ Escudo quebrou! Absorveu parte. {dano} de dano vaza para o HP.", Color.Orange);
                shields[hero.Name] = 0;
            }
        }

        hero.CurrentLife -= dano;
        AppendLog($"💥 Dano sofrido por {hero.Name}: {dano} HP. Atual: {Math.Max(0, hero.CurrentLife)}/{hero.MaxLife}", Color.LightPink);

        if (hero.CurrentLife <= 0)
        {
            if (mode == "Difícil")
            {
                AppendLog($"☠️ PERMADEATH: {hero.Name} morreu e foi expurgado da equipe.", Color.Red);
                team.Remove(hero);
            }
            else
            {
                hero.CurrentLife = 0;
                AppendLog($"💤 NOCAUTE: {hero.Name} desmaiou.", Color.Yellow);
            }
        }
    }

    #endregion

    #region Resultado Final
    // ==========================================
    // TELA 4: RESULTADO FINAL
    // ==========================================
    private void EndMissionCalculations()
    {
        ClearScreen();

        foreach (var a in team) a.ResetFatigue();

        Panel frame = new Panel { Size = new Size(500, 400), BackColor = Color.Transparent };
        frame.Location = new Point((mainPanel.Width - frame.Width) / 2, (mainPanel.Height - frame.Height) / 2);
        frame.Anchor = AnchorStyles.None;
        mainPanel.Controls.Add(frame);

        Label lblTitle = new Label
        {
            Text = $"RESULTADO FINAL: {sucessos} SUCESSOS | {falhas} FALHAS",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Size = new Size(500, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Top = 20
        };
        frame.Controls.Add(lblTitle);

        var aliveHeroes = team.Any(a => a.CurrentLife > 0);

        if (!aliveHeroes)
        {
            string msgText = !aliveHeroes ? "🔴 GAME OVER!\nSua equipe inteira está morta!" : "🔴 GAME OVER!\nSua equipe acumulou 3 ou mais falhas e não conseguiu completar o andar.";
            Label lblRes = new Label { Text = msgText, Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#f44336"), Size = new Size(500, 60), TextAlign = ContentAlignment.MiddleCenter, Top = 100 };
            frame.Controls.Add(lblRes);

            Button btnRestart = new Button { Text = "REINICIAR JOGO", Font = new Font("Arial", 11, FontStyle.Bold), BackColor = ColorTranslator.FromHtml("#f44336"), ForeColor = Color.White, Size = new Size(200, 45), Left = 150, Top = 200, FlatStyle = FlatStyle.Flat };
            btnRestart.Click += (s, e) => RestartEntireGame();
            frame.Controls.Add(btnRestart);
        }

        else
        {
            string txt;
            int g;
            if (sucessos == 3) { txt = "🟡 VITÓRIA PÍRRICA! Vocês avançaram no limite."; g = 2; }
            else if (sucessos == 4) { txt = "🔵 VITÓRIA CONFIANTE! Uma excelente exibição tática."; g = 5; }
            else { txt = "🟢 VITÓRIA ABSOLUTA! Perfeito e lendário!"; g = 8; }

            gold += g;

            Label lblRes = new Label { Text = $"{txt}\nRecompensa da Fase: +{g}g", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#4caf50"), Size = new Size(500, 60), TextAlign = ContentAlignment.MiddleCenter, Top = 100 };
            frame.Controls.Add(lblRes);

            Button btnContinue = new Button { Text = "AVANÇAR PARA DESCANSO", Font = new Font("Arial", 11, FontStyle.Bold), BackColor = ColorTranslator.FromHtml("#4caf50"), ForeColor = Color.White, Size = new Size(220, 45), Left = 140, Top = 200, FlatStyle = FlatStyle.Flat };
            btnContinue.Click += (s, e) => NextLevelRestPhase();
            frame.Controls.Add(btnContinue);
        }
    }
    #endregion

    #region Tela Descanso
    // ==========================================
    // TELA 5: FASE DE DESCANSO
    // ==========================================
    private void NextLevelRestPhase()
    {
        ClearScreen();
        gold += 5; // Salário Base

        int suportes = Math.Min(3, team.Count(a => a.Type == AgentType.Suporte && a.CurrentLife > 0));
        int defensores = Math.Min(3, team.Count(a => a.Type == AgentType.Defensor && a.CurrentLife > 0));
        int lutadores = Math.Min(3, team.Count(a => a.Type == AgentType.Lutador && a.CurrentLife > 0));
        int especialistas = Math.Min(3, team.Count(a => a.Type == AgentType.Especialista && a.CurrentLife > 0));

        Panel frame = new Panel { Size = new Size(500, 450), BackColor = Color.Transparent };
        frame.Location = new Point((mainPanel.Width - frame.Width) / 2, (mainPanel.Height - frame.Height) / 2);
        frame.Anchor = AnchorStyles.None;
        mainPanel.Controls.Add(frame);

        Label lblTitle = new Label { Text = "☕ FASE DE DESCANSO", Font = new Font("Arial", 16, FontStyle.Bold), ForeColor = Color.White, Size = new Size(500, 40), TextAlign = ContentAlignment.MiddleCenter, Top = 10 };
        frame.Controls.Add(lblTitle);

        TextBox logBox = new TextBox { Multiline = true, ReadOnly = true, BackColor = ColorTranslator.FromHtml("#2d2d2d"), ForeColor = Color.White, Font = new Font("Arial", 10), Size = new Size(400, 150), Left = 50, Top = 70, BorderStyle = BorderStyle.None };
        frame.Controls.Add(logBox);

        logBox.AppendText("💰 +5g de salário base depositados.\r\n");

        if (suportes > 0)
        {
            foreach (var a in team)
            {
                if (a.CurrentLife > 0) a.CurrentLife = Math.Min(a.MaxLife, a.CurrentLife + suportes);
            }
            logBox.AppendText($"💚 {suportes} Suporte(s): Time recuperou +{suportes} de Vida.\r\n");
        }

        var deathHeroes = team.Where(w => w.CurrentLife == 0);

        if (deathHeroes.Any())
        {
            foreach(var hero in deathHeroes)
            {
                hero.CurrentLife = 1;
            }
        }

        roundBonuses["Escudo"] = defensores;
        if (defensores > 0) logBox.AppendText($"🛡️ {defensores} Defensor(es): +{defensores} de Escudo para o próximo andar.\r\n");

        roundBonuses[Agent.Ataque] = lutadores;
        roundBonuses[Agent.Defesa] = lutadores;
        if (lutadores > 0) logBox.AppendText($"🔥 {lutadores} Lutador(es): +{lutadores} de Fúria nos testes seguintes.\r\n");

        roundBonuses["DC_Reduction"] = especialistas;
        if (especialistas > 0) logBox.AppendText($"🧠 {especialistas} Especialista(s): DC alvo reduzida em -{especialistas}.\r\n");

        currentLevel++;

        Button btnNext = new Button { Font = new Font("Arial", 11, FontStyle.Bold), BackColor = ColorTranslator.FromHtml("#2196f3"), ForeColor = Color.White, Size = new Size(200, 45), Left = 150, Top = 260, FlatStyle = FlatStyle.Flat };

        if (currentLevel > 5)
        {
            btnNext.Text = "VER TELA DE VITÓRIA 🎉";
            btnNext.Click += (s, e) => ShowVictoryScreen();
        }
        else
        {
            btnNext.Text = "IR PARA A LOJA 🛒";
            btnNext.Click += (s, e) => {
                market = RollMarket();
                rerollCount = 1;
                CreateShopScreen();
            };
        }
        frame.Controls.Add(btnNext);
    }

    // ==========================================
    // TELA 6: VITÓRIA FINAL
    // ==========================================
    private void ShowVictoryScreen()
    {
        ClearScreen();
        Panel frame = new Panel { Size = new Size(500, 300), BackColor = Color.Transparent };
        frame.Location = new Point((mainPanel.Width - frame.Width) / 2, (mainPanel.Height - frame.Height) / 2);
        frame.Anchor = AnchorStyles.None;
        mainPanel.Controls.Add(frame);

        Label lblWin = new Label { Text = "🎉 PARABÉNS! 🎉", Font = new Font("Arial", 24, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#ff9800"), Size = new Size(500, 50), TextAlign = ContentAlignment.MiddleCenter, Top = 20 };
        frame.Controls.Add(lblWin);

        Label lblSub = new Label { Text = "VOCÊ SUPEROU OS 5 NÍVEIS E ZEROU O JOGO!", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, Size = new Size(500, 40), TextAlign = ContentAlignment.MiddleCenter, Top = 90 };
        frame.Controls.Add(lblSub);

        Button btnAgain = new Button { Text = "JOGAR NOVAMENTE", Font = new Font("Arial", 11, FontStyle.Bold), BackColor = ColorTranslator.FromHtml("#4caf50"), ForeColor = Color.White, Size = new Size(200, 45), Left = 150, Top = 160, FlatStyle = FlatStyle.Flat };
        btnAgain.Click += (s, e) => RestartEntireGame();
        frame.Controls.Add(btnAgain);
    }

    private void RestartEntireGame()
    {
        ResetGameState();
        CreateModeSelectionScreen();
    }
}
    #endregion