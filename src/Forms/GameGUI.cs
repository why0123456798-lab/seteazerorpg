using CsvHelper;
using Microsoft.Data.Sqlite;
using RPGBattleMaker.Data;
using RPGBattleMaker.Data.Interface;
using RPGBattleMaker.Infrastructure;
using RPGBattleMaker.Infrastructure.Interface;
using RPGBattleMaker.Models;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;

public class GameGUI : Form
{
    #region Private List
    private readonly IDbContext _dbContext;
    private readonly IAgentService _agentService;
    private readonly IGameService _gameService;
    private readonly IEventService _eventService;

    private List<Item> itemShop = new List<Item>();
    private List<Item> purchasedItems = new List<Item>();

    private List<Agent> allAgents = new List<Agent>();
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
    private int extraDc = 0;
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
    private Label lblHeroStats;
    private PictureBox pbBattleHero;
    #endregion

    #region Constructor
    public GameGUI(IDbContext dbContext, IAgentService agentService, IGameService gameService, IEventService eventService)
    {
        _agentService = agentService;
        _gameService = gameService;
        _dbContext = dbContext;

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

        mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        this.Controls.Add(mainPanel);

        ResetGameState();
        CreateModeSelectionScreen();
        _eventService = eventService;
    }
    #endregion

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

        #region Populate Data from DB
        _dbContext.InitializeDatabase().Wait();
        _agentService.GetAllHeroes(allAgents).Wait();

