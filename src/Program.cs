using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPGBattleMaker.Data;
using RPGBattleMaker.Data.Interface;
using RPGBattleMaker.Infrastructure;
using RPGBattleMaker.Infrastructure.Interface;

namespace RPGBattleMaker
{
    internal static class Program
    {
        // O Host que guardará os serviços injetados
        public static IHost? ServiceHost { get; private set; }

        [STAThread]
        static void Main()
        {
            SQLitePCL.Batteries.Init();
            ApplicationConfiguration.Initialize();

            ServiceHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IDbContext, DbContext>();

                    services.AddSingleton<IAgentRepository, AgentRepository>();

                    services.AddSingleton<IAgentService, AgentService>();
                    services.AddSingleton<IGameService, GameService>();

                    services.AddTransient<GameGUI>();
                })
                .Build();

            ServiceHost.Start();

            var mainForm = ServiceHost.Services.GetRequiredService<GameGUI>();

            Application.Run(mainForm);
        }
    }
}