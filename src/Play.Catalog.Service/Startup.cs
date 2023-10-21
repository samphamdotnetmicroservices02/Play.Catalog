using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Play.Catalog.Service.Entities;
using Play.Common.HealthChecks;
using Play.Common.Identity;
using Play.Common.Logging;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Common.OpenTelemetry;
using Play.Common.Settings;

namespace Play.Catalog.Service
{
    public class Startup
    {
        private const string AllowedOriginSetting = "AllowedOrigin";

        private ServiceSettings serviceSettings;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

            services.AddMongo()
                    .AddMongoRepository<Item>("items")
                    .AddMassTransitWithMessageBroker(Configuration)
                    .AddJwtBearerAuthentication();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.Read, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireClaim("scope", "catalog.readaccess", "catalog.fullaccess");
                });

                options.AddPolicy(Policies.Write, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireClaim("scope", "catalog.writeaccess", "catalog.fullaccess");
                });
            });

            services.AddControllers(options =>
            {
                options.SuppressAsyncSuffixInActionNames = false;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Catalog.Service", Version = "v1" });
            });

            services.AddHealthChecks()
                    .AddMongoDb();

            services.AddSeqLogging(Configuration)
                .AddTracing(Configuration)
                .AddMetrics(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Catalog.Service v1"));

                app.UseCors(builder =>
                {
                    builder.WithOrigins(Configuration[AllowedOriginSetting])
                        .AllowAnyHeader() //Allows any the headers that the client want to send
                        .AllowAnyMethod(); //Allows any the methods the client side want to use including GET, POST, PUT and all other verbs
                });
            }

            /*
            * Prometheus:
            * With the metrics side, we also need to do one more thing and that is to enable or create or expose what's going to
            * be called the scraping endpoint. So this is the endpoint that tools like Prometheus can use in a giving interval,
            * start pulling down and pulling into Prometheus, the metrics that we've been collecting across the lifetime of the
            * application. This "UseOpenTelemetryPrometheusScrapingEndpoint" is going to stand up that endpoint that it actually
            * ends with /metrics. You can configure it if you want to, for us, that's going to be good enough
            */
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapPlayEconomyHealthCheck();
            });
        }
    }
}
