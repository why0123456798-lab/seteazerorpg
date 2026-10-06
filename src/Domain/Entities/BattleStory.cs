namespace RPGBattleMaker.Domain.Entities;

public sealed class BattleStory
{
    public string Title { get; }
    public string Narrative { get; }

    public BattleStory(string title, string narrative)
    {
        Title = title;
        Narrative = narrative;
    }
}
