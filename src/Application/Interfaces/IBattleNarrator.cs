using RPGBattleMaker.Domain.Entities;

namespace RPGBattleMaker.Application.Interfaces;

public interface IBattleNarrator
{
    Task<BattleStory?> GenerateInitialAsync(
        int level,
        IReadOnlyCollection<string> previousStories,
        IReadOnlyCollection<string> recentEvents);

    Task<BattleStory?> GenerateAsync(BattleNarrativeContext context);

    Task<BattleStory?> GenerateConclusionAsync(
        int level,
        bool success,
        int successes,
        int failures,
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        IReadOnlyCollection<string> previousStories,
        IReadOnlyCollection<string> recentEvents);

    Task<Event?> GenerateEventAsync(
        int mechanicsId,
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        int level,
        int gold,
        IReadOnlyCollection<string> previousBattleStories,
        IReadOnlyCollection<string> recentEvents);
}
