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

        public async Task GetAllHeroes(List<Agent> allAgents)
        {
            await _agentRepository.GetAllHeroes(allAgents);
        }
    }
}
