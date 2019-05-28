using Duber.Chaos.API.Infrastructure.Repository;
using Duber.Infrastructure.Chaos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

namespace Duber.Chaos.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddApplicationInsightsTelemetry(Configuration)
                .AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

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

            services.AddDistributedRedisCache(option =>
            {
                option.Configuration = Configuration.GetValue<string>("ConnectionString");
                option.InstanceName = "master";
            });

            services.AddSingleton<IChaosRepository, ChaosRepository>();
            services.Configure<GeneralChaosSetting>(Configuration.GetSection("GeneralChaosSetting"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.UseSwagger()
               .UseSwaggerUI(c =>
               {
                   c.SwaggerEndpoint("/swagger/v1/swagger.json", "Duber.Chaos V1");
                   c.RoutePrefix = string.Empty;
               });
        }
    }
}
