using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Infrastructure
{
    public static class AgentHelper
    {
        public static string MappingTypes(string agentType)
        {
            if (agentType == AgentType.Lutador) return "⚔️";
            if (agentType == AgentType.Defensor) return "🛡️";
            if (agentType == AgentType.Especialista) return "🎯";
            if (agentType == AgentType.Suporte) return "❤️";
            return agentType;
        }
    }
}
