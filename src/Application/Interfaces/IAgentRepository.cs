using RPGBattleMaker.Domain.Entities;

using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Application.Interfaces
{
    public interface IAgentRepository
    {
        Task GetAllHeroes(List<Agent> allAgents);
        Task<Agent> GetHeroById(int id);
        Task<List<string>> GetHeroSynergies(int heroId);
    }
}

