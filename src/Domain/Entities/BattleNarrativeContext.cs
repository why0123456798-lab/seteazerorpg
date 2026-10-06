namespace RPGBattleMaker.Domain.Entities;

public sealed class BattleNarrativeContext
{
    public int Level { get; init; }
    public string Theme { get; init; } = string.Empty;
    public Agent Hero { get; init; } = null!;
    public int Round { get; init; }
    public int D20 { get; init; }
    public int Total { get; init; }
    public int Dc { get; init; }
    public bool IsInitial { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string OutcomeDetail { get; init; } = string.Empty;
    public IReadOnlyList<Agent> Team { get; init; } = Array.Empty<Agent>();
    public IReadOnlyList<Item> Items { get; init; } = Array.Empty<Item>();
    public IReadOnlyCollection<string> PreviousStories { get; init; } = Array.Empty<string>();
}
