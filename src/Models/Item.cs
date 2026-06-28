using RPGBattleMaker.Data;

namespace RPGBattleMaker.Data
{
    public enum ItemEffect
    {
        BonusAtaque,
        BonusDefesa,
        BonusPericia,
        BonusHP,
        BonusEscudo,
        ReducaoDC
    }

    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public int Rarity { get; set; } // 1-5, igual aos heróis
        public ItemEffect Effect { get; set; }
        public int EffectValue { get; set; }
        public string Emoji { get; set; }

        public Item(string name, string description, int cost, int rarity, ItemEffect effect, int effectValue, string emoji)
        {
            Name = name;
            Description = description;
            Cost = cost;
            Rarity = rarity;
            Effect = effect;
            EffectValue = effectValue;
            Emoji = emoji;
        }
    }

    /// <summary>
    /// Catálogo estático de itens disponíveis na loja.
    /// Adicione novos itens aqui para expandir o jogo.
    /// </summary>
    public static class ItemCatalog
    {
        public static List<Item> AllItems => new List<Item>
        {
            // Comuns (Rarity 1) — custo baixo, bônus pequeno
            new Item("Adaga Enferrujada",   "Bônus de +1 Ataque ao time.",       1, 1, ItemEffect.BonusAtaque,  1, "🗡️"),
            new Item("Escudo de Madeira",   "Bônus de +1 Defesa ao time.",       1, 1, ItemEffect.BonusDefesa,  1, "🛡️"),
            new Item("Erva Medicinal",      "+5 HP máx. a todos os heróis.",     1, 1, ItemEffect.BonusHP,      5, "🌿"),
            new Item("Capa Surrada",        "+1 Perícia ao time.",               1, 1, ItemEffect.BonusPericia, 1, "🧥"),

            // Incomuns (Rarity 2)
            new Item("Espada Curta",        "Bônus de +2 Ataque ao time.",       2, 2, ItemEffect.BonusAtaque,  2, "⚔️"),
            new Item("Armadura de Couro",   "Bônus de +2 Defesa ao time.",       2, 2, ItemEffect.BonusDefesa,  2, "🪖"),
            new Item("Poção de Cura",       "+10 HP máx. a todos os heróis.",    2, 2, ItemEffect.BonusHP,     10, "🧪"),
            new Item("Amuleto do Sábio",    "+2 Perícia ao time.",               2, 2, ItemEffect.BonusPericia, 2, "📿"),
            new Item("Escudo de Ferro",     "+1 de Escudo extra na batalha.",    2, 2, ItemEffect.BonusEscudo,  1, "🔰"),

            // Raros (Rarity 3)
            new Item("Machado de Guerra",   "Bônus de +3 Ataque ao time.",       3, 3, ItemEffect.BonusAtaque,  3, "🪓"),
            new Item("Peitoral de Aço",     "Bônus de +3 Defesa ao time.",       3, 3, ItemEffect.BonusDefesa,  3, "⚙️"),
            new Item("Elixir Revigorante",  "+15 HP máx. a todos os heróis.",    3, 3, ItemEffect.BonusHP,     15, "💉"),
            new Item("Tomo Arcano",         "+3 Perícia ao time.",               3, 3, ItemEffect.BonusPericia, 3, "📖"),
            new Item("Talisman Protetor",   "+2 de Escudo extra na batalha.",    3, 3, ItemEffect.BonusEscudo,  2, "🧿"),
            new Item("Mapa das Fraquezas",  "Reduz DC alvo em -1.",             3, 3, ItemEffect.ReducaoDC,    1, "🗺️"),

            // Épicos (Rarity 4)
            new Item("Espada Longa Rúnica", "Bônus de +5 Ataque ao time.",       5, 4, ItemEffect.BonusAtaque,  5, "🌟"),
            new Item("Armadura Dracônica",  "Bônus de +5 Defesa ao time.",       5, 4, ItemEffect.BonusDefesa,  5, "🐉"),
            new Item("Fonte da Juventude",  "+20 HP máx. a todos os heróis.",    5, 4, ItemEffect.BonusHP,     20, "⛲"),
            new Item("Olho do Oráculo",     "Reduz DC alvo em -2.",             5, 4, ItemEffect.ReducaoDC,    2, "👁️"),
            new Item("Escudo Sagrado",      "+3 de Escudo extra na batalha.",    5, 4, ItemEffect.BonusEscudo,  3, "✨"),

            // Lendários (Rarity 5)
            new Item("Lâmina do Caos",      "Bônus de +8 Ataque ao time.",       8, 5, ItemEffect.BonusAtaque,  8, "💥"),
            new Item("Égide Imponente",     "Bônus de +8 Defesa ao time.",       8, 5, ItemEffect.BonusDefesa,  8, "🏛️"),
            new Item("Coração de Dragão",   "+30 HP máx. a todos os heróis.",    8, 5, ItemEffect.BonusHP,     30, "❤️‍🔥"),
            new Item("Segredo dos Deuses",  "Reduz DC alvo em -3.",             8, 5, ItemEffect.ReducaoDC,    3, "🌌"),
        };
    }
}