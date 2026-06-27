using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Data.Interface
{
    public interface IAgentRepository
    {
        Task GetAllHeroes(List<Agent> allAgents);
        Task<Agent> GetHeroByName(string agent);
        Task<List<string>> GetHeroSynergies(string heroName);
    }
}
