using RPGBattleMaker.Data.Interface;
using RPGBattleMaker.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Text;

namespace RPGBattleMaker.Infrastructure
{
    public class AgentService : IAgentService
    {
        private Dictionary<string, Image> imageCache = new Dictionary<string, Image>();
        private string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        private readonly IAgentRepository _agentRepository;

        public AgentService(IAgentRepository agentRepository)
        {
            _agentRepository = agentRepository;
        }

        public async Task<Image> GetAgentImage(Agent agent, Size size)
        {
            string cacheKey = $"{agent.Name}_{size.Width}x{size.Height}";
            if (imageCache.ContainsKey(cacheKey)) return imageCache[cacheKey];

            string imgPath = Path.Combine(baseDir, "shared/images", agent.ImageFilename + ".png");
            if (!File.Exists(imgPath)) imgPath = Path.Combine(baseDir, "shared/images", agent.ImageFilename + ".jpg");

            if (File.Exists(imgPath))
            {
                try
                {
                    Image rawImg = Image.FromFile(imgPath);
                    Bitmap resized = new Bitmap(size.Width, size.Height);
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(rawImg, 0, 0, size.Width, size.Height);
                    }
                    rawImg.Dispose();
                    imageCache[cacheKey] = resized;
                    return resized;
                }
                catch { }
            }

            // Placeholder caso a imagem falte
            Bitmap placeholder = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(placeholder))
            {
                g.Clear(ColorTranslator.FromHtml("#555555"));
            }
            imageCache[cacheKey] = placeholder;
            return placeholder;
        }

        public async Task<string> GetSynergyName(Agent agent)
        {
            var synergyAgents = new SynergyAgents();

            var heroSynergies = await GetSynergiesForHero(agent.Name);

            if (!string.IsNullOrEmpty(heroSynergies))
            {
                return $"Sinergia: {heroSynergies}";
            }
            else
            {
                return "";
            }
        }

        public async Task GetAllHeroes(List<Agent> allAgents)
        {
            await _agentRepository.GetAllHeroes(allAgents);
        }

        // Função que descobre TODAS as sinergias que um determinado herói possui
        public async Task<string> GetSynergiesForHero(string heroName)
        {
            var result = await _agentRepository.GetHeroSynergies(heroName);

            return string.Join(", ", result);
        }

        public async Task<int> GetSynergyBonus(Agent currentAgent, List<Agent> teamAgents)
        {
            int bonus = 0;

            // Buscando assincronamente as sinergias do agente atual usando o parâmetro recebido
            List<string> mySynergies = await _agentRepository.GetHeroSynergies(currentAgent.Name);

            if (mySynergies.Count == 0) return 0;

            // Criando o mapa. Como o repositório é async, buscamos os dados antes de montar o Dictionary
            var teamSynergiesMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var agent in teamAgents)
            {
                var synergies = await _agentRepository.GetHeroSynergies(agent.Name);
                teamSynergiesMap[agent.Name] = synergies;
            }

            foreach (var synergyName in mySynergies)
            {
                int count = teamSynergiesMap.Values.Count(heroSynergies =>
                    heroSynergies.Contains(synergyName, StringComparer.OrdinalIgnoreCase));

                if (synergyName.Equals("Solo", StringComparison.OrdinalIgnoreCase))
                {
                    if (teamAgents.Count == 1)
                    {
                        bonus += 4;
                    }
                }
                else if (synergyName.Equals("Líder", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Trindade", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Ordem", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Nova Ordem", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Vendedores", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Hara-Kiri", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("A chama", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Uagamora", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Ninho do Dragão", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Dragão", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Caos", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Fé", StringComparison.OrdinalIgnoreCase))
                {
                    if (count >= 2)
                    {
                        bonus += 1 + (count - 2);
                    }
                }
                else if (synergyName.Equals("Irmãos", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Irmãs", StringComparison.OrdinalIgnoreCase))
                {
                    if (count >= 2)
                    {
                        bonus += 3;
                    }
                }
                else if (synergyName.Equals("Amor", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Amor Platônico", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Casal Real", StringComparison.OrdinalIgnoreCase) ||
                         synergyName.Equals("Vilão", StringComparison.OrdinalIgnoreCase))
                {
                    if (count >= 2)
                    {
                        bonus += 2;
                    }
                }
            }

            return bonus;
        }
    }
}
