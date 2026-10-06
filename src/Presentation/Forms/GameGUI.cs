using CsvHelper;
using Microsoft.Data.Sqlite;
using RPGBattleMaker.Application.Interfaces;
using RPGBattleMaker.Application.Services;
using RPGBattleMaker.Domain.Entities;
using RPGBattleMaker.Domain.Helpers;
using RPGBattleMaker.Infrastructure.Database;
using RPGBattleMaker.Presentation.Controls;

namespace RPGBattleMaker.Presentation.Forms;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;

public class GameGUI : Form
{
    #region Private List
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly IAgentService _agentService;
    private readonly IGameService _gameService;
    private readonly IEventService _eventService;
    private readonly IBattleNarrator _battleNarrator;

    private List<Item> itemShop = new List<Item>();
    private List<Item> purchasedItems = new List<Item>();

    private List<Agent> allAgents = new List<Agent>();
    private readonly Random random = Random.Shared;

    // Estado do Jogo
    private List<Agent> team = new List<Agent>();
    private int gold;
    private int currentLevel;
    private string mode = "Difícil";
    private Dictionary<string, int> roundBonuses = new Dictionary<string, int>();
    private List<Agent> market = new List<Agent>();
    private int rerollCount = 1;

    private readonly List<string> battleStoryHistory = new();
    private readonly List<string> eventHistory = new();
    private Task<BattleStory?>? initialBattleStoryTask;
    private BattleStory? lastBattleConclusion;

    // Estado da Batalha/Missão
    private string currentTheme = Agent.Ataque;
    private int dc;
    private int extraDc = 0;
    private Dictionary<string, int> shields = new Dictionary<string, int>();
    private int sucessos;
    private int falhas;
    private int testeNum;
    private Agent bestHero = null!;
    private int bestVal;

    // Componentes da Interface Dinâmica
    private Panel mainPanel = null!;
    private Dictionary<int, Color> rarityColors = null!;

    // Controles específicos de telas para atualização
    private RichTextBox logTxt = null!;
    private Button btnRoll = null!;
    private Label lblHeroStats = null!;
    private PictureBox pbBattleHero = null!;
    #endregion

    #region Constructor
    public GameGUI(
        IDatabaseInitializer databaseInitializer,
        IAgentService agentService,
        IGameService gameService,
        IEventService eventService,
        IBattleNarrator battleNarrator)
    {
        _agentService = agentService;
        _gameService = gameService;
        _databaseInitializer = databaseInitializer;
        _battleNarrator = battleNarrator;

        this.Text = "RPG Autobattler Roguelike";
        this.Size = new Size(1440, 900);
        this.MinimumSize = new Size(1180, 720);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.WindowState = FormWindowState.Maximized;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.BackColor = ColorTranslator.FromHtml("#10131b");
        this.Font = new Font("Segoe UI", 9F);

        rarityColors = new Dictionary<int, Color>
            {
                { 1, ColorTranslator.FromHtml("#b0b0b0") },
                { 2, ColorTranslator.FromHtml("#4caf50") },
                { 3, ColorTranslator.FromHtml("#2196f3") },
                { 4, ColorTranslator.FromHtml("#9c27b0") },
                { 5, ColorTranslator.FromHtml("#ff9800") }
            };

        mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#10131b") };
        mainPanel.ControlAdded += (_, e) =>
        {
            if (e.Control is not null)
                ApplyGameTheme(e.Control);
        };
        this.Controls.Add(mainPanel);

        _eventService = eventService;

        ResetGameState();
        CreateModeSelectionScreen();
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
        battleStoryHistory.Clear();
        eventHistory.Clear();
        initialBattleStoryTask = null;
        lastBattleConclusion = null;

        #region Populate Data from DB
        _databaseInitializer.InitializeDatabase().Wait();
        _agentService.GetAllHeroes(allAgents).Wait();

        itemShop.Clear();
        purchasedItems.Clear();
        #endregion
    }

    private void ClearScreen()
    {
        mainPanel.Controls.Clear();
    }

    private static VectorIconKind GetItemIconKind(Item item)
    {
        return item.Effect switch
        {
            ItemEffect.BonusAtaque => item.Name.Contains("Machado", StringComparison.OrdinalIgnoreCase) ? VectorIconKind.Axe : VectorIconKind.Sword,
            ItemEffect.BonusDefesa => item.Name.Contains("Armadura", StringComparison.OrdinalIgnoreCase) ? VectorIconKind.Armor : VectorIconKind.Shield,
            ItemEffect.BonusHP => VectorIconKind.Potion,
            ItemEffect.BonusPericia => VectorIconKind.Tome,
            ItemEffect.BonusEscudo => VectorIconKind.Shield,
            ItemEffect.ReducaoDC => item.Name.Contains("Mapa", StringComparison.OrdinalIgnoreCase)
                ? VectorIconKind.Map
                : item.Name.Contains("Olho", StringComparison.OrdinalIgnoreCase) ? VectorIconKind.Eye : VectorIconKind.Target,
            _ => VectorIconKind.Relic
        };
    }

    private static string GetRarityName(int rarity)
    {
        return rarity switch
        {
            1 => "COMUM",
            2 => "INCOMUM",
            3 => "RARO",
            4 => "ÉPICO",
            5 => "LENDÁRIO",
            _ => "DESCONHECIDO"
        };
    }

    private static void ApplyCardHover(Panel card, Color normalColor, Color hoverColor)
    {
        void SetHover(bool hovered)
        {
            card.BackColor = hovered ? hoverColor : normalColor;
        }

        void HandleEnter(object? sender, EventArgs e)
        {
            SetHover(true);
        }

        void HandleLeave(object? sender, EventArgs e)
        {
            Point cursorPosition = card.PointToClient(Cursor.Position);

            if (!card.ClientRectangle.Contains(cursorPosition))
                SetHover(false);
        }

        void Attach(Control control)
        {
            control.MouseEnter += HandleEnter;
            control.MouseLeave += HandleLeave;

            foreach (Control child in control.Controls)
                Attach(child);
        }

        Attach(card);
    }

    private static void ApplyButtonHover(Button button, Color normalColor, Color hoverColor)
    {
        button.UseVisualStyleBackColor = false;
        button.BackColor = normalColor;

        button.MouseEnter += (_, _) =>
        {
            if (button.Enabled)
                button.BackColor = hoverColor;
        };

        button.MouseLeave += (_, _) =>
        {
            if (button.Enabled)
                button.BackColor = normalColor;
        };
    }

