public class SynergyAgents
{
    // Estrutura que guarda o nome da Sinergia e a lista de heróis que fazem parte dela
    public class SynergyGroup
    {
        public string Name { get; set; }
        public List<string> Heroes { get; set; }

        public SynergyGroup(string name, params string[] heroes)
        {
            Name = name;
            // Guarda os nomes sem espaços extras para evitar erros de digitação
            Heroes = heroes.Select(h => h.Trim()).ToList();
        }
    }

    public List<SynergyGroup> Groups { get; set; }

    private static readonly string S_Irmas = "Irmãs";
    private static readonly string S_Irmaos = "Irmãos";
    private static readonly string S_HaraKiri = "Hara-Kiri";
    private static readonly string S_Lider = "Líder";
    private static readonly string S_AChama = "A Chama";
    private static readonly string S_Trindade = "Trindade";
    private static readonly string S_Amor = "Amor";
    private static readonly string S_CasalReal = "Casal Real";
    private static readonly string S_AmorPlatonico = "Amor Platônico";
    private static readonly string S_Ordem = "Ordem";
    private static readonly string S_NovaOrdem = "Nova Ordem";
    private static readonly string S_Uagamora = "Uagamora";
    private static readonly string S_Solo = "Solo";
    private static readonly string S_NinhoDragao = "Ninho do Dragão";
    private static readonly string S_Dragao = "Dragão";
    private static readonly string S_Caos = "Caos";
    private static readonly string S_Viloes = "Vilões";
    private static readonly string S_Vendedores = "Vendedores";
    private static readonly string S_Fe = "Fé";
    public SynergyAgents()
    {

        // Mapeamento exato de todas as sinergias do seu arquivo synergy_values.csv
        Groups = new List<SynergyGroup>
        {
            new SynergyGroup("Irmãs", "Akane", "Kazumi"),
            new SynergyGroup("Irmãos", "Shyva", "Samir"),
            new SynergyGroup("Hara-Kiri", "Kazumi", "Megan", "Marcos", "Nyu", "Seph Flores"),
            new SynergyGroup("Líder", "Kazumi", "Vysenia", "Marcus"),
            new SynergyGroup("A Chama", "Agni", "Aziza", "Shantal"),
            new SynergyGroup("Trindade", "Thoryn", "Barbara", "Zendaya"),
            new SynergyGroup("Amor", "Shantal", "Saphyra"),
            new SynergyGroup("Casal Real", "Vysenia", "Daerion"),
            new SynergyGroup("Amor Platônico", "Meilyn", "Daerion"),
            new SynergyGroup("Ordem", "Perdigas", "Leão", "Maria Cecília"),
            new SynergyGroup("Nova Ordem", "Tom", "Maria Cecília", "Matheus", "Akane"),
            new SynergyGroup("Uagamora", "Oriven", "Mauga", "Shyvana", "Kael", "Kyra", "Viktor", "Aryte", "Symon"),
            new SynergyGroup("Solo", "Kazumi", "Crane", "Lilith"),
            new SynergyGroup("Ninho do Dragão", "Cael", "Meilyn", "Daerion", "Vysenia", "Aya"),
            new SynergyGroup("Dragão", "Daerion", "Shyvana", "Vysenia"),
            new SynergyGroup("Caos", "Shyva", "Samir", "Aya", "Padre", "Sirius"),
            new SynergyGroup("Vilões", "Lilith", "Caim"),
            new SynergyGroup("Vendedores", "Ruivo", "Zahra", "Bree", "Mimoso", "Yasmin"),
            new SynergyGroup("Fé", "Thoryn", "Nix", "Orion", "Perdigas", "Arkmeros", "Yasmin")
        };
    }

    // Função que descobre TODAS as sinergias que um determinado herói possui
    public List<string> GetSynergiesForHero(string heroName)
    {
        return Groups
            .Where(g => g.Heroes.Contains(heroName, StringComparer.OrdinalIgnoreCase))
            .Select(g => g.Name)
            .ToList();
    }
}

