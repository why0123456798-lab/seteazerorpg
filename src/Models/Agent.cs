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
}

public class Agent
{
    public int Id { get; set; }
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

    public Agent(int id, string name, string type, int rarity, string synergyText, int ataque, int defesa, int vida, int pericia)
    {
        Id = id;
        Name = name;
        Type = type;
        Rarity = rarity; // Sem int.Parse!
        SynergyText = synergyText.Trim();

        BaseAttack = ataque;   // Sem int.Parse!
        BaseDefense = defesa; // Sem int.Parse!
        MaxLife = vida;       // Sem int.Parse!
        BaseSkill = pericia;   // Sem int.Parse!

        CurrentLife = MaxLife;
        Fatigue = new Dictionary<string, int> { { "Ataque", 0 }, { "Defesa", 0 }, { "Perícia", 0 } };
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
}

public class AgentType
{
    public const string Lutador = "Lutador";
    public const string Defensor = "Defensor";
    public const string Especialista = "Especialista";
    public const string Suporte = "Suporte";
}