    private static Panel CreateSynergyChip(string synergy)
    {
        FlowLayoutPanel chip = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Height = 24,
            Padding = new Padding(5, 2, 7, 2),
            Margin = new Padding(0, 2, 4, 2),
            BackColor = ColorTranslator.FromHtml("#3a3022")
        };

        chip.Controls.Add(new VectorIcon(
            VectorIconKind.Sparkles,
            ColorTranslator.FromHtml("#ffb74d"),
            15)
        {
            Margin = new Padding(0, 1, 3, 0)
        });

        chip.Controls.Add(new Label
        {
            Text = synergy.ToUpperInvariant(),
            Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#ffb74d"),
            AutoSize = true,
            Margin = new Padding(0, 1, 0, 0)
        });

        return chip;
    }

    private static Panel CreateStatCell(VectorIconKind iconKind, string text, Color textColor)
    {
        Panel cell = new()
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };

        FlowLayoutPanel row = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };

        row.Controls.Add(new VectorIcon(iconKind, textColor, 16));
        row.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 8.5f),
            Margin = new Padding(2, 1, 0, 0)
        });

        void CenterRow()
        {
            row.Left = Math.Max(0, (cell.ClientSize.Width - row.Width) / 2);
            row.Top = Math.Max(0, (cell.ClientSize.Height - row.Height) / 2);
        }

        cell.Controls.Add(row);
        cell.Resize += (_, _) => CenterRow();

        CenterRow();

        return cell;
    }

    private static TableLayoutPanel CreateMarketStats(Agent agent, bool hideStats)
    {
        string attack = hideStats ? "ATK: ?" : $"ATK: {agent.BaseAttack}";
        string defense = hideStats ? "DEF: ?" : $"DEF: {agent.BaseDefense}";
        string skill = hideStats ? "PER: ?" : $"PER: {agent.BaseSkill}";
        string hp = $"HP: {agent.MaxLife}";

        TableLayoutPanel grid = new()
        {
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false
        };

        for (int i = 0; i < 2; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        for (int i = 0; i < 2; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        grid.Controls.Add(CreateStatCell(VectorIconKind.Sword, attack, ColorTranslator.FromHtml("#d8dee9")), 0, 0);
        grid.Controls.Add(CreateStatCell(VectorIconKind.Shield, defense, ColorTranslator.FromHtml("#d8dee9")), 1, 0);
        grid.Controls.Add(CreateStatCell(VectorIconKind.Target, skill, ColorTranslator.FromHtml("#d8dee9")), 0, 1);
        grid.Controls.Add(CreateStatCell(VectorIconKind.Heart, hp, ColorTranslator.FromHtml("#d8dee9")), 1, 1);

        return grid;
    }

    private static TableLayoutPanel CreateTeamStats(Agent agent)
    {
        TableLayoutPanel grid = new()
        {
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false
        };

        for (int i = 0; i < 2; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        for (int i = 0; i < 2; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        Color textColor = Color.White;
        grid.Controls.Add(CreateStatCell(VectorIconKind.Heart, $"HP: {agent.CurrentLife}/{agent.MaxLife}", textColor), 0, 0);
        grid.Controls.Add(CreateStatCell(VectorIconKind.Sword, $"ATK: {agent.BaseAttack}", textColor), 1, 0);
        grid.Controls.Add(CreateStatCell(VectorIconKind.Shield, $"DEF: {agent.BaseDefense}", textColor), 0, 1);
        grid.Controls.Add(CreateStatCell(VectorIconKind.Target, $"PER: {agent.BaseSkill}", textColor), 1, 1);

        return grid;
    }

    private static void ApplyGameTheme(Control control)
    {
        if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI Semibold", Math.Max(9, button.Font.Size), FontStyle.Bold);
            button.Padding = new Padding(6, 2, 6, 2);
        }
        else if (control is GroupBox group)
        {
            group.ForeColor = ColorTranslator.FromHtml("#d7b56d");
            group.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            group.BackColor = ColorTranslator.FromHtml("#171c27");
        }
        else if (control is Label label && label.Font.Name.StartsWith("Arial", StringComparison.OrdinalIgnoreCase))
        {
            label.Font = new Font("Segoe UI", label.Font.Size, label.Font.Style);
        }

        control.ControlAdded += (_, e) =>
        {
            if (e.Control is not null)
                ApplyGameTheme(e.Control);
        };
        foreach (Control child in control.Controls)
            ApplyGameTheme(child);
    }

    # region TELA 1: SELEÇÃO DE MODO
    private void CreateModeSelectionScreen()
    {
        ClearScreen();

        TableLayoutPanel stage = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = ColorTranslator.FromHtml("#10131b"),
            Padding = new Padding(24, 8, 24, 8)
        };
        stage.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        stage.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        stage.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        mainPanel.Controls.Add(stage);

        TableLayoutPanel hero = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        hero.RowStyles.Add(new RowStyle(SizeType.Percent, 76));
        hero.RowStyles.Add(new RowStyle(SizeType.Percent, 24));
        stage.Controls.Add(hero, 0, 0);

        Label lblTitle = new Label
        {
            Text = "CRÔNICAS\nDO ÚLTIMO DADO",
            Font = new Font("Segoe UI Black", 27, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#f3d58a"),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = false
        };
        hero.Controls.Add(lblTitle, 0, 0);

        Label tagline = new Label
        {
            Text = "MONTE SEU ESQUADRÃO  •  ENCARE A JORNADA  •  VENÇA NO D20",
            Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#8f9aaf"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        hero.Controls.Add(tagline, 0, 1);

        TableLayoutPanel modeRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(90, 24, 90, 24),
            BackColor = Color.Transparent
        };
        modeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        modeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        modeRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        stage.Controls.Add(modeRow, 0, 1);

        Panel classicCard = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#19251f"), Margin = new Padding(10) };
        Panel hardCard = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#291d22"), Margin = new Padding(10) };
        modeRow.Controls.Add(classicCard, 0, 0);
        modeRow.Controls.Add(hardCard, 1, 0);

        void AddModeCard(Panel card, string title, string detail, string buttonText, Color accent, Func<Task> start)
        {
            card.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent });
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20, 10, 20, 10),
                BackColor = Color.Transparent
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            card.Controls.Add(content);

            content.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 19, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false }, 0, 0);
            content.Controls.Add(new Label { Text = detail, Font = new Font("Segoe UI", 10), ForeColor = ColorTranslator.FromHtml("#aeb7c7"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, AutoSize = false, Margin = new Padding(0) }, 0, 1);
            var actionButton = new Button { Text = buttonText, Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), BackColor = accent, ForeColor = Color.White, Dock = DockStyle.Fill, AutoSize = false, Margin = new Padding(0) };
            actionButton.Click += async (_, _) => await start();
            content.Controls.Add(actionButton, 0, 3);
        }

        AddModeCard(classicCard, "Jornada clássica", "Uma aventura equilibrada. Heróis nocauteados podem voltar ao combate.", "INICIAR JORNADA", ColorTranslator.FromHtml("#35785b"), () => StartGame("Normal"));
        AddModeCard(hardCard, "Jornada brutal", "Permadeath e críticos dobrados. Cada rolagem pode mudar tudo.", "ACEITAR O DESAFIO", ColorTranslator.FromHtml("#a84843"), () => StartGame("Difícil"));

        Label footer = new Label { Text = "CINCO ANDARES. UM ESQUADRÃO. UMA CHANCE.", Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#596477"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        stage.Controls.Add(footer, 0, 2);
    }

    private void StartInitialBattleStoryGeneration()
    {
        // A abertura de cada nível continua a narrativa acumulada da run.
        // Copiamos o histórico no momento da criação da Task para que o prompt
        // não mude enquanto a geração estiver acontecendo em background.
        initialBattleStoryTask = _battleNarrator.GenerateInitialAsync(
            currentLevel,
            battleStoryHistory.ToList());
    }

    private async Task StartGame(string chosenMode)
    {
        mode = chosenMode;

        StartInitialBattleStoryGeneration();

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
        Panel topFrame = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = ColorTranslator.FromHtml("#1a202c"), Padding = new Padding(8) };
        mainPanel.Controls.Add(topFrame);

        Label infoLbl = new Label
        {
            Text = $"ANDAR {currentLevel} / 5   •   {mode.ToUpper()}",
            Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#f3d58a"),
            AutoSize = false,
            Size = new Size(225, 44),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        Panel goldBadge = new Panel
        {
            Size = new Size(94, 34),
            BackColor = ColorTranslator.FromHtml("#252d3b"),
            Margin = new Padding(8, 5, 0, 0),
            Padding = new Padding(6, 0, 8, 0)
        };

        VectorIcon goldIcon = new(VectorIconKind.Coin, ColorTranslator.FromHtml("#f3d58a"), 18)
        {
            Location = new Point(6, 8)
        };
        goldBadge.Controls.Add(goldIcon);

        Label goldLbl = new Label
        {
            Text = $"{gold} G",
            Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#f3d58a"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 0, 0)
        };
        goldBadge.Controls.Add(goldLbl);

        FlowLayoutPanel headerInfo = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 340,
            Height = 54,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(18, 7, 0, 0),
            BackColor = Color.Transparent
        };
        headerInfo.Controls.Add(infoLbl);
        headerInfo.Controls.Add(goldBadge);
        topFrame.Controls.Add(headerInfo);

        Button btnMission = new Button
        {
            Text = "MISSÃO",
            Image = VectorIcon.CreateBitmap(VectorIconKind.Map, Color.White, 18, ColorTranslator.FromHtml("#a84843")),
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#a84843"),
            ForeColor = Color.White,
            Size = new Size(132, 40),
            FlatStyle = FlatStyle.Flat
        };
        btnMission.Click += async (s, e) => await CheckGoToMission();
        ApplyButtonHover(btnMission, ColorTranslator.FromHtml("#a84843"), ColorTranslator.FromHtml("#c15b55"));

        bool canReroll = gold >= rerollCount && (team.Count > 0 || gold > rerollCount);

        Button btnReroll = new Button
        {
            Text = $"REROLL  •  {rerollCount} G",
            Image = VectorIcon.CreateBitmap(
                VectorIconKind.Dice,
                Color.White,
                18,
                ColorTranslator.FromHtml("#795548")),
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#795548"),
            ForeColor = Color.White,
            Size = new Size(164, 40),
            FlatStyle = FlatStyle.Flat,
            Enabled = canReroll
        };
        btnReroll.Click += async (s, e) => await RerollShop();
        ApplyButtonHover(btnReroll, ColorTranslator.FromHtml("#795548"), ColorTranslator.FromHtml("#956d5a"));

        Button btnRestart = new Button
        {
            Text = "RECOMEÇAR",
            Image = VectorIcon.CreateBitmap(
                VectorIconKind.Refresh,
                Color.White,
                17,
                ColorTranslator.FromHtml("#4287f5")),
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#4287f5"),
            ForeColor = Color.White,
            Size = new Size(138, 40),
            FlatStyle = FlatStyle.Flat
        };
        btnRestart.Click += (s, e) => RestartEntireGame();
        ApplyButtonHover(btnRestart, ColorTranslator.FromHtml("#4287f5"), ColorTranslator.FromHtml("#5a99ff"));

        FlowLayoutPanel headerActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 470,
            Height = 54,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 7, 4, 0),
            BackColor = Color.Transparent
        };
        headerActions.Controls.Add(btnMission);
        headerActions.Controls.Add(btnReroll);
        headerActions.Controls.Add(btnRestart);
        topFrame.Controls.Add(headerActions);

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
        GroupBox marketFrame = new GroupBox { Text = " MERCADO DE HERÓIS ", Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#d7b56d"), Dock = DockStyle.Fill, Padding = new Padding(8) };
        mainLayout.Controls.Add(marketFrame, 0, 0);

        GroupBox itemFrame = new GroupBox
        {
            Text = " RELÍQUIAS ",
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#ff9800"),
            Dock = DockStyle.Fill
        };
        mainLayout.Controls.Add(itemFrame, 1, 0);

        Panel itemList = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        itemFrame.Controls.Add(itemList);

        int itemCardWidth = Math.Max(220, itemList.ClientSize.Width);
        List<Panel> itemCards = new();

        void ResizeItemCards()
        {
            int width = Math.Max(220, itemList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            for (int i = 0; i < itemCards.Count; i++)
            {
                Panel itemPanel = itemCards[i];
                itemPanel.Width = width;
                itemPanel.Left = 0;
                itemPanel.Top = i * 132;
            }

            int contentHeight = itemCards.Count * 132;
            itemList.AutoScrollMinSize = new Size(0, Math.Max(contentHeight, itemList.ClientSize.Height));
        }

        itemList.SizeChanged += (_, _) => ResizeItemCards();

        // Cards de itens
        for (int i = 0; i < itemShop.Count; i++)
        {
            int itemIdx = i;
            Item shopItem = itemShop[i];

            RarityCard itemCard = new RarityCard
            {
                Size = new Size(itemCardWidth, 122),
                BackColor = ColorTranslator.FromHtml("#202735"),
                BorderThickness = 2,
                Margin = Padding.Empty,
                Padding = new Padding(2)
            };
            itemCards.Add(itemCard);
            itemList.Controls.Add(itemCard);

            if (shopItem == null)
            {
                VectorIcon soldIcon = new(VectorIconKind.Check, ColorTranslator.FromHtml("#4caf50"), 24)
                {
                    Location = new Point(10, 46)
                };
                itemCard.Controls.Add(soldIcon);

                Label soldLbl = new Label
                {
                    Text = "COMPRADO",
                    Font = new Font("Segoe UI", 10, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    Location = new Point(40, 48),
                    Size = new Size(itemCard.Width - 50, 22),
                    AutoSize = false
                };
                itemCard.Controls.Add(soldLbl);
            }
            else
            {
                Color rarityColor = rarityColors.ContainsKey(shopItem.Rarity) ? rarityColors[shopItem.Rarity] : Color.White;

                itemCard.BorderColor = rarityColor;

                Panel itemHeader = new Panel
                {
                    Location = new Point(8, 4),
                    Size = new Size(itemCard.Width - 16, 34),
                    BackColor = Color.Transparent
                };

                VectorIcon itemIcon = new VectorIcon(GetItemIconKind(shopItem), rarityColor, 26)
                {
                    Location = new Point(0, 4)
                };
                itemHeader.Controls.Add(itemIcon);

                const int rarityBadgeWidth = 72;

                Label nameLbl = new Label
                {
                    Text = shopItem.Name,
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                    ForeColor = rarityColor,
                    Location = new Point(34, 0),
                    Size = new Size(Math.Max(80, itemHeader.Width - 34 - rarityBadgeWidth - 14), 34),
                    AutoSize = false,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                itemHeader.Controls.Add(nameLbl);

                Label rarityLbl = new Label
                {
                    Text = GetRarityName(shopItem.Rarity),
                    Font = new Font("Segoe UI Semibold", 7f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(55, rarityColor),
                    Size = new Size(rarityBadgeWidth, 18),
                    Location = new Point(itemHeader.Width - rarityBadgeWidth - 8, 8),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                itemHeader.Controls.Add(rarityLbl);

                itemCard.Controls.Add(itemHeader);

                Label descLbl = new Label
                {
                    Text = shopItem.Description,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = ColorTranslator.FromHtml("#cccccc"),
                    Location = new Point(8, 36),
                    Size = new Size(itemCard.Width - 16, 48),
                    AutoSize = false
                };
                itemCard.Controls.Add(descLbl);

                CenteredIconButton btnBuyItem = new CenteredIconButton
                {
                    Text = $"COMPRAR  •  {shopItem.Cost} G",
                    IconKind = VectorIconKind.Coin,
                    IconColor = Color.White,
                    IconSize = 18,
                    IconTextGap = 6,
                    BackColor = ColorTranslator.FromHtml("#795548"),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                    Dock = DockStyle.Bottom,
                    Height = 30,
                    FlatStyle = FlatStyle.Flat
                };
                btnBuyItem.Click += async (s, e) => await BuyItem(itemIdx);
                ApplyButtonHover(btnBuyItem, ColorTranslator.FromHtml("#795548"), ColorTranslator.FromHtml("#956d5a"));
                itemCard.Controls.Add(btnBuyItem);

                ApplyCardHover(
                    itemCard,
                    ColorTranslator.FromHtml("#202735"),
                    ColorTranslator.FromHtml("#293448"));
            }
        }

        // Exibe itens já comprados nesta run
        if (purchasedItems.Count > 0)
        {
            int ownedTop = itemCards.Count * 132;

            Label ownedTitle = new Label
            {
                Text = "── Itens Ativos ──",
                Font = new Font("Arial", 8, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(0, ownedTop + 4)
            };
            itemList.Controls.Add(ownedTitle);

            int ownedIndex = 0;
            foreach (var owned in purchasedItems)
            {
                Label ownedLbl = new Label
                {
                    Text = $"{owned.Emoji} {owned.Name}",
                    Font = new Font("Arial", 8),
                    ForeColor = ColorTranslator.FromHtml("#4caf50"),
                    AutoSize = true,
                    Location = new Point(2, ownedTop + 26 + ownedIndex * 22)
                };
                itemList.Controls.Add(ownedLbl);
                ownedIndex++;
            }

            itemList.AutoScrollMinSize = new Size(
                0,
                Math.Max(itemCards.Count * 132 + 26 + purchasedItems.Count * 22, itemList.ClientSize.Height));
        }

        ResizeItemCards();

        TableLayoutPanel marketGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 2,
            Padding = new Padding(10),
            AutoSize = false
        };
        marketGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        marketGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        marketGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        marketGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        marketFrame.Controls.Add(marketGrid);

        for (int i = 0; i < market.Count; i++)
        {
            int index = i;
            Agent agent = market[i];

            RarityCard card = new RarityCard
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTranslator.FromHtml("#202735"),
                BorderThickness = 2,
                Margin = new Padding(7),
                Padding = new Padding(4)
            };
            marketGrid.Controls.Add(card, i % 2, i / 2);

            if (agent == null)
            {
                TableLayoutPanel purchasedLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = 1,
                    BackColor = Color.Transparent,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                purchasedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                purchasedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                purchasedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

                FlowLayoutPanel purchasedContent = new FlowLayoutPanel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    BackColor = Color.Transparent,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    Anchor = AnchorStyles.None
                };

                purchasedContent.Controls.Add(new VectorIcon(
                    VectorIconKind.Check,
                    ColorTranslator.FromHtml("#4caf50"),
                    24)
                {
                    Margin = new Padding(0, 0, 4, 0)
                });

                purchasedContent.Controls.Add(new Label
                {
                    Text = "COMPRADO",
                    Font = new Font("Segoe UI", 11, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Margin = new Padding(0, 2, 0, 0)
                });

                purchasedLayout.Controls.Add(purchasedContent, 1, 0);
                card.Controls.Add(purchasedLayout);
            }
            else
            {
                Color rarityColor = rarityColors.ContainsKey(agent.Rarity) ? rarityColors[agent.Rarity] : Color.White;

                card.BorderColor = rarityColor;

                Label titleLbl = new Label
                {
                    Text = $"{agent.Name}\n({AgentHelper.MappingTypes(agent.Type)})",
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                    ForeColor = rarityColor,
                    TextAlign = ContentAlignment.TopCenter,
                    Location = new Point(0, 5),
                    Width = card.ClientSize.Width,
                    Height = 36,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                card.Controls.Add(titleLbl);

                Label rarityLbl = new Label
                {
                    Text = GetRarityName(agent.Rarity),
                    Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(55, rarityColor),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(84, 20),
                    Location = new Point(0, 43),
                    AutoSize = false
                };
                void CenterRarity(object? sender, EventArgs e) => rarityLbl.Left = Math.Max(0, (card.ClientSize.Width - rarityLbl.Width) / 2);
                card.SizeChanged += CenterRarity;
                CenterRarity(card, EventArgs.Empty);
                card.Controls.Add(rarityLbl);

                PictureBox pb = new PictureBox
                {
                    Image = await _agentService.GetAgentImage(agent, new Size(80, 80)),
                    Size = new Size(80, 80),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Left = 0,
                    Top = 68,
                    Anchor = AnchorStyles.Top
                };
                void CenterPortrait(object? sender, EventArgs e) => pb.Left = Math.Max(0, (card.ClientSize.Width - pb.Width) / 2);
                card.SizeChanged += CenterPortrait;
                CenterPortrait(card, EventArgs.Empty);
                card.Controls.Add(pb);

                Label statsLbl = new Label
                {
                    Text = string.Empty,
                    Font = new Font("Arial", 9),
                    ForeColor = ColorTranslator.FromHtml("#bbbbbb"),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(0, 148),
                    Width = card.ClientSize.Width,
                    Height = 50,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                statsLbl.Visible = false;
                card.Controls.Add(statsLbl);

                TableLayoutPanel statsGrid = CreateMarketStats(agent, mode == "Difícil");
                statsGrid.Location = new Point(14, 148);
                statsGrid.Size = new Size(Math.Max(180, card.ClientSize.Width - 28), 50);
                statsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                card.Controls.Add(statsGrid);

                var synergyStr = await _agentService.GetSynergyName(agent);

                Label synergyLbl = new Label
                {
                    Text = synergyStr,
                    Font = new Font("Arial", 9, FontStyle.Italic),
                    ForeColor = ColorTranslator.FromHtml("#ff9800"),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(0, 198),
                    Width = card.ClientSize.Width,
                    Height = 28,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                void ResizeStats(object? sender, EventArgs e)
                {
                    statsLbl.Width = card.ClientSize.Width;
                    synergyLbl.Width = card.ClientSize.Width;
                    CenterPortrait(sender, e);
                }
                card.SizeChanged += ResizeStats;
                ResizeStats(card, EventArgs.Empty);
                card.Controls.Add(synergyLbl);

                CenteredIconButton btnBuy = new CenteredIconButton
                {
                    Text = $"COMPRAR  •  {agent.Rarity} G",
                    IconKind = VectorIconKind.Coin,
                    IconColor = Color.White,
                    IconSize = 18,
                    IconTextGap = 6,
                    BackColor = ColorTranslator.FromHtml("#4caf50"),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                    Dock = DockStyle.Bottom,
                    Height = 30,
                    FlatStyle = FlatStyle.Flat
                };
                btnBuy.Click += async (s, e) => await BuyAgent(index);
                ApplyButtonHover(btnBuy, ColorTranslator.FromHtml("#4caf50"), ColorTranslator.FromHtml("#62c76b"));
                card.Controls.Add(btnBuy);

                ApplyCardHover(
                    card,
                    ColorTranslator.FromHtml("#202735"),
                    ColorTranslator.FromHtml("#293448"));
            }
        }

        // Lado Direito: Sua Equipe
        GroupBox teamFrame = new GroupBox { Text = $" SEU ESQUADRÃO  {team.Count}/5 ", Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#d7b56d"), Dock = DockStyle.Fill };
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

            Color rarityColor = rarityColors.ContainsKey(agent.Rarity) ? rarityColors[agent.Rarity] : Color.White;
            Color teamCardBackColor = Color.FromArgb(
                (32 + rarityColor.R) / 2,
                (39 + rarityColor.G) / 2,
                (53 + rarityColor.B) / 2);

            Panel card = new Panel
            {
                Size = new Size(Math.Max(300, teamList.ClientSize.Width - 24), 88),
                BackColor = teamCardBackColor,
                Margin = new Padding(0, 6, 0, 6)
            };
            teamList.Controls.Add(card);

            card.Controls.Add(new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = rarityColor
            });

            teamList.SizeChanged += (_, _) => card.Width = Math.Max(280, teamList.ClientSize.Width - 24);

            PictureBox pb = new PictureBox { Image = await _agentService.GetAgentImage(agent, new Size(45, 45)), Size = new Size(45, 45), Location = new Point(5, 7) };
            card.Controls.Add(pb);

            Label titleLbl = new Label { Text = $"{agent.Name} ({AgentHelper.MappingTypes(agent.Type)})", Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = rarityColor, Location = new Point(55, 5), Size = new Size(Math.Max(100, card.Width - 130), 22), AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            card.Controls.Add(titleLbl);

            string hpStr = $"❤️ Vida: {agent.CurrentLife}/{agent.MaxLife}";
            Label statsLbl = new Label
            {
                Text = string.Empty,
                Visible = false,
                Location = new Point(55, 29),
                Size = new Size(Math.Max(100, card.Width - 130), 34)
            };
            card.Controls.Add(statsLbl);

            TableLayoutPanel teamStatsGrid = CreateTeamStats(agent);
            teamStatsGrid.Location = new Point(55, 28);
            teamStatsGrid.Size = new Size(Math.Max(100, card.Width - 130), 34);
            teamStatsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(teamStatsGrid);

            string synergyName = await _agentService.GetSynergyName(agent);
            Label synergyLbl = new Label { Text = $"{synergyName}", Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), ForeColor = Color.Orange, Location = new Point(55, 64), Size = new Size(Math.Max(100, card.Width - 130), 20), AutoSize = false, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            card.Controls.Add(synergyLbl);

            Button btnSell = new Button
            {
                Text = $"+{Math.Ceiling(agent.Rarity / 2.0)}g",
                BackColor = ColorTranslator.FromHtml("#f44336"),
                ForeColor = Color.White,
                Font = new Font("Arial", 8, FontStyle.Bold),
                Size = new Size(55, 30),
                Location = new Point(card.Width - 60, 29),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat
            };
            btnSell.Click += async (s, e) => await SellAgent(index);
            card.Controls.Add(btnSell);

            void ResizeTeamCard(object? sender, EventArgs e)
            {
                int textWidth = Math.Max(100, card.ClientSize.Width - 130);
                titleLbl.Width = textWidth;
                statsLbl.Width = textWidth;
                synergyLbl.Width = textWidth;
                btnSell.Left = card.ClientSize.Width - btnSell.Width - 5;
            }

            card.SizeChanged += ResizeTeamCard;
            ResizeTeamCard(card, EventArgs.Empty);
            ApplyButtonHover(btnSell, ColorTranslator.FromHtml("#f44336"), ColorTranslator.FromHtml("#ef625d"));
        }

        // Exibe as sinergias que estão ativas no esquadrão.
        List<string> activeSynergies = await _agentService.GetActiveSynergies(team);

        if (activeSynergies.Count > 0)
        {
            Label synergyTitle = new Label
            {
                Text = "SINERGIAS ATIVAS",
                Font = new Font("Segoe UI Semibold", 8, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#d7b56d"),
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 3)
            };
            teamList.Controls.Add(synergyTitle);

            FlowLayoutPanel synergyList = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 2),
                Padding = Padding.Empty,
                BackColor = Color.Transparent,
                MaximumSize = new Size(Math.Max(200, teamList.ClientSize.Width - 20), 0)
            };

            foreach (string synergy in activeSynergies)
                synergyList.Controls.Add(CreateSynergyChip(synergy));

            teamList.Controls.Add(synergyList);
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
        lastBattleConclusion = null;

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

        Panel battleFrame = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#10131b"), Padding = new Padding(28) };
        mainPanel.Controls.Add(battleFrame);

        // --- CONTROLES COM DOCK TOP (A ordem de adição importa!) ---

        // 1º O Título Principal da Missão
        Label lblMTitle = new Label
        {
            Text = $"🚨 MISSÃO NÍVEL {currentLevel} | TEMA: {currentTheme.ToUpper()} 🚨",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#f3d58a"),
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
            Location = new Point(Math.Max(0, (battleFrame.ClientSize.Width - 80) / 2), 120),
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
            Width = Math.Max(400, battleFrame.ClientSize.Width - 40),
            Height = 210,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        battleFrame.Controls.Add(logTxt);

        // Botão de Rolar Dados
        btnRoll = new Button
        {
            Text = "ROLAR DADO (TESTE 1/5)",
            Image = VectorIcon.CreateBitmap(VectorIconKind.Dice, Color.White, 24, ColorTranslator.FromHtml("#e91e63")),
            ImageAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Arial", 12, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#e91e63"),
            ForeColor = Color.White,
            Size = new Size(360, 50),
            Location = new Point(Math.Max(0, (battleFrame.ClientSize.Width - 360) / 2), 440),
            Anchor = AnchorStyles.Top,
            FlatStyle = FlatStyle.Flat
        };
        btnRoll.Click += async (s, e) => await NextTestRoll();
        battleFrame.Controls.Add(btnRoll);

        await SetupNextCombatantInfo();

        if (bestHero is not null)
        {
            btnRoll.Enabled = false;
            await GenerateBattleStoryAsync(isInitial: true);
            btnRoll.Enabled = true;
        }
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
            int val = a.GetAttr(currentTheme) + (await _agentService.GetSynergyBonus(a, aliveHeroes));
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
        btnRoll.Text = $"ROLAR PARA {bestHero.Name.ToUpper()} (Total: {bestVal}) [Teste {testeNum}/5]";
        pbBattleHero.Image = await _agentService.GetAgentImage(bestHero, new Size(80, 80));
    }

    private async Task NextTestRoll()
    {
        if (btnRoll.Text == "VER RESULTADO FINAL")
        {
            await EndMissionCalculations();
            return;
        }

        btnRoll.Enabled = false;

        int round = testeNum;
        Agent rollingHero = bestHero;
        int d20 = random.Next(1, 21);
        int total = bestVal + d20;

        await AppendLog($"\n--- TESTE {round}/5 ({rollingHero.Name}) ---", Color.White);
        await AppendLog($"Resultado do dado: {d20} | Total: {total} vs Alvo {dc}", Color.White);

        string outcome;
        string outcomeDetail;

        if (mode == "Difícil" && d20 == 20)
        {
            outcome = "CRÍTICO POSITIVO";
            outcomeDetail = "A rodada contou como 2 sucessos.";
            await AppendLog("🌟 CRÍTICO POSITIVO! Contando como 2 SUCESSOS!", Color.Green);
            sucessos += 2;
        }
        else if (mode == "Difícil" && d20 == 1)
        {
            outcome = "CRÍTICO NEGATIVO";
            outcomeDetail = $"{rollingHero.Name} sofreu a consequência crítica e recebeu dano.";
            await AppendLog("💀 CRÍTICO NEGATIVO! 1 Falha Crítica anotada.", Color.Red);
            falhas += 2;
            await ApplyGuiDamage(rollingHero, currentLevel * 2);
        }
        else if (total >= dc)
        {
            outcome = "SUCESSO";
            outcomeDetail = "A ação foi bem-sucedida e a equipe avançou.";
            await AppendLog("🟢 SUCESSO!", Color.Green);
            sucessos += 1;
        }
        else
        {
            outcome = "FALHA";
            outcomeDetail = $"{rollingHero.Name} sofreu as consequências da falha e recebeu dano.";
            await AppendLog("🔴 FALHA!", Color.Red);
            falhas += 1;
            await ApplyGuiDamage(rollingHero, currentLevel);
        }

        bool missionEnded =
            falhas >= 3 ||
            round >= 5 ||
            team.All(a => a.CurrentLife <= 0);

        bool missionSuccess = missionEnded &&
            team.Any(a => a.CurrentLife > 0) &&
            falhas < 3;

        if (missionEnded)
        {
            outcome = missionSuccess
                ? "VITÓRIA DO NÍVEL"
                : "DERROTA DO NÍVEL";

            outcomeDetail = missionSuccess
                ? $"O nível terminou com {sucessos} sucesso(s) e {falhas} falha(s). Esta é a conclusão do arco atual. A party venceu o confronto e deve ser narrada encerrando o capítulo em triunfo."
                : $"O nível terminou com {sucessos} sucesso(s) e {falhas} falha(s). Esta é a conclusão do arco atual. A party foi derrotada e a narrativa deve encerrar sua jornada em fracasso.";
        }

        if (!rollingHero.Fatigue.ContainsKey(currentTheme))
            rollingHero.Fatigue[currentTheme] = 0;

        rollingHero.Fatigue[currentTheme] += 1;

        await GenerateBattleStoryAsync(
            isInitial: false,
            isFinal: missionEnded,
            finalSuccess: missionSuccess,
            hero: rollingHero,
            round: round,
            d20: d20,
            total: total,
            outcome: outcome,
            outcomeDetail: outcomeDetail);

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

        btnRoll.Enabled = true;
    }

    private async Task GenerateBattleStoryAsync(
        bool isInitial,
        bool isFinal = false,
        bool finalSuccess = false,
        Agent? hero = null,
        int round = 1,
        int d20 = 0,
        int total = 0,
        string outcome = "",
        string outcomeDetail = "")
    {
        Agent storyHero = hero ?? bestHero;
        if (storyHero is null)
            return;

        if (isFinal)
            await AppendLog("\n✦ O confronto chega ao desfecho... ✦", Color.Orange);
        else if (!isInitial)
            await AppendLog("\n✦ A batalha continua... a história se atualiza. ✦", Color.Orange);
        else
            await AppendLog("\n✦ A batalha começa... ✦", Color.Orange);

        BattleNarrativeContext context = new()
        {
            Level = currentLevel,
            Theme = currentTheme,
            Hero = storyHero,
            Round = round,
            D20 = d20,
            Total = total,
            Dc = dc,
            IsInitial = isInitial,
            Outcome = outcome,
            OutcomeDetail = outcomeDetail,
            Team = team.ToList(),
            Items = purchasedItems.ToList(),
            PreviousStories = battleStoryHistory.ToList()
        };

        BattleStory? story;

        if (isInitial && initialBattleStoryTask is not null)
        {
            story = await initialBattleStoryTask;
            initialBattleStoryTask = null;
        }
        else if (isFinal)
        {
            string currentRoundStory = $"""
                Rodada final {round}/5:
                {storyHero.Name} teve o resultado "{outcome}".
                {outcomeDetail}
                D20: {d20}. Total: {total}. DC: {dc}.
                Resultado consolidado do nível: {(finalSuccess ? "VITÓRIA DA PARTY" : "DERROTA DA PARTY")}.
                Placar final: {sucessos} sucesso(s) e {falhas} falha(s).
                """;

            IReadOnlyList<string> conclusionHistory = battleStoryHistory
                .Concat(new[] { currentRoundStory })
                .ToList();

            story = await _battleNarrator.GenerateConclusionAsync(
                currentLevel,
                finalSuccess,
                sucessos,
                falhas,
                team.ToList(),
                purchasedItems.ToList(),
                conclusionHistory,
                eventHistory.ToList());

            lastBattleConclusion = story;
        }
        else
        {
            story = await _battleNarrator.GenerateAsync(context);
        }

        if (story is null)
        {
            await AppendLog(
                "A narrativa não pôde ser gerada, mas os acontecimentos mecânicos da batalha continuam.",
                Color.Gray);
            return;
        }

        await AppendLog($"📖 {story.Title}", Color.Gold);
        await AppendLog(story.Narrative, Color.White);

        battleStoryHistory.Add($"{story.Title}: {story.Narrative}");
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

        bool aliveHeroes = team.Any(a => a.CurrentLife > 0);
        bool missionSuccess = aliveHeroes && falhas < 3;

        Panel loadingFrame = new Panel
        {
            Size = new Size(620, 220),
            BackColor = ColorTranslator.FromHtml("#1a1a2e"),
            Location = new Point(
                (mainPanel.Width - 620) / 2,
                (mainPanel.Height - 220) / 2),
            Anchor = AnchorStyles.None
        };

        Label loadingTitle = new Label
        {
            Text = missionSuccess
                ? "✦ CONCLUSÃO DA BATALHA ✦"
                : "✦ DESFECHO DA JORNADA ✦",
            Font = new Font("Segoe UI Semibold", 17, FontStyle.Bold),
            ForeColor = missionSuccess
                ? ColorTranslator.FromHtml("#4caf50")
                : ColorTranslator.FromHtml("#f44336"),
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label loadingDescription = new Label
        {
            Text = missionSuccess
                ? "A batalha terminou. O narrador está encerrando este capítulo..."
                : "A batalha chegou ao limite. O destino da party está sendo decidido...",
            Font = new Font("Segoe UI", 11, FontStyle.Italic),
            ForeColor = ColorTranslator.FromHtml("#b8c1d1"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        loadingFrame.Controls.Add(loadingDescription);
        loadingFrame.Controls.Add(loadingTitle);
        mainPanel.Controls.Add(loadingFrame);
        loadingFrame.BringToFront();

        // Permite que a tela de carregamento seja desenhada antes da inferência.
        await Task.Yield();

        BattleStory? conclusion = await _battleNarrator.GenerateConclusionAsync(
            currentLevel,
            missionSuccess,
            sucessos,
            falhas,
            team.ToList(),
            purchasedItems.ToList(),
            battleStoryHistory.ToList(),
            eventHistory.ToList());

        if (conclusion is null)
        {
            conclusion = missionSuccess
                ? new BattleStory(
                    $"Vitória no Nível {currentLevel}",
                    "A party supera o confronto e encerra este capítulo com uma vitória clara. As marcas da batalha permanecem, mas os heróis seguem adiante.")
                : new BattleStory(
                    "A Queda da Party",
                    "As forças da party chegam ao fim. Derrotados pelas consequências do confronto, os heróis não conseguem continuar a jornada.");
        }

        battleStoryHistory.Add(
            $"{conclusion.Title}: {conclusion.Narrative}");

        ClearScreen();

        Panel frame = new Panel
        {
            Size = new Size(680, 590),
            BackColor = ColorTranslator.FromHtml("#10131b"),
            Padding = new Padding(22)
        };
        frame.Location = new Point(
            (mainPanel.Width - frame.Width) / 2,
            (mainPanel.Height - frame.Height) / 2);
        frame.Anchor = AnchorStyles.None;
        mainPanel.Controls.Add(frame);

        Label lblTitle = new Label
        {
            Text = $"RESULTADO FINAL: {sucessos} SUCESSOS | {falhas} FALHAS",
            Font = new Font("Segoe UI Semibold", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Size = new Size(636, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Top = 8
        };
        frame.Controls.Add(lblTitle);

        Label lblStoryTitle = new Label
        {
            Text = conclusion.Title,
            Font = new Font("Segoe UI Semibold", 18, FontStyle.Bold),
            ForeColor = missionSuccess
                ? ColorTranslator.FromHtml("#4caf50")
                : ColorTranslator.FromHtml("#f44336"),
            Size = new Size(636, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Top = 58
        };
        frame.Controls.Add(lblStoryTitle);

        RichTextBox narrativeBox = new RichTextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = ColorTranslator.FromHtml("#0d1117"),
            ForeColor = ColorTranslator.FromHtml("#e0e0e0"),
            Font = new Font("Segoe UI", 11),
            Location = new Point(42, 115),
            Size = new Size(596, 220),
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Text = conclusion.Narrative
        };
        frame.Controls.Add(narrativeBox);

        if (!missionSuccess)
        {
            string msgText = !aliveHeroes
                ? "🔴 GAME OVER!\nSua equipe inteira está morta!"
                : "🔴 GAME OVER!\nSua equipe acumulou 3 ou mais falhas e não conseguiu completar o andar.";

            Label lblRes = new Label
            {
                Text = msgText,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#f44336"),
                Size = new Size(636, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 350
            };
            frame.Controls.Add(lblRes);

            Button btnRestart = new Button
            {
                Text = "REINICIAR JOGO",
                Font = new Font("Arial", 11, FontStyle.Bold),
                BackColor = ColorTranslator.FromHtml("#f44336"),
                ForeColor = Color.White,
                Size = new Size(200, 45),
                Left = 240,
                Top = 440,
                FlatStyle = FlatStyle.Flat
            };
            btnRestart.Click += (s, e) => RestartEntireGame();
            frame.Controls.Add(btnRestart);
        }
        else
        {
            string txt;
            int g;

            if (sucessos == 3)
            {
                txt = "🟡 VITÓRIA PÍRRICA! Vocês avançaram no limite.";
                g = 2;
            }
            else if (sucessos == 4)
            {
                txt = "🔵 VITÓRIA CONFIANTE! Uma excelente exibição tática.";
                g = 5;
            }
            else
            {
                txt = "🟢 VITÓRIA ABSOLUTA! Perfeito e lendário!";
                g = 8;
            }

            gold += g;
            extraDc = 0;

            Label lblRes = new Label
            {
                Text = $"{txt}\nRecompensa da Fase: +{g}g",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#4caf50"),
                Size = new Size(636, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 350
            };
            frame.Controls.Add(lblRes);

            Button btnContinue = new Button
            {
                Text = "AVANÇAR PARA EVENTO",
                Font = new Font("Arial", 11, FontStyle.Bold),
                BackColor = ColorTranslator.FromHtml("#ff9800"),
                ForeColor = Color.White,
                Size = new Size(220, 45),
                Left = 230,
                Top = 440,
                FlatStyle = FlatStyle.Flat
            };
            btnContinue.Click += async (s, e) => await NextLevelRestPhase();
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
            Size = new Size(560, 52),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(7, 38),
            AutoSize = false
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
            Size = new Size(540, 88),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(27, 94),
            AutoSize = false
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
                Size = new Size(520, 62),
                Location = new Point(37, 190 + i * 72),
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
                EventResult result = _eventService.GetEventResult(
                    gameEvent,
                    chosenIndex,
                    team);

                string chosenOption = optionTexts[chosenIndex]
                    ?? $"Opção {chosenIndex + 1}";

                // Guarda a cena + decisão + consequência para que o próximo
                // evento continue exatamente de onde este terminou.
                eventHistory.Add(
                    $"Evento: {gameEvent.Name}. " +
                    $"{gameEvent.Description} " +
                    $"Escolha do grupo: {chosenOption} " +
                    $"Resultado: {result.Title}. {result.Description}");

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
        Agent? heroAffected = result.AffectedAgentId != 0
            ? team.FirstOrDefault(a => a.Id == result.AffectedAgentId)
            : null;

        if (heroAffected is not null && result.HpBonus != 0)
        {
            heroAffected.CurrentLife += result.HpBonus;
            heroAffected.CurrentLife = Math.Clamp(
                heroAffected.CurrentLife,
                0,
                heroAffected.MaxLife);
        }

        if (heroAffected is not null && result.HpBonusMaxLife != 0)
        {
            heroAffected.MaxLife += result.HpBonusMaxLife;
            heroAffected.MaxLife = Math.Max(1, heroAffected.MaxLife);
            heroAffected.CurrentLife = Math.Min(
                heroAffected.CurrentLife,
                heroAffected.MaxLife);
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

        if (heroAffected is not null && result.PermanentAttack != 0)
        {
            heroAffected.BaseAttack += result.PermanentAttack;
        }

        if(result.ExtraDc != 0)
        {
            extraDc = result.ExtraDc;
        }

        if (heroAffected is not null && result.TemporarySkillBonus != 0)
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
        ShowEventGenerationScreen();

        // Dá uma chance para o WinForms pintar a tela de carregamento
        // antes de começar a inferência local.
        await Task.Yield();

        Event? mechanicsTemplate = await _eventService.RandomEvent();
        if (mechanicsTemplate is null)
        {
            await ExecuteRestPhase();
            return;
        }

        Event? generatedEvent = await _battleNarrator.GenerateEventAsync(
            mechanicsTemplate,
            team.ToList(),
            purchasedItems.ToList(),
            currentLevel,
            gold,
            battleStoryHistory.ToList(),
            eventHistory.ToList());

        Event gameEvent = generatedEvent ?? mechanicsTemplate;

        await NextEventPhase(gameEvent);
    }

    private void ShowEventGenerationScreen()
    {
        ClearScreen();

        Panel frame = new Panel
        {
            Size = new Size(600, 240),
            BackColor = ColorTranslator.FromHtml("#1a1a2e"),
            Location = new Point(
                (mainPanel.Width - 600) / 2,
                (mainPanel.Height - 240) / 2),
            Anchor = AnchorStyles.None
        };

        Label title = new Label
        {
            Text = "✦ UM NOVO ENCONTRO ✦",
            Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#ff9800"),
            Dock = DockStyle.Top,
            Height = 52,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label description = new Label
        {
            Text = "O mundo reage à sua jornada...",
            Font = new Font("Segoe UI", 11, FontStyle.Italic),
            ForeColor = ColorTranslator.FromHtml("#b8c1d1"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        frame.Controls.Add(description);
        frame.Controls.Add(title);
        mainPanel.Controls.Add(frame);
        frame.BringToFront();
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

        // O novo andar já começa a preparar sua história de batalha enquanto
        // o jogador está no descanso e, principalmente, durante a loja.
        if (currentLevel <= 5)
            StartInitialBattleStoryGeneration();

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
            btnNext.Click += async (s, e) =>
            {
                market = _gameService.RollMarket(team, allAgents, currentLevel);
                itemShop = RollItemShop(currentLevel);
                rerollCount = 1;
                await CreateShopScreen();
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
