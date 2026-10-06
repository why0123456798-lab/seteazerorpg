using RPGBattleMaker.Domain.Entities;

namespace RPGBattleMaker.Application.Interfaces;

public interface IBattleNarrator
{
    Task<BattleStory?> GenerateAsync(BattleNarrativeContext context);
}
