using System;
using Duber.Domain.Driver.Persistence;
using Duber.Domain.User.Persistence;
using Duber.Infrastructure.WebHost;
using Duber.WebSite.Infrastructure.Persistence;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Duber.WebSite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args)
                .MigrateDbContext<UserContext>((context, services) =>
                {
                    var logger = services.GetService<ILogger<UserContextSeed>>();
                    new UserContextSeed()
                        .SeedAsync(context, logger)
                        .Wait();
                })
                .MigrateDbContext<DriverContext>((context, services) =>
                {
                    var logger = services.GetService<ILogger<DriverContextSeed>>();
                    new DriverContextSeed()
                        .SeedAsync(context, logger)
                        .Wait();
                })
                .MigrateDbContext<ReportingContext>((_, __) => { })
                .Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    config.AddEnvironmentVariables();

                    var settings = config.Build();
                    if (settings.GetValue<bool>("UseAzureAppConfiguration"))
                    {
                        config.AddAzureAppConfiguration(options =>
                        {
                            options.Connect(settings["ConnectionStrings:AppConfig"])
                                .ConfigureRefresh(refresh =>
                                {
                                    refresh.Register("GeneralChaosSetting:Sentinel", refreshAll: true);
                                    refresh.SetCacheExpiration(TimeSpan.FromSeconds(1));
                                });
                        });
                    }
                })
                .ConfigureLogging((hostingContext, builder) =>
                {
                    builder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    builder.AddConsole();
                    builder.AddDebug();
                })
                .Build();
    }
}
