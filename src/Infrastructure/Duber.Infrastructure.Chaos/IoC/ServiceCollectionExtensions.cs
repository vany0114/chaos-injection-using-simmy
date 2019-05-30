using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

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

            services.AddScoped<Lazy<Task<GeneralChaosSetting>>>(sp =>
            {
                // we use LazyThreadSafetyMode.None in order to avoid locking.
                var chaosApiHttpClient = sp.GetRequiredService<ChaosApiHttpClient>();
                return new Lazy<Task<GeneralChaosSetting>>(() => chaosApiHttpClient.GetGeneralChaosSettings(), LazyThreadSafetyMode.None);
            });

            return services;
        }
    }
}
