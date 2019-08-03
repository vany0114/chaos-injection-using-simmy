using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Duber.Infrastructure.Chaos;
using Duber.Infrastructure.Chaos.IoC;
using Duber.Infrastructure.EventBus.Abstractions;
using Duber.Infrastructure.EventBus.RabbitMQ.IoC;
using Duber.Infrastructure.EventBus.ServiceBus.IoC;
using Duber.Infrastructure.Resilience.Abstractions;
using Duber.WebSite.Application.IntegrationEvents.Events;
using Duber.WebSite.Application.IntegrationEvents.Handlers;
using Duber.WebSite.Extensions;
using Duber.WebSite.Hubs;
using Duber.WebSite.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;
// ReSharper disable InconsistentNaming
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable AssignNullToNotNullAttribute

namespace Duber.WebSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache()
                .Configure<FormOptions>(x => x.ValueCountLimit = 2048)
                .AddApplicationInsightsTelemetry(Configuration)
                .AddMvc();
            
            services.AddSignalR();

            services.Configure<TripApiSettings>(Configuration.GetSection("TripApiSettings"))
                .AddResilientStrategies(Configuration)
                .AddChaosApiHttpClient(Configuration)
                .AddPersistenceAndRepositories(Configuration);

            // service bus configuration
            if (Configuration.GetValue<bool>("AzureServiceBusEnabled"))
            {
                services.AddServiceBus(Configuration);
            }
            else
            {
                services.AddRabbitMQ(Configuration);
            }

            RegisterEventBusHandlers(services);

            var container = new ContainerBuilder();
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                // We only want to add Simmy chaos injection in stage or prod environments in order to test out our resiliency.
                // Wrap every policy in the policy registry in Simmy chaos injectors.
                var httpPolicyRegistry = app.ApplicationServices.GetRequiredService<IPolicyRegistry<string>>();
                var sqlPolicyExecutor = app.ApplicationServices.GetRequiredService<IPolicyAsyncExecutor>();
                httpPolicyRegistry?.AddHttpChaosInjectors();
                sqlPolicyExecutor?.PolicyRegistry?.AddChaosInjectors();
            }

            app.UseSignalR(routes =>
            {
                routes.MapHub<TripHub>("/triphub");
            });

            ConfigureEventBusEvents(app);
            app.UseStaticFiles();

            if (Configuration.GetValue<bool>("UseAzureAppConfiguration"))
                app.UseAzureAppConfiguration();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void RegisterEventBusHandlers(IServiceCollection services)
        {
            services.AddTransient<TripCreatedIntegrationEventHandler>();
            services.AddTransient<TripUpdatedIntegrationEventHandler>();
            services.AddTransient<InvoiceCreatedIntegrationEventHandler>();
            services.AddTransient<InvoicePaidIntegrationEventHandler>();
        }

        private void ConfigureEventBusEvents(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            eventBus.Subscribe<TripCreatedIntegrationEvent, TripCreatedIntegrationEventHandler>();
            eventBus.Subscribe<TripUpdatedIntegrationEvent, TripUpdatedIntegrationEventHandler>();
            eventBus.Subscribe<InvoiceCreatedIntegrationEvent, InvoiceCreatedIntegrationEventHandler>();
            eventBus.Subscribe<InvoicePaidIntegrationEvent, InvoicePaidIntegrationEventHandler>();
        }
    }
}
