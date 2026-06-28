using RPGBattleMaker.Data.Interface;
using RPGBattleMaker.Infrastructure.Interface;
using RPGBattleMaker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Infrastructure
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private Random random = new Random();
        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<Event?> RandomEvent()
        {
            var eventOccurs = ChanceEvent();
            if(eventOccurs)
            {
                var events = await _eventRepository.GetAllEvents();
                var valueId = random.Next(1, events.Count);
                return events.FirstOrDefault(f => f.Id == valueId);
            }

            return null;
        }

        public bool ChanceEvent()
        {
            if (random.NextDouble() < 0.2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public EventResult GetEventResult(Event events, int selectedValue, List<Agent> teamAgents)
        {
            var teamAgentsAlive = teamAgents.Where(w => w.CurrentLife > 0);
            var agentAttackValue = teamAgentsAlive.MaxBy(m => m.BaseAttack);
            var agentPericiaValue = teamAgentsAlive.MaxBy(m => m.BaseSkill);
            var agentDefenseValue = teamAgentsAlive.MaxBy(m => m.BaseDefense);

            switch (events.Id)
            {
                case 1: // ex: "Mercador Misterioso"

                    var attackResult1 = EventResult.GetEventIsPositive(agentAttackValue.BaseAttack, 15);
                    var skillResult1 = EventResult.GetEventIsPositive(agentPericiaValue.BaseSkill, 14);

                    return selectedValue switch
                    {
                        0 => new EventResult // OptionA - Defense
                        {
                            Title = events.OptionA,
                            Description = attackResult1 ? "Ele se assusta e larga o pingente. O herói ganha +2 de Ataque permanente" : 
                            "O mercador puxa uma adaga oculta, corta o herói e foge. O herói perde -3 de Vida atual",
                            AffectedAgentId = agentAttackValue.Id,
                            IsPositive = attackResult1,
                            PermanentAttack = attackResult1 ? 2 : 0,
                            HpBonus = attackResult1 ? 0 : -3
                        },
                        1 => new EventResult // OptionB - Skill
                        {
                            Title = events.OptionB,
                            Description = skillResult1 ? "O Especialista percebe que o item é falso, mas encontra uma poção real escondida na carroça. Ganha +2 de Cura global para a próxima missão" : 
                            "O Especialista cai na lábia do mercador e compra uma bugiganga que amaldiçoa o grupo. A próxima missão ganha +2 DC",
                            AffectedAgentId = agentPericiaValue.Id,
                            IsPositive = skillResult1,
                            GlobalCure = skillResult1 ? 2 : 0,
                            ExtraDc = skillResult1 ? 0 : 2
                        },
                        _ => new EventResult // OptionC
                        {
                            Title = events.OptionC,
                            Description = "Nada Acontece."
                        }
                    };

                case 2: // ex: "Emboscada na Estrada"

                    var defenseResult2 = EventResult.GetEventIsPositive(agentDefenseValue.BaseDefense, 16);
                    var skillResult2 = EventResult.GetEventIsPositive(agentPericiaValue.BaseSkill, 15);

                    return selectedValue switch
                    {
                        0 => new EventResult
                        {
                            Title = events.OptionA,
                            Description = defenseResult2 ? "O corpo do Defensor absorve a energia. Ele ganha +3 de Vida máxima permanente" :
                            "O veneno mágico queima o herói por dentro. Ele inicia a próxima missão com apenas 1 de Vida atual.",
                            AffectedAgentId = agentDefenseValue.Id,
                            IsPositive = defenseResult2,
                            HpBonusMaxLife = defenseResult2 ? 3 : 0,
                            HpBonus = defenseResult2 ? 0 : -agentDefenseValue.CurrentLife + 1
                        },
                        1 => new EventResult
                        {
                            Title = events.OptionB,
                            Description = skillResult2 ? "O Especialista purifica o fluxo mágico do mapa. Reduz permanentemente a dificuldade da próxima missão em -2 DC." :
                            "A fonte explode em uma onda de choque. O Especialista perde -2 de Perícia até o final da próxima missão.",
                            AffectedAgentId = agentPericiaValue.Id,
                            IsPositive = skillResult2,
                            ExtraDc = skillResult2 ? -2 : 0,
                            TemporarySkillBonus = skillResult2 ? 0 : -2

                        },
                        _ => new EventResult
                        {
                            Title = events.OptionC,
                            Description = "Nada Acontece."
                        }
                    };

                case 3: // ex: "Templo Abandonado"

                    var attackResult3 = EventResult.GetEventIsPositive(agentDefenseValue.BaseDefense, 17);
                    var skillResult3 = EventResult.GetEventIsPositive(agentPericiaValue.BaseSkill, 15);

                    return selectedValue switch
                    {
                        0 => new EventResult
                        {
                            Title = events.OptionA,
                            Description = skillResult3 ? "Os deuses abençoam o grupo. Concede +2 de Escudo global na próxima missão." :
                            "O ritual é feito de forma errada e os espíritos se enfurecem. O Suporte perde -2 de Perícia até o final da próxima missão.",
                            AffectedAgentId = agentPericiaValue.Id,
                            IsPositive = skillResult3,
                            GlobalShield = skillResult3 ? 3 : 0,
                            TemporarySkillBonusSupport = skillResult3 ? 0 : -2
                        },
                        1 => new EventResult
                        {
                            Title = events.OptionB,
                            Description = attackResult3 ? "O Lutador quebra o mármore e saca relíquias de ouro. Garante 10 moedas de ouro." :
                            "Uma maldição ancestral cai sobre o grupo. A próxima missão inteira ganha +3 DC.",
                            AffectedAgentId = agentAttackValue.Id,
                            IsPositive = attackResult3,
                            GoldBonus = attackResult3 ? 10 : 0,
                            ExtraDc = attackResult3 ? 0 : 3

                        },
                        _ => new EventResult
                        {
                            Title = events.OptionC,
                            Description = "Nada Acontece."
                        }
                    };

                default:
                    return new EventResult
                    {
                        Title = "Evento desconhecido.",
                        Description = "Nada aconteceu."
                    };
            }
        }
    }
}