public class Agent
{
    public AgentType agentType { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Desc { get; set; }
    public string Player { get; set; }
    public int Rarity { get; set; }
    public string SynergyText { get; set; }

    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int MaxLife { get; set; }
    public int BaseSkill { get; set; }

    public int CurrentLife { get; set; }
    public Dictionary<string, int> Fatigue { get; set; }
    public string ImageFilename { get; set; }

    public const string Ataque = "Ataque";
    public const string Defesa = "Defesa";
    public const string Pericia = "Pericia";

    public Agent(dynamic row)
    {
        Name = row.Agente;
        Type = row.Tipo;
        Desc = row.Descrição;
        Player = row.Player;
        Rarity = int.Parse(row.Raridade);
        SynergyText = ((string)row.Sinergia).Trim();

        BaseAttack = int.Parse(row.Ataque);
        BaseDefense = int.Parse(row.Defesa);
        MaxLife = int.Parse(row.Vida);
        BaseSkill = int.Parse(row.Perícia);

        CurrentLife = MaxLife;
        Fatigue = new Dictionary<string, int> { { Ataque, 0 }, { Defesa, 0 }, { Pericia, 0 } };
        ImageFilename = Name.ToLower();
    }

    public void ResetStatus()
    {
        CurrentLife = MaxLife;
        ResetFatigue();
    }

    public void ResetFatigue()
    {
        Fatigue[Ataque] = 0;
        Fatigue[Defesa] = 0;
        Fatigue[Pericia] = 0;
    }

    public int GetAttr(string attrName, List<Agent> teamAgents)
    {
        int baseVal = 0;
        if (attrName == Ataque) baseVal = BaseAttack;
        else if (attrName == Defesa) baseVal = BaseDefense;
        else if (attrName == Pericia) baseVal = BaseSkill;

        int val = baseVal - (Fatigue.ContainsKey(attrName) ? Fatigue[attrName] : 0);
        return val;
    }

    public int GetSynergyBonus(List<Agent> teamAgents)
    {
        int bonus = 0;

        var synergyAgents = new SynergyAgents();

        var namesInTeam = teamAgents.Select(a => a.Name).ToList();

        foreach (var group in synergyAgents.Groups)
        {
            bool selfHasGroup = group.Heroes.Contains(this.Name, StringComparer.OrdinalIgnoreCase);

            if (selfHasGroup)
            {
                int count = group.Heroes.Count(heroName =>
                    namesInTeam.Contains(heroName));

                if (group.Name == "Solo")
                {
                    if (teamAgents.Count == 1)
                    {
                        bonus += 4;
                    }
                }
                else if (group.Name == "Líder" ||
                    group.Name == "Trindade" ||
                    group.Name == "Ordem" ||
                    group.Name == "Nova Ordem" ||
                    group.Name == "Vendedores" ||
                    group.Name == "Hara-Kiri" ||
                    group.Name == "A chama" ||
                    group.Name == "Uagamora" ||
                    group.Name == "Ninho do Dragão" ||
                    group.Name == "Dragão" ||
                    group.Name == "Caos" ||
                    group.Name == "Fé")
                {
                    if (count >= 2)
                    {
                        bonus += 1 + (count - 2);
                    }
                }
                else if (group.Name == "Irmãos" ||
                    group.Name == "Irmãs")
                {
                    bonus += 3;
                }
                else if (group.Name == "Amor" ||
                    group.Name == "Amor Platônico" ||
                    group.Name == "Casal Real" ||
                    group.Name == "Vilão")
                {
                    bonus += 2;
                }
            }
        }

        return bonus;
    }

    public string GetSynergyName(Agent agent)
    {
        var synergyAgents = new SynergyAgents();

        List<string> heroSynergies = synergyAgents.GetSynergiesForHero(agent.Name);

        if (heroSynergies.Count > 0)
        {
            return "Sinergia: " + string.Join(", ", heroSynergies);
        }
        else
        {
            return "";
        }
    }
}

public class AgentType
{
    public const string Lutador = "Lutador";
    public const string Defensor = "Defensor";
    public const string Especialista = "Especialista";
    public const string Suporte = "Suporte";
}
