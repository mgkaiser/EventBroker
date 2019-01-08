using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventBrokerInterfaces;
using EventBrokerConfig;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit;
using System.IO;
using RabbitMQ.Client;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace EventBrokerListener
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", reloadOnChange: true, optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            var elasticUri = Configuration["ElasticConfiguration:Uri"];

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", Configuration["ElasticConfiguration:Application"])
                .Enrich.WithProperty("FriendlyName", System.AppDomain.CurrentDomain.FriendlyName)
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
                {
                    AutoRegisterTemplate = true,
                })
            .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Register Config
            services.AddSingleton<IConfig, Config>();

            // Register Logging
            services.AddLogging(lb =>
            {
                lb.AddConfiguration(Configuration.GetSection("Logging"));
                lb.AddFile(o => o.RootPath = (Configuration.GetSection("Logging:File:LoggingRoot")?.Value ?? Directory.GetCurrentDirectory()));                                                        
                lb.AddConsole();
                lb.AddDebug();   
                lb.AddSerilog();             
            });                                     

            // Setup the Mass Transit Bus
            services.AddSingleton(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(Configuration.GetSection("rabbitMQSetting:RabbitMQServer")?.Value, Configuration.GetSection("rabbitMQSetting:RabbitMQVirtualHost")?.Value ?? "/", h => { 
                    h.Username(Configuration.GetSection("rabbitMQSetting:RabbitMQUsername")?.Value);
                    h.Password(Configuration.GetSection("rabbitMQSetting:RabbitMQPassword")?.Value);
                });

                cfg.UseDelayedExchangeMessageScheduler();

                cfg.Send<IEGTEvent>(x => {
                        x.UseRoutingKeyFormatter(context => {
                        var routingKey = $"{context.Message.SenderId}:{context.Message.EventType}";
                        System.Diagnostics.Debug.WriteLine($"RoutingKey={routingKey}");
                        return routingKey;
                    });
                });

                cfg.Publish<IEGTEvent>(x => x.ExchangeType = ExchangeType.Direct);
            }));

            // Register the bus
            services.AddSingleton<IPublishEndpoint>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<ISendEndpointProvider>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<IBus>(provider => provider.GetRequiredService<IBusControl>());            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {                
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
