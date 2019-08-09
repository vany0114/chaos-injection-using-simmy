using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Duber.Infrastructure.Chaos.IoC
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChaosApiHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<ChaosApiHttpClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.BaseAddress = new Uri(configuration.GetValue<string>("ChaosApiSettings:BaseUrl"));
            });

            if (configuration.GetValue<bool>("UseAzureAppConfiguration"))
                services.Configure<GeneralChaosSetting>(configuration.GetSection("GeneralChaosSetting"));

            services.AddScoped<Lazy<Task<GeneralChaosSetting>>>(sp =>
            {
                if (configuration.GetValue<bool>("UseAzureAppConfiguration"))
                {
                    var chaosSettings = sp.GetRequiredService<IOptionsSnapshot<GeneralChaosSetting>>();
                    return new Lazy<Task<GeneralChaosSetting>>(() => Task.FromResult(chaosSettings.Value), LazyThreadSafetyMode.None);
                }

                // we use LazyThreadSafetyMode.None in order to avoid locking.
                var chaosApiHttpClient = sp.GetRequiredService<ChaosApiHttpClient>();
                return new Lazy<Task<GeneralChaosSetting>>(() => chaosApiHttpClient.GetGeneralChaosSettings(), LazyThreadSafetyMode.None);
            });

            return services;
        }
    }
}
