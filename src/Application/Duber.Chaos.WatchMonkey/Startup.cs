using Duber.Infrastructure.Chaos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Duber.Chaos.WatchMonkey.Startup))]
namespace Duber.Chaos.WatchMonkey
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient<ChaosApiHttpClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ChaosApiSettings:BaseUrl"));
            });
        }
    }
}
