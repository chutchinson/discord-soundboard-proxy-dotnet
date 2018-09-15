using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Soundboard.Options;
using Soundboard.Services;
using Soundboard.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soundboard
{
    static class Program
    {
        static IServiceProvider BuildApplication()
        {
            // Setup dependency injection container

            var services = new ServiceCollection();

            // Logging

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(path: "soundboard.log", encoding: Encoding.UTF8)
                .CreateLogger();

            services.AddLogging(logging => logging
                .AddDebug()
                .AddSerilog());

            // Configuration

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: true, reloadOnChange: true)
                .Build();

            services.Configure<SoundboardOptions>(configuration);

            // Services

            services.AddScoped<KeybindingService>();
            services.AddScoped<SoundboardProxyService>();
            services.AddScoped<MainView>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = BuildApplication();

            using (var scope = services.CreateScope())
            {
                var view = scope.ServiceProvider.GetRequiredService<MainView>();
                Application.Run(view);
            }
        }
    }
}
