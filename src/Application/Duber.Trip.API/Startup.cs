using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Duber.Infrastructure.EventBus.RabbitMQ.IoC;
using Duber.Infrastructure.EventBus.ServiceBus.IoC;
using Duber.Trip.API.Application.Validations;
using Duber.Trip.API.Infrastructure.Filters;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Duber.Trip.API.Extensions;
// ReSharper disable InconsistentNaming

namespace Duber.Trip.API
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
                .AddFluentValidation(x => x.RegisterValidatorsFromAssemblyContaining<UpdateTripCommandValidator>());

            services.AddCQRS(Configuration);

            services.AddOptions()
                .AddCors(options =>
                {
                    options.AddPolicy("CorsPolicy",
                        builder => builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials());
                });

            services.AddCustomSwagger();

            // service bus configuration
            if (Configuration.GetValue<bool>("AzureServiceBusEnabled"))
            {
                services.AddServiceBus(Configuration);
            }
            else
            {
                services.AddRabbitMQ(Configuration);
            }

            var container = new ContainerBuilder();
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("CorsPolicy");
            app.UseMvc();

            app.UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Duber.Trip V1");
                    c.RoutePrefix = string.Empty;
                });
        }
    }
}
