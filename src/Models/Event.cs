using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }

        public Event(int id, string name, string description, string optionA, string optionB, string optionC)
        {
            Id = id;
            Name = name;
            Description = description;
            OptionA = optionA;
            OptionB = optionB;
            OptionC = optionC;
        }
    }
    public class EventResult
    {
        /// <summary>Título curto do resultado. Ex: "Você aceitou o desafio!"</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Descrição detalhada do efeito. Ex: "+3g de recompensa recebidos."</summary>
        public string Description { get; set; } = string.Empty;

        public int AffectedAgentId { get; set; }

        /// <summary>True = resultado positivo (ícone ✅ verde), False = negativo (ícone ⚠️ vermelho).</summary>
        public bool IsPositive { get; set; } = true;

        public int GlobalCure { get; set; } = 0; // Bônus de Cura global para a próxima missão
        public int GlobalShield { get; set; } = 0; // Bônus de Cura global para a próxima missão
        public int PermanentAttack { get; set; } = 0; // Bônus de Ataque permanente
        public int HpBonus { get; set; } = 0; // Bônus de Ataque permanente
        public int GoldBonus { get; set; } = 0; // Bônus de Ataque permanente
        public int TemporarySkillBonus { get; set; } = 0;
        public int TemporarySkillBonusSupport { get; set; } = 0;
        public int HpBonusMaxLife { get; set; } = 0; // Bônus de Ataque permanente

        public int ExtraDc { get; set; } = 0;   


        public static bool GetEventIsPositive(int pericia, int dadoPC)
        {
            var random = new Random();
            var dado = random.Next(1,20);

            if ((pericia + dado) >= dadoPC)
                return true;

            return false;
        }
    }
}
