using System;

public class SynergyAgents
{
    public List<(string, string)> Pairs { get; set; }

    public SynergyAgents()
    {
        var pairs = new List<(string, string)>
            {
                ("Kazumi", "Akane"), ("Thoryn", "Zendaya"),
                ("Perdigas", "Maria Cecília"), ("Tom", "Maria Cecília"),
                ("Saphyra", "Shantal")
            };


        Pairs = pairs;
    }
}

public class Agent
{
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
        Fatigue["Ataque"] = 0;
        Fatigue["Defesa"] = 0;
        Fatigue["Perícia"] = 0;
    }

    public int GetAttr(string attrName, List<Agent> teamAgents)
    {
        int baseVal = 0;
        if (attrName == "Ataque") baseVal = BaseAttack;
        else if (attrName == "Defesa") baseVal = BaseDefense;
        else if (attrName == "Perícia") baseVal = BaseSkill;

        int val = baseVal - (Fatigue.ContainsKey(attrName) ? Fatigue[attrName] : 0);
        return val;
    }

    public int GetSynergyBonus(List<Agent> teamAgents)
    {
        int bonus = 0;
        var namesInTeam = teamAgents.Select(a => a.Name).ToList();
        var synergyPairs = new SynergyAgents().Pairs;
        foreach (var (p1, p2) in synergyPairs)
        {
            if (Name == p1 || Name == p2)
            {
                if (namesInTeam.Contains(p1) && namesInTeam.Contains(p2))
                {
                    bonus += 3;
                }
            }
        }

        var groups = new List<string> { "Heróis de Uagamora", "Heróis do Ninho", "Hara Kiri", "A Chama", "A Banda", "Vilões", "Vendedor", "Dragão" };
        foreach (var g in groups)
        {
            bool selfHasGroup = SynergyText.IndexOf(g, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                (g == "Vilões" && SynergyText.IndexOf("vilão", StringComparison.OrdinalIgnoreCase) >= 0);

            if (selfHasGroup)
            {
                int count = 0;
                foreach (var a in teamAgents)
                {
                    if (a.SynergyText.IndexOf(g, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (g == "Vilões" && a.SynergyText.IndexOf("vilão", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        count++;
                    }
                }

                if (count == 3) bonus += 2;
                else if (count == 4) bonus += 3;
                else if (count >= 5) bonus += 4;
            }
        }

        return bonus;
    }

    public string GetSynergyHero(Agent agent)
    {
        var synergyAgents = new SynergyAgents();
        var match = synergyAgents.Pairs.FirstOrDefault(p =>
            p.Item1.Equals(agent.Name, StringComparison.OrdinalIgnoreCase) ||
            p.Item2.Equals(agent.Name, StringComparison.OrdinalIgnoreCase));

        string synergyStr = "";
        if (match != default)
        {
            string partnerName = match.Item1.Equals(agent.Name, StringComparison.OrdinalIgnoreCase) ? match.Item2 : match.Item1;
            return synergyStr = $"Sinergia ativa com: {partnerName}";
        }
        else
        {
            return synergyStr;
        }
    }
}
