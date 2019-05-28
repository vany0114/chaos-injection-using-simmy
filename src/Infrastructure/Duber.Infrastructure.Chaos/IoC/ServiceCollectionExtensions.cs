using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

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

            services.AddScoped<GeneralChaosSetting>(sp =>
            {
                // TODO: This gonna get the chaos settings from the api per request instead of injecting the httpClient in order to avoid get the settings every time an object is created and add too latency.
                // Find a better way to get chaos settings per request...Another way migth be get settings directly from redis rather than api.
                var chaosApiHttpClient = sp.GetRequiredService<ChaosApiHttpClient>();
                return chaosApiHttpClient.GetGeneralChaosSettings()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            });

            return services;
        }
    }
}