        itemShop.Clear();
        purchasedItems.Clear();
        #endregion
    }

    private void ClearScreen()
    {
        mainPanel.Controls.Clear();
    }

    # region TELA 1: SELEÇÃO DE MODO
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
        btnNormal.Click += (s, e) => StartGame("Normal").Wait();
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
        btnHard.Click += (s, e) => StartGame("Difícil").Wait();
        centerFrame.Controls.Add(btnHard);
    }

    private async Task StartGame(string chosenMode)
    {
        mode = chosenMode;
        market = _gameService.RollMarket(team, allAgents, currentLevel);
        itemShop = RollItemShop(currentLevel);
        await CreateShopScreen();
    }
    #endregion

    #region TELA 2: LOJA / MERCADO
    private async Task CreateShopScreen()
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
        btnMission.Click += (s, e) => CheckGoToMission().Wait();
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
        btnReroll.Click += (s, e) => RerollShop().Wait();
        topFrame.Controls.Add(btnReroll);

        Button btnRestart = new Button
        {
            Text = $"🔄 Recomeçar",
            Font = new Font("Arial", 10, FontStyle.Regular),
            BackColor = ColorTranslator.FromHtml("#4287f5"),
            ForeColor = Color.White,
            Size = new Size(160, 40),
            Location = new Point(topFrame.Width - 520, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat
        };
        btnRestart.Click += (s, e) => RestartEntireGame();
        topFrame.Controls.Add(btnRestart);

        // Conteineres do Mercado e Time
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 3,
            Top = 60
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f)); // Mercado Heróis
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f)); // Loja de Itens  ← NOVO
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // Time
        mainPanel.Controls.Add(mainLayout);
        mainLayout.BringToFront();

        // Lado Esquerdo: Mercado
        GroupBox marketFrame = new GroupBox { Text = " Mercado (4 slots) ", Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(marketFrame, 0, 0);

        GroupBox itemFrame = new GroupBox
        {
            Text = " 🧪 Loja de Itens ",
            Font = new Font("Arial", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#ff9800"),
            Dock = DockStyle.Fill
        };
        mainLayout.Controls.Add(itemFrame, 1, 0);

        FlowLayoutPanel itemList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            Padding = new Padding(8)
        };
        itemFrame.Controls.Add(itemList);

        // Cards de itens
        for (int i = 0; i < itemShop.Count; i++)
        {
            int itemIdx = i;
            Item shopItem = itemShop[i];

            Panel itemCard = new Panel
            {
                Size = new Size(190, 100),
                BackColor = ColorTranslator.FromHtml("#2a2a2a"),
                Margin = new Padding(0, 5, 0, 5)
            };
            itemList.Controls.Add(itemCard);

            if (shopItem == null)
            {
                Label soldLbl = new Label
                {
                    Text = "✅ COMPRADO",
                    Font = new Font("Arial", 10, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                itemCard.Controls.Add(soldLbl);
            }
            else
            {
                Color rarityColor = rarityColors.ContainsKey(shopItem.Rarity) ? rarityColors[shopItem.Rarity] : Color.White;

                Label nameLbl = new Label
                {
                    Text = $"{shopItem.Emoji} {shopItem.Name}",
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    ForeColor = rarityColor,
                    Location = new Point(5, 5),
                    Size = new Size(180, 20)
                };
                itemCard.Controls.Add(nameLbl);

                Label descLbl = new Label
                {
                    Text = shopItem.Description,
                    Font = new Font("Arial", 8),
                    ForeColor = ColorTranslator.FromHtml("#cccccc"),
                    Location = new Point(5, 25),
                    Size = new Size(180, 35),
                    AutoEllipsis = true
                };
                itemCard.Controls.Add(descLbl);

                Button btnBuyItem = new Button
                {
                    Text = $"🪙 {shopItem.Cost}g  Comprar",
                    BackColor = ColorTranslator.FromHtml("#795548"),
                    ForeColor = Color.White,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    Dock = DockStyle.Bottom,
                    Height = 28,
                    FlatStyle = FlatStyle.Flat
                };
                btnBuyItem.Click += (s, e) => BuyItem(itemIdx).Wait();
                itemCard.Controls.Add(btnBuyItem);
            }
        }

        // Exibe itens já comprados nesta run
        if (purchasedItems.Count > 0)
        {
            Label ownedTitle = new Label
            {
                Text = "── Itens Ativos ──",
                Font = new Font("Arial", 8, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 2)
            };
            itemList.Controls.Add(ownedTitle);

            foreach (var owned in purchasedItems)
            {
                Label ownedLbl = new Label
                {
                    Text = $"{owned.Emoji} {owned.Name}",
                    Font = new Font("Arial", 8),
                    ForeColor = ColorTranslator.FromHtml("#4caf50"),
                    AutoSize = true,
                    Margin = new Padding(2, 1, 0, 1)
                };
                itemList.Controls.Add(ownedLbl);
            }
        }

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
                Label lbl = new Label { Text = "✅ COMPRADO ", Font = new Font("Arial", 11, FontStyle.Italic), ForeColor = Color.Gray, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                card.Controls.Add(lbl);
            }
            else
            {
                Color rarityColor = rarityColors.ContainsKey(agent.Rarity) ? rarityColors[agent.Rarity] : Color.White;

                Label titleLbl = new Label
                {
                    Text = $"{agent.Name}\n({AgentHelper.MappingTypes(agent.Type)})",
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    ForeColor = rarityColor,
                    TextAlign = ContentAlignment.TopCenter,
                    Dock = DockStyle.Top,
                    Height = 35
                };
                card.Controls.Add(titleLbl);

                PictureBox pb = new PictureBox
                {
                    Image = await _agentService.GetAgentImage(agent, new Size(90, 90)),
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

                var synergyStr = await _agentService.GetSynergyName(agent);

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
                btnBuy.Click += (s, e) => BuyAgent(index).Wait();
                card.Controls.Add(btnBuy);
            }
        }

        // Lado Direito: Sua Equipe
        GroupBox teamFrame = new GroupBox { Text = " Sua Equipe (Mín 1 / Máx 5) ", Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(teamFrame, 2, 0);

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

            PictureBox pb = new PictureBox { Image = await _agentService.GetAgentImage(agent, new Size(45, 45)), Size = new Size(45, 45), Location = new Point(5, 7) };
            card.Controls.Add(pb);

            Color rarityColor = rarityColors.ContainsKey(agent.Rarity) ? rarityColors[agent.Rarity] : Color.White;
            Label titleLbl = new Label { Text = $"{agent.Name} ({AgentHelper.MappingTypes(agent.Type)})", Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = rarityColor, Location = new Point(55, 5), AutoSize = true };
            card.Controls.Add(titleLbl);

            string hpStr = $"❤️ Vida: {agent.CurrentLife}/{agent.MaxLife}";
            Label statsLbl = new Label { Text = $"{hpStr} | ⚔️ ATK:{agent.BaseAttack} 🛡️ DEF:{agent.BaseDefense} 🎯 PER:{agent.BaseSkill}", Font = new Font("Arial", 8.5f), ForeColor = Color.White, Location = new Point(55, 28), AutoSize = true };
            card.Controls.Add(statsLbl);

            string synergyName = await _agentService.GetSynergyName(agent);
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
            btnSell.Click += (s, e) => SellAgent(index).Wait();
            card.Controls.Add(btnSell);
        }
    }

    private async Task RerollShop()
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
            market = _gameService.RollMarket(team, allAgents, currentLevel);
            await CreateShopScreen();
        }
        else
        {
            MessageBox.Show("Moedas insuficientes para Reroll!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task BuyAgent(int idx)
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
                await CreateShopScreen();
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

    private async Task SellAgent(int idx)
    {
        Agent removed = team[idx];
        team.RemoveAt(idx);
        gold += (int)Math.Ceiling(removed.Rarity / 2.0);
        await CreateShopScreen();
    }

    private async Task CheckGoToMission()
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
            await StartMissionPhase();
        }
    }
    #endregion

    #region TELA 3: TELA DE BATALHA / MISSÃO
    private async Task StartMissionPhase()
    {
        ClearScreen();

        string[] themes = { Agent.Ataque, Agent.Defesa, Agent.Pericia };
        currentTheme = themes[random.Next(themes.Length)];

        Dictionary<int, int> dcTable = mode == "Difícil" ?
            new Dictionary<int, int> { { 1, 13 }, { 2, 16 }, { 3, 19 }, { 4, 23 }, { 5, 27 } } :
            new Dictionary<int, int> { { 1, 10 }, { 2, 13 }, { 3, 16 }, { 4, 20 }, { 5, 25 } };

        int baseDc = dcTable[currentLevel] + extraDc;
        dc = Math.Max(1, baseDc - roundBonuses["DC_Reduction"]);

        shields = team.ToDictionary(a => a.Name, a => roundBonuses["Escudo"]);
        sucessos = 0;
        falhas = 0;
        testeNum = 1;

        Panel battleFrame = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#121212"), Padding = new Padding(20) };
        mainPanel.Controls.Add(battleFrame);

        // --- CONTROLES COM DOCK TOP (A ordem de adição importa!) ---

        // 1º O Título Principal da Missão
        Label lblMTitle = new Label
        {
            Text = $"🚨 MISSÃO NÍVEL {currentLevel} | TEMA: {currentTheme.ToUpper()} 🚨",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#ff9800"),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 35
        };
        battleFrame.Controls.Add(lblMTitle);

        // 2º Informações de Dificuldade (DC)
        Label lblDcInfo = new Label
        {
            Text = $"Dificuldade Alvo (DC): {dc}  (DC Base: {baseDc})",
            Font = new Font("Arial", 11),
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 25
        };
        battleFrame.Controls.Add(lblDcInfo);

        // 3º Status do Herói Atual (Agora bem posicionado logo abaixo da DC)
        lblHeroStats = new Label
        {
            Text = $"{currentTheme}: ? | Fadiga: ?",
            Font = new Font("Arial", 11, FontStyle.Italic),
            ForeColor = ColorTranslator.FromHtml("#4caf50"), // Um verde para dar destaque aos atributos
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 25
        };
        battleFrame.Controls.Add(lblHeroStats);

        // --- CONTROLES COM POSICIONAMENTO FIXO / ANCHOR ---

        // Imagem do Herói - Empurrada um pouco mais para baixo (Top = 120) para dar espaço às labels acima
        pbBattleHero = new PictureBox
        {
            Size = new Size(80, 80),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Location = new Point((this.Width - 80) / 2, 120),
            Anchor = AnchorStyles.Top
        };
        battleFrame.Controls.Add(pbBattleHero);

        // Log de Batalha - Ajustado o Top para 210 para não colidir com o PictureBox
        logTxt = new RichTextBox
        {
            BackColor = ColorTranslator.FromHtml("#1e1e1e"),
            Font = new Font("Consolas", 10),
            ReadOnly = true,
            Location = new Point(20, 210),
            Width = this.Width - 60,
            Height = 210,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        battleFrame.Controls.Add(logTxt);

        // Botão de Rolar Dados
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
        btnRoll.Click += (s, e) => NextTestRoll().Wait();
        battleFrame.Controls.Add(btnRoll);

        await AppendLog("Fase preparada. Identificando combatentes viáveis...", Color.White);
        await SetupNextCombatantInfo();
    }

    private async Task AppendLog(string text, Color color)
    {
        logTxt.SelectionStart = logTxt.TextLength;
        logTxt.SelectionLength = 0;
        logTxt.SelectionColor = color;
        logTxt.AppendText(text + "\n");
        logTxt.SelectionColor = logTxt.ForeColor;
        logTxt.ScrollToCaret();
    }

    private async Task SetupNextCombatantInfo()
    {
        var aliveHeroes = team.Where(a => a.CurrentLife > 0).ToList();
        if (aliveHeroes.Count == 0 || falhas >= 3)
        {
            await EndMissionCalculations();
            return;
        }

        bestHero = null;
        bestVal = -999;

        foreach (var a in aliveHeroes)
        {
            int val = a.GetAttr(currentTheme, team) + (await _agentService.GetSynergyBonus(a, aliveHeroes));
            if (currentTheme == Agent.Ataque) val += roundBonuses[Agent.Ataque];
            else if (currentTheme == Agent.Defesa) val += roundBonuses[Agent.Defesa];

            if (val > bestVal)
            {
                bestVal = val;
                bestHero = a;
            }
        }

        var setValueExibition = currentTheme == Agent.Ataque ? bestHero.BaseAttack : currentTheme == Agent.Defesa ? bestHero.BaseDefense : bestHero.BaseSkill;

        lblHeroStats.Text = $"{currentTheme}: {setValueExibition} Fadiga: {bestHero.Fatigue.FirstOrDefault(f => f.Key == currentTheme).Value}";
        btnRoll.Text = $"🎲 ROLAR PARA {bestHero.Name.ToUpper()} (Total: {bestVal}) [Teste {testeNum}/5]";
        pbBattleHero.Image = await _agentService.GetAgentImage(bestHero, new Size(80, 80));
    }

    private async Task NextTestRoll()
    {
        if (btnRoll.Text == "VER RESULTADO FINAL")
        {
            await EndMissionCalculations();
            return;
        }

        int d20 = random.Next(1, 21);
        int total = bestVal + d20;

        await AppendLog($"\n--- TESTE {testeNum}/5 ({bestHero.Name}) ---", Color.White);
        await AppendLog($"Resultado do dado: {d20} | Total: {total} vs Alvo {dc}", Color.White);

        if (mode == "Difícil" && d20 == 20)
        {
            await AppendLog("🌟 CRÍTICO POSITIVO! Contando como 2 SUCESSOS!", Color.Green);
            sucessos += 2;
        }
        else if (mode == "Difícil" && d20 == 1)
        {
            await AppendLog("💀 CRÍTICO NEGATIVO! 1 Falha Crítica anotada.", Color.Red);
            falhas += 2;
            await ApplyGuiDamage(bestHero, currentLevel * 2);
        }
        else if (total >= dc)
        {
            await AppendLog("🟢 SUCESSO!", Color.Green);
            sucessos += 1;
        }
        else
        {
            await AppendLog("🔴 FALHA!", Color.Red);
            falhas += 1;
            await ApplyGuiDamage(bestHero, currentLevel);
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
            await SetupNextCombatantInfo();
        }
    }

    private async Task ApplyGuiDamage(Agent hero, int dano)
    {
        if (shields.ContainsKey(hero.Name) && shields[hero.Name] > 0)
        {
            if (shields[hero.Name] >= dano)
            {
                shields[hero.Name] -= dano;
                await AppendLog($"🛡️ Escudo absorveu o golpe completamente. Restante: {shields[hero.Name]}", Color.Cyan);
                return;
            }
            else
            {
                dano -= shields[hero.Name];
                await AppendLog($"🛡️ Escudo quebrou! Absorveu parte. {dano} de dano vaza para o HP.", Color.Orange);
                shields[hero.Name] = 0;
            }
        }

        hero.CurrentLife -= dano;
        await AppendLog($"💥 Dano sofrido por {hero.Name}: {dano} HP. Atual: {Math.Max(0, hero.CurrentLife)}/{hero.MaxLife}", Color.LightPink);

        if (hero.CurrentLife <= 0)
        {
            if (mode == "Difícil")
            {
                await AppendLog($"☠️ PERMADEATH: {hero.Name} morreu e foi expurgado da equipe.", Color.Red);
                team.Remove(hero);
            }
            else
            {
                hero.CurrentLife = 0;
                await AppendLog($"💤 NOCAUTE: {hero.Name} desmaiou.", Color.Yellow);
            }
        }
    }

    #endregion

    #region TELA 4: Resultado Final
    private async Task EndMissionCalculations()
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

        if (!aliveHeroes || falhas >= 3)
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
            extraDc = 0;

            Label lblRes = new Label { Text = $"{txt}\nRecompensa da Fase: +{g}g", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#4caf50"), Size = new Size(500, 60), TextAlign = ContentAlignment.MiddleCenter, Top = 100 };
            frame.Controls.Add(lblRes);

            Button btnContinue = new Button { Text = "AVANÇAR PARA DESCANSO", Font = new Font("Arial", 11, FontStyle.Bold), BackColor = ColorTranslator.FromHtml("#4caf50"), ForeColor = Color.White, Size = new Size(220, 45), Left = 140, Top = 200, FlatStyle = FlatStyle.Flat };
            btnContinue.Click += (s, e) => NextLevelRestPhase().Wait();
            frame.Controls.Add(btnContinue);
        }
    }
    #endregion

    #region Tela 4.5 Events
    private async Task NextEventPhase(Event gameEvent)
    {
        ClearScreen();

        // Painel de fundo centralizado
        Panel frame = new Panel
        {
            Size = new Size(600, 480),
            BackColor = ColorTranslator.FromHtml("#1a1a2e"),
        };
        frame.Location = new Point(
            (mainPanel.Width - frame.Width) / 2,
            (mainPanel.Height - frame.Height) / 2
        );
        frame.Anchor = AnchorStyles.None;
        mainPanel.Controls.Add(frame);

        // Borda decorativa (painel interno levemente diferente)
        Panel innerBorder = new Panel
        {
            Size = new Size(594, 474),
            Location = new Point(3, 3),
            BackColor = ColorTranslator.FromHtml("#16213e"),
        };
        frame.Controls.Add(innerBorder);

        // ── Cabeçalho ──────────────────────────────────────────────
        Label lblBadge = new Label
        {
            Text = "⚡ EVENTO ALEATÓRIO",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#ff9800"),
            Size = new Size(580, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(7, 10)
        };
        innerBorder.Controls.Add(lblBadge);

        Label lblTitle = new Label
        {
            Text = gameEvent.Name,
            Font = new Font("Arial", 17, FontStyle.Bold),
            ForeColor = Color.White,
            Size = new Size(560, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(7, 38)
        };
        innerBorder.Controls.Add(lblTitle);

        // Separador visual
        Panel separator = new Panel
        {
            Size = new Size(540, 2),
            Location = new Point(27, 82),
            BackColor = ColorTranslator.FromHtml("#ff9800")
        };
        innerBorder.Controls.Add(separator);

        // Descrição / narrativa do evento
        Label lblDesc = new Label
        {
            Text = gameEvent.Description,
            Font = new Font("Arial", 10),
            ForeColor = ColorTranslator.FromHtml("#cccccc"),
            Size = new Size(540, 80),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(27, 92)
        };
        innerBorder.Controls.Add(lblDesc);

        // ── 3 Botões de Opção ──────────────────────────────────────
        var optionColors = new[]
        {
        ColorTranslator.FromHtml("#c62828"), // Opção A — vermelho / ousada
        ColorTranslator.FromHtml("#1565c0"), // Opção B — azul   / equilibrada
        ColorTranslator.FromHtml("#2e7d32"), // Opção C — verde  / segura
    };

        var optionTexts = new[] { gameEvent.OptionA, gameEvent.OptionB, gameEvent.OptionC };

        for (int i = 0; i < 3; i++)
        {
            int chosenIndex = i;

            string optionText = optionTexts[i] ?? $"Opção {i + 1}";

            Button btnOption = new Button
            {
                Text = $"{i + 1}. {optionText}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = optionColors[i],
                ForeColor = Color.White,
                Size = new Size(520, 52),
                Location = new Point(37, 190 + i * 66),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            btnOption.FlatAppearance.BorderSize = 0;

            btnOption.Click += async (s, e) =>
            {
                // Desativa todos os botões para evitar duplo clique
                foreach (Control ctrl in innerBorder.Controls)
                    if (ctrl is Button b) b.Enabled = false;

                // Chama o serviço com a opção escolhida pelo jogador
                var result = _eventService.GetEventResult(gameEvent, chosenIndex, team);

                // Exibe o resultado na mesma tela
                await ShowEventResult(innerBorder, result);
            };

            innerBorder.Controls.Add(btnOption);
        }
    }

    /// <summary>
    /// Mostra o resultado da opção escolhida sobre a tela do evento,
    /// substituindo os botões por um painel de resultado + botão de continuar.
    /// </summary>
    private async Task ShowEventResult(Panel innerBorder, EventResult result)
    {
        // Remove os 3 botões de opção (mantém título, badge, separador e descrição)
        var buttonsToRemove = innerBorder.Controls
            .OfType<Button>()
            .ToList();
        foreach (var btn in buttonsToRemove)
            innerBorder.Controls.Remove(btn);

        // Painel de resultado
        Panel resultPanel = new Panel
        {
            Size = new Size(520, 160),
            Location = new Point(37, 190),
            BackColor = ColorTranslator.FromHtml("#0d1117"),
            Padding = new Padding(12)
        };
        innerBorder.Controls.Add(resultPanel);

        // Ícone de resultado (success/fail/neutral pode vir do EventResult)
        bool isPositive = result.IsPositive; // true = bom, false = ruim
        string resultIcon = isPositive ? "✅" : "⚠️";
        Color resultColor = isPositive
            ? ColorTranslator.FromHtml("#4caf50")
            : ColorTranslator.FromHtml("#f44336");

        Label lblResultTitle = new Label
        {
            Text = $"{resultIcon}  {result.Title}",   // Título do resultado (ex: "Você comprou o item!")
            Font = new Font("Arial", 12, FontStyle.Bold),
            ForeColor = resultColor,
            Size = new Size(496, 28),
            Location = new Point(12, 10),
            TextAlign = ContentAlignment.MiddleLeft
        };
        resultPanel.Controls.Add(lblResultTitle);

        Label lblResultDesc = new Label
        {
            Text = result.Description,   // Descrição do efeito (ex: "+3g adicionados ao inventário")
            Font = new Font("Arial", 10),
            ForeColor = ColorTranslator.FromHtml("#dddddd"),
            Size = new Size(496, 80),
            Location = new Point(12, 44),
            TextAlign = ContentAlignment.TopLeft
        };
        resultPanel.Controls.Add(lblResultDesc);

        // Botão continuar — prossegue para a fase de descanso
        Button btnContinue = new Button
        {
            Text = "CONTINUAR →",
            Font = new Font("Arial", 11, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#37474f"),
            ForeColor = Color.White,
            Size = new Size(200, 42),
            Location = new Point(187, 390),
            FlatStyle = FlatStyle.Flat
        };
        btnContinue.FlatAppearance.BorderSize = 0;

        // Ao continuar, aplica efeitos do resultado e vai para o descanso
        btnContinue.Click += async (s, e) =>
        {
            await ApplyEventResult(result);
            await ContinueToRestPhase();
        };

        innerBorder.Controls.Add(btnContinue);
    }

    /// <summary>
    /// Aplica os efeitos mecânicos do resultado do evento no estado do jogo.
    /// Adapte os campos de EventResult conforme sua implementação de IEventService.
    /// </summary>
    private async Task ApplyEventResult(EventResult result)
    {
        var heroAffected = team.First(a => a.Id == result.AffectedAgentId);
        if (result.HpBonus != 0)
        {
            heroAffected.CurrentLife += result.HpBonus;
            if(heroAffected.CurrentLife < 0)
                heroAffected.CurrentLife = 0;
        }

        if (result.HpBonusMaxLife != 0)
        {
            heroAffected.MaxLife += result.HpBonusMaxLife;
            if (heroAffected.MaxLife < 0)
                heroAffected.MaxLife = 0;
        }

        if(result.GlobalShield != 0)
        {
            foreach (var hero in team)
            {
                if (!shields.ContainsKey(hero.Name))
                    shields[hero.Name] = 0;
                shields[hero.Name] += result.GlobalShield;
            }
        }

        if (result.GlobalCure != 0)
        {
            foreach(var hero in team)
            {
                hero.CurrentLife += result.GlobalCure;
                if (hero.CurrentLife < 0)
                    hero.CurrentLife = 0;
            }
        }

        if (result.PermanentAttack != 0)
        {
            heroAffected.BaseAttack += result.PermanentAttack;
        }

        if(result.ExtraDc != 0)
        {
            extraDc = result.ExtraDc;
        }

        if (result.TemporarySkillBonus != 0)
        {
            heroAffected.TemporarySkillBonus += result.TemporarySkillBonus;
        }

        if (result.TemporarySkillBonusSupport != 0)
        {
            foreach(var hero in team.Where(w => w.Type == AgentType.Suporte))
            {
                hero.TemporarySkillBonus += result.TemporarySkillBonusSupport;
            }
        }

        if (result.GoldBonus != 0)
        {
            gold += result.GoldBonus;
        }
    }

    /// <summary>
    /// Wrapper que leva para a fase de descanso sem re-checar evento
    /// (o evento já foi resolvido nesta chamada).
    /// </summary>
    private async Task ContinueToRestPhase()
    {
        await ExecuteRestPhase(); // método interno extraído abaixo
    }

#endregion

    // =============================================================
    // REFATORAÇÃO de NextLevelRestPhase()
    // Extraia a lógica de descanso para ExecuteRestPhase() e ajuste
    // NextLevelRestPhase() para chamar o evento ANTES do descanso.
    // =============================================================

    #region TELA 5: Fase Descanso

    private async Task NextLevelRestPhase()
    {
        var randomEvent = await _eventService.RandomEvent();

        if (randomEvent is not null)
        {
            // Exibe a tela de evento; ao fechar, ela chama ContinueToRestPhase → ExecuteRestPhase
            await NextEventPhase(randomEvent);
        }
        else
        {
            // Sem evento: vai direto para o descanso
            await ExecuteRestPhase();
        }
    }

    /// <summary>
    /// Tela de descanso em si (o que estava em NextLevelRestPhase antes).
    /// Separado para que tanto o fluxo normal quanto o pós-evento possam chamá-la.
    /// </summary>
    private async Task ExecuteRestPhase()
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

        Label lblTitle = new Label
        {
            Text = "☕ FASE DE DESCANSO",
            Font = new Font("Arial", 16, FontStyle.Bold),
            ForeColor = Color.White,
            Size = new Size(500, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Top = 10
        };
        frame.Controls.Add(lblTitle);

        TextBox logBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BackColor = ColorTranslator.FromHtml("#2d2d2d"),
            ForeColor = Color.White,
            Font = new Font("Arial", 10),
            Size = new Size(400, 150),
            Left = 50,
            Top = 70,
            BorderStyle = BorderStyle.None
        };
        frame.Controls.Add(logBox);

        logBox.AppendText("💰 +5g de salário base depositados.\r\n");

        if (suportes > 0)
        {
            foreach (var a in team)
                if (a.CurrentLife > 0) a.CurrentLife = Math.Min(a.MaxLife, a.CurrentLife + suportes);
            logBox.AppendText($"💚 {suportes} Suporte(s): Time recuperou +{suportes} de Vida.\r\n");
        }

        foreach (var hero in team.Where(w => w.CurrentLife == 0))
            hero.CurrentLife = 1;

        roundBonuses["Escudo"] = defensores;
        if (defensores > 0)
            logBox.AppendText($"🛡️ {defensores} Defensor(es): +{defensores} de Escudo para o próximo andar.\r\n");

        roundBonuses[Agent.Ataque] = lutadores;
        roundBonuses[Agent.Defesa] = lutadores;
        if (lutadores > 0)
            logBox.AppendText($"🔥 {lutadores} Lutador(es): +{lutadores} de Fúria nos testes seguintes.\r\n");

        roundBonuses["DC_Reduction"] = especialistas;
        if (especialistas > 0)
            logBox.AppendText($"🧠 {especialistas} Especialista(s): DC alvo reduzida em -{especialistas}.\r\n");

        currentLevel++;

        Button btnNext = new Button
        {
            Font = new Font("Arial", 11, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#2196f3"),
            ForeColor = Color.White,
            Size = new Size(200, 45),
            Left = 150,
            Top = 260,
            FlatStyle = FlatStyle.Flat
        };

        if (currentLevel > 5)
        {
            btnNext.Text = "VER TELA DE VITÓRIA 🎉";
            btnNext.Click += (s, e) => ShowVictoryScreen();
        }
        else
        {
            btnNext.Text = "IR PARA A LOJA 🛒";
            btnNext.Click += (s, e) =>
            {
                market = _gameService.RollMarket(team, allAgents, currentLevel);
                itemShop = RollItemShop(currentLevel);
                rerollCount = 1;
                CreateShopScreen().Wait();
            };
        }
        frame.Controls.Add(btnNext);
    }

    private List<Item> RollItemShop(int level)
    {
        // Peso de raridade cresce com o nível (igual ao RollMarket de heróis)
        var pool = ItemCatalog.AllItems
            .Where(i => i.Rarity <= Math.Min(level + 1, 5))
            .ToList();

        var result = new List<Item>();
        while (result.Count < 3 && pool.Count > 0)
        {
            int idx = random.Next(pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }

    private void ApplyItemEffect(Item item)
    {
        switch (item.Effect)
        {
            case ItemEffect.BonusAtaque:
                roundBonuses[Agent.Ataque] += item.EffectValue;
                break;
            case ItemEffect.BonusDefesa:
                roundBonuses[Agent.Defesa] += item.EffectValue;
                break;
            case ItemEffect.BonusPericia:
                // Pericia não estava no roundBonuses; adicione no ResetGameState se quiser.
                // Por ora, aplica direto nos heróis.
                foreach (var hero in team) hero.BaseSkill += item.EffectValue;
                break;
            case ItemEffect.BonusHP:
                foreach (var hero in team)
                {
                    hero.MaxLife += item.EffectValue;
                    hero.CurrentLife = Math.Min(hero.CurrentLife + item.EffectValue, hero.MaxLife);
                }
                break;
            case ItemEffect.BonusEscudo:
                roundBonuses["Escudo"] += item.EffectValue;
                break;
            case ItemEffect.ReducaoDC:
                roundBonuses["DC_Reduction"] += item.EffectValue;
                break;
        }
    }

    private async Task BuyItem(int idx)
    {
        Item item = itemShop[idx];
        if (item == null) return;

        if (gold >= item.Cost)
        {
            gold -= item.Cost;
            ApplyItemEffect(item);
            purchasedItems.Add(item);
            itemShop[idx] = null;
            await CreateShopScreen();
        }
        else
        {
            MessageBox.Show("Moedas insuficientes!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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