namespace RPGBattleMaker.Domain.Entities;

public sealed class BattleStory
{
    public string Title { get; }
    public string Narrative { get; }
    public string? Theme { get; }

    public BattleStory(string title, string narrative, string? theme = null)
    {
        Title = title;
        Narrative = narrative;
        Theme = theme;
    }
}
