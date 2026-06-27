using RPGBattleMaker.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Text;

namespace RPGBattleMaker.Infrastructure
{
    public class GameService : IGameService
    {
        private Random random = new Random();
        public GameService()
        {
            
        }

        public List<Agent> GetMarketPool(List<Agent> team, List<Agent> allAgents)
        {
            var teamNames = team.Select(a => a.Name).ToList();
            return allAgents.Where(a => !teamNames.Contains(a.Name)).ToList();
        }

        public List<Agent> RollMarket(List<Agent> team, List<Agent> allAgents)
        {
            var pool = GetMarketPool(team, allAgents);
            if (pool.Count == 0) return new List<Agent>();

            var rarityWeights = new Dictionary<int, double> { { 1, 0.40 }, { 2, 0.30 }, { 3, 0.15 }, { 4, 0.10 }, { 5, 0.05 } };
            List<Agent> marketSlots = new List<Agent>();
            int slotsNecessarios = Math.Min(4, pool.Count);

            while (marketSlots.Count < slotsNecessarios)
            {
                var opcoesDisponiveis = pool.Where(a => !marketSlots.Contains(a)).ToList();
                if (opcoesDisponiveis.Count == 0) break;

                var pesos = opcoesDisponiveis.Select(a => rarityWeights.ContainsKey(a.Rarity) ? rarityWeights[a.Rarity] : 0.0).ToList();
                if (pesos.Sum() == 0) pesos = Enumerable.Repeat(1.0, opcoesDisponiveis.Count).ToList();

                // Roleta ponderada simples
                double sum = pesos.Sum();
                double r = random.NextDouble() * sum;
                double currentSum = 0;
                Agent chosen = opcoesDisponiveis.Last();

                for (int i = 0; i < opcoesDisponiveis.Count; i++)
                {
                    currentSum += pesos[i];
                    if (r <= currentSum)
                    {
                        chosen = opcoesDisponiveis[i];
                        break;
                    }
                }
                marketSlots.Add(chosen);
            }
            return marketSlots;
        }
    }
}
