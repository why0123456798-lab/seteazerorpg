using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Infrastructure.Interface
{
    public interface IAgentService
    {
        Task<Image> GetAgentImage(Agent agent, Size size);
        Task GetAllHeroes(List<Agent> allAgents);
        Task<string> GetSynergyName(Agent agent);
        Task<int> GetSynergyBonus(Agent currentAgent, List<Agent> teamAgents);
    }
}
