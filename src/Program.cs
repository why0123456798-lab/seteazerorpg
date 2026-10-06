using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPGBattleMaker.Application.Interfaces;
using RPGBattleMaker.Application.Services;
using RPGBattleMaker.Infrastructure.AI;
using RPGBattleMaker.Infrastructure.Database;
using RPGBattleMaker.Infrastructure.Repositories;
using RPGBattleMaker.Presentation.Forms;

namespace RPGBattleMaker
{
    internal static class Program
    {
        // O Host que guardarÃ¡ os serviÃ§os injetados
        public static IHost? ServiceHost { get; private set; }

        [STAThread]
        static void Main()
        {
            SQLitePCL.Batteries.Init();
            ApplicationConfiguration.Initialize();

            ServiceHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IDatabaseInitializer, GameDatabaseInitializer>();

                    services.AddSingleton<IAgentRepository, AgentRepository>();

                    services.AddSingleton<IAgentService, AgentService>();
                    services.AddSingleton<IGameService, GameService>();
                    services.AddSingleton<IEventService, EventService>();
                    services.AddSingleton<ILocalEventGenerator, LocalEventGenerator>();

                    services.AddTransient<GameGUI>();
                })
                .Build();

            ServiceHost.Start();

            var mainForm = ServiceHost.Services.GetRequiredService<GameGUI>();

            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}
