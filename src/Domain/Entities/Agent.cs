namespace RPGBattleMaker.Domain.Entities;

public class Agent
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int Rarity { get; set; }
    public string SynergyText { get; set; }

    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int MaxLife { get; set; }
    public int BaseSkill { get; set; }

    public int TemporaryAttackBonus { get; set; } = 0;
    public int TemporaryDefenseBonus { get; set; } = 0;
    public int TemporarySkillBonus { get; set; } = 0;
    public int TemporaryMaxLifeBonus { get; set; } = 0;

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

        BaseAttack = ataque + TemporaryAttackBonus;   // Sem int.Parse!
        BaseDefense = defesa + TemporaryDefenseBonus; // Sem int.Parse!
        MaxLife = vida + TemporaryMaxLifeBonus;       // Sem int.Parse!
        BaseSkill = pericia + TemporarySkillBonus;   // Sem int.Parse!

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

        TemporaryAttackBonus = 0;
        TemporaryDefenseBonus = 0;
        TemporarySkillBonus = 0;
        TemporaryMaxLifeBonus = 0;
    }

    public int GetAttr(string attrName)
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


