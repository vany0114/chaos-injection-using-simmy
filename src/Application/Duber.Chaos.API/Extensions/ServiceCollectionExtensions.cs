using Duber.Chaos.API.Infrastructure.Repository;
using Duber.Infrastructure.Chaos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

namespace Duber.Chaos.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            // swagger configuration
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info
                {
                    Title = "Duber.Chaos HTTP API",
                    Version = "v1",
                    Description = "The Duber Chaos Service HTTP API",
                    TermsOfService = "Terms Of Service"
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            return services;
        }

        public static IServiceCollection AddDistributedCache(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment environment)
        {
            // we don't need using cache when using AzureAppConfiguration.
            if (configuration.GetValue<bool>("UseAzureAppConfiguration"))
                return services;

            if (environment.IsDevelopment())
            {
                services.AddMemoryCache();
                services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddDistributedRedisCache(option =>
                {
                    option.Configuration = configuration.GetValue<string>("ConnectionStrings:ChaosDB");
                    option.InstanceName = "master";
                });
            }

            return services;
        }

        public static IServiceCollection AddRepository(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration.GetValue<bool>("UseAzureAppConfiguration"))
                services.AddTransient<IChaosRepository, AzureAppConfigurationRepository>();
            else
                services.AddTransient<IChaosRepository, ChaosRepository>();

            services.Configure<GeneralChaosSetting>(configuration.GetSection("GeneralChaosSetting"));
            return services;
        }
    }
}
