using RPGBattleMaker.Application.Interfaces;
using RPGBattleMaker.Domain.Entities;

namespace RPGBattleMaker.Application.Services;

public class EventService : IEventService
{
    private static readonly Random Random = Random.Shared;

    public Task<Event?> RandomEvent()
    {
        // Eventos acontecem em todos os níveis.
        // O Id define apenas qual conjunto de regras mecânicas será usado pelo C#.
        int mechanicsId = Random.Next(1, 4);

        Event mechanicsTemplate = mechanicsId switch
        {
            1 => new Event(
                1,
                "Evento",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty),

            2 => new Event(
                2,
                "Evento",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty),

            _ => new Event(
                3,
                "Evento",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty)
        };

        return Task.FromResult<Event?>(mechanicsTemplate);
    }

    public EventResult GetEventResult(
        Event events,
        int selectedValue,
        List<Agent> teamAgents)
    {
        var teamAgentsAlive = teamAgents.Where(w => w.CurrentLife > 0).ToList();

        if (teamAgentsAlive.Count == 0)
        {
            return new EventResult
            {
                Title = "Nenhum herói disponível",
                Description = "O grupo não tinha nenhum herói capaz de responder ao encontro."
            };
        }

        var agentAttackValue = teamAgentsAlive.MaxBy(m => m.BaseAttack)!;
        var agentPericiaValue = teamAgentsAlive.MaxBy(m => m.BaseSkill)!;
        var agentDefenseValue = teamAgentsAlive.MaxBy(m => m.BaseDefense)!;

        switch (events.Id)
        {
            case 1:
                var attackResult1 = EventResult.GetEventIsPositive(agentAttackValue.BaseAttack, 15);
                var skillResult1 = EventResult.GetEventIsPositive(agentPericiaValue.BaseSkill, 14);

                return selectedValue switch
                {
                    0 => new EventResult
                    {
                        Title = events.OptionA ?? "Escolha física",
                        Description = attackResult1
                            ? "A abordagem física funciona. O herói ganha +2 de Ataque permanente."
                            : "A abordagem falha e o herói perde -3 de Vida atual.",
                        AffectedAgentId = agentAttackValue.Id,
                        IsPositive = attackResult1,
                        PermanentAttack = attackResult1 ? 2 : 0,
                        HpBonus = attackResult1 ? 0 : -3
                    },

                    1 => new EventResult
                    {
                        Title = events.OptionB ?? "Escolha de perícia",
                        Description = skillResult1
                            ? "A análise cuidadosa encontra uma vantagem. O grupo ganha +2 de Cura global."
                            : "A tentativa falha e a próxima missão ganha +2 DC.",
                        AffectedAgentId = agentPericiaValue.Id,
                        IsPositive = skillResult1,
                        GlobalCure = skillResult1 ? 2 : 0,
                        ExtraDc = skillResult1 ? 0 : 2
                    },

                    _ => new EventResult
                    {
                        Title = events.OptionC ?? "Escolha cautelosa",
                        Description = "O grupo decide não se envolver e segue adiante."
                    }
                };

            case 2:
                var defenseResult2 = EventResult.GetEventIsPositive(agentDefenseValue.BaseDefense, 16);
                var skillResult2 = EventResult.GetEventIsPositive(agentPericiaValue.BaseSkill, 15);

                return selectedValue switch
                {
                    0 => new EventResult
                    {
                        Title = events.OptionA ?? "Escolha física",
                        Description = defenseResult2
                            ? "A resistência é bem-sucedida. O herói ganha +3 de Vida máxima permanente."
                            : "A resistência falha. O herói inicia a próxima missão com apenas 1 de Vida.",
                        AffectedAgentId = agentDefenseValue.Id,
                        IsPositive = defenseResult2,
                        HpBonusMaxLife = defenseResult2 ? 3 : 0,
                        HpBonus = defenseResult2 ? 0 : -agentDefenseValue.CurrentLife + 1
                    },

                    1 => new EventResult
                    {
                        Title = events.OptionB ?? "Escolha de perícia",
                        Description = skillResult2
                            ? "A perícia resolve a situação. A próxima missão recebe -2 DC."
                            : "A tentativa falha. O Especialista perde -2 de Perícia até o fim da próxima missão.",
                        AffectedAgentId = agentPericiaValue.Id,
                        IsPositive = skillResult2,
                        ExtraDc = skillResult2 ? -2 : 0,
                        TemporarySkillBonus = skillResult2 ? 0 : -2
                    },

                    _ => new EventResult
                    {
                        Title = events.OptionC ?? "Escolha cautelosa",
                        Description = "O grupo evita o perigo e segue adiante."
                    }
                };

            case 3:
                var attackResult3 = EventResult.GetEventIsPositive(agentDefenseValue.BaseDefense, 17);
                var skillResult3 = EventResult.GetEventIsPositive(agentPericiaValue.BaseSkill, 15);

                return selectedValue switch
                {
                    0 => new EventResult
                    {
                        Title = events.OptionA ?? "Escolha física",
                        Description = skillResult3
                            ? "O grupo recebe uma bênção. Ganha +3 de Escudo global."
                            : "O ritual dá errado. O Suporte perde -2 de Perícia até o fim da próxima missão.",
                        AffectedAgentId = agentPericiaValue.Id,
                        IsPositive = skillResult3,
                        GlobalShield = skillResult3 ? 3 : 0,
                        TemporarySkillBonusSupport = skillResult3 ? 0 : -2
                    },

                    1 => new EventResult
                    {
                        Title = events.OptionB ?? "Escolha de força",
                        Description = attackResult3
                            ? "A força abre caminho para uma recompensa. O grupo ganha 10 moedas de ouro."
                            : "A ação desperta uma ameaça. A próxima missão ganha +3 DC.",
                        AffectedAgentId = agentAttackValue.Id,
                        IsPositive = attackResult3,
                        GoldBonus = attackResult3 ? 10 : 0,
                        ExtraDc = attackResult3 ? 0 : 3
                    },

                    _ => new EventResult
                    {
                        Title = events.OptionC ?? "Escolha cautelosa",
                        Description = "O grupo respeita o perigo e segue adiante."
                    }
                };

            default:
                return new EventResult
                {
                    Title = "Evento desconhecido",
                    Description = "Nada aconteceu."
                };
        }
    }
}
