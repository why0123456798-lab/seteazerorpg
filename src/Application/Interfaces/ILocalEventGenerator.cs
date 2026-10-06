using RPGBattleMaker.Domain.Entities;

namespace RPGBattleMaker.Application.Interfaces;

public interface ILocalEventGenerator
{
    Task<Event?> GenerateAsync(
        Event mechanicsTemplate,
        IReadOnlyList<Agent> team,
        IReadOnlyList<Item> items,
        int level,
        int gold,
        IReadOnlyCollection<string> recentEvents);
}
