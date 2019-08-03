using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;

namespace Duber.Chaos.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
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
                });
    }
}
