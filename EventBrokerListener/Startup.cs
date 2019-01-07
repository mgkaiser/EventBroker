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

namespace EventBrokerListener
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private static IConfig _config = new Config();

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Register Config
            services.AddSingleton<IConfig>(_config);

            // Register Logging
            services.AddLogging(lb =>
            {
                lb.AddConfiguration(_config.Logging);
                lb.AddFile(o => o.RootPath = (_config.LoggingRoot ?? Directory.GetCurrentDirectory()));                                                        
                lb.AddConsole();
                lb.AddDebug();                
            });                                     

            // Setup the Mass Transit Bus
            services.AddSingleton(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(_config.RabbitMQServer, _config.RabbitMQVirtualHost, h => { 
                    h.Username(_config.RabbitMQUsername);
                    h.Password(_config.RabbitMQPassword);
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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
