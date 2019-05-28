using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Duber.Infrastructure.Chaos;
using Duber.Infrastructure.Chaos.IoC;
using Duber.Infrastructure.EventBus.Abstractions;
using Duber.Infrastructure.EventBus.RabbitMQ.IoC;
using Duber.Infrastructure.EventBus.ServiceBus.IoC;
using Duber.Infrastructure.Resilience.Abstractions;
using Duber.Invoice.API.Application.IntegrationEvents.Events;
using Duber.Invoice.API.Application.IntegrationEvents.Hnadlers;
using Duber.Invoice.API.Application.Validations;
using Duber.Invoice.API.Extensions;
using Duber.Invoice.API.Infrastructure.AutofacModules;
using Duber.Invoice.API.Infrastructure.Filters;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.Registry;
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
#pragma warning disable 618

namespace Duber.Invoice.API
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
            services.AddAutoMapper()
                .AddApplicationInsightsTelemetry(Configuration)
                .AddMvc(options =>
                {
                    options.Filters.Add(typeof(HttpGlobalExceptionFilter));
                    options.Filters.Add(typeof(ValidatorActionFilter));
                })
                .AddFluentValidation(x => x.RegisterValidatorsFromAssemblyContaining<CreateInvoiceRequestValidator>());

            services.AddOptions()
                .AddCors(options =>
                {
                    options.AddPolicy("CorsPolicy",
                        builder => builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials());
                });

            services.AddResilientStrategies(Configuration)
                .AddChaosApiHttpClient(Configuration)
                .AddPersistenceAndRepository(Configuration)
                .AddPaymentService(Configuration)
                .AddCustomSwagger();
            
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

            //configure autofac
            var container = new ContainerBuilder();
            container.Populate(services);
            container.RegisterModule(new MediatorModule());

            return new AutofacServiceProvider(container.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // We only want to add Simmy chaos injection in stage or prod environments in order to test out our resiliency.
                // Wrap every policy in the policy registry in Simmy chaos injectors.
                var httpPoliciesRegistry = app.ApplicationServices.GetRequiredService<IPolicyRegistry<string>>();
                var sqlPoliciesRegistry = app.ApplicationServices.GetRequiredService<IPolicyAsyncExecutor>();
                httpPoliciesRegistry?.AddHttpChaosInjectors();
                sqlPoliciesRegistry.PolicyRegistry.AddChaosInjectors();
            }

            app.UseCors("CorsPolicy");
            app.UseMvc();
            ConfigureEventBusEvents(app);

            app.UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Duber.Invoice V1");
                    c.RoutePrefix = string.Empty;
                });
        }

        private void RegisterEventBusHandlers(IServiceCollection services)
        {
            services.AddTransient<TripCancelledIntegrationEventHandler>();
            services.AddTransient<TripFinishedIntegrationEventHandler>();
        }

        private void ConfigureEventBusEvents(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            eventBus.Subscribe<TripCancelledIntegrationEvent, TripCancelledIntegrationEventHandler>();
            eventBus.Subscribe<TripFinishedIntegrationEvent, TripFinishedIntegrationEventHandler>();
        }
    }
}
