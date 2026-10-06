using RPGBattleMaker.Domain.Entities;

using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Application.Interfaces
{
    public interface IGameService
    {
        List<Agent> GetMarketPool(List<Agent> team, List<Agent> allAgents);
        List<Agent> RollMarket(List<Agent> team, List<Agent> allAgents, int level = 1);
    }
}

