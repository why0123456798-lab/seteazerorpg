using System;
using System.Windows.Forms;
using LLama.Native;
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
            // LLamaSharp must be configured before any native LLama API is touched.
            // CUDA runtime DLLs are not installed, so use the NVIDIA GPU through Vulkan.
            NativeLibraryConfig.All
                .WithCuda(false)
                .WithVulkan(true)
                .WithAutoFallback(false)
                .WithLogCallback((level, message) =>
                {
                    string logDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
                    Directory.CreateDirectory(logDirectory);
                    File.AppendAllText(
                        Path.Combine(logDirectory, "battle_ai_native.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}");
                });

            Environment.SetEnvironmentVariable("RPGBATTLE_LLM_BACKEND", "vulkan");

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
                    services.AddSingleton<IBattleNarrator, LocalBattleNarrator>();

                    services.AddTransient<GameGUI>();
                })
                .Build();

            ServiceHost.Start();

            var mainForm = ServiceHost.Services.GetRequiredService<GameGUI>();

            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}
