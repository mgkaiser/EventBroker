﻿using System;
using EventBrokerDispatcher.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Configuration;
using Karambolo.Extensions.Logging.File;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using EventBrokerConfig;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using EventBrokerDispatcher.Consumers;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit;
using RabbitMQ.Client;
using EventBrokerInterfaces;

namespace EventBrokerDispatcher
{
    class Program
    {
        private static IConfig _config = new Config();

        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {                                       
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

                    // Register the consumers
                    services.AddScoped<EGTEventConsumer>();             
                    
                    // Attach consumers to Mass Transit
                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<EGTEventConsumer>();
                    });

                    // Setup the Mass Transit Bus
                    services.AddSingleton(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                    {
                        var host = cfg.Host(_config.RabbitMQServer, _config.RabbitMQVirtualHost, h => { 
                            h.Username(_config.RabbitMQUsername);
                            h.Password(_config.RabbitMQPassword);
                        });

                        cfg.UseDelayedExchangeMessageScheduler();

                        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

                        foreach (var queue in _config.eventBrokerQueues.queues)
                        {
                            cfg.ReceiveEndpoint(host, $"EventBrokerDispatcher_{queue.queueName}_queue", x =>
                            {                                
                                x.BindMessageExchanges = false;        
                                foreach (var binding in queue.bindings)   
                                {
                                    logger.LogInformation($"Binding {queue.queueName} to SenderId {binding.senderId} : EventType {binding.eventType}.");
                                    x.Bind("EventBrokerInterfaces:IEGTEvent", config =>
                                    {
                                        config.ExchangeType = ExchangeType.Direct;
                                        config.RoutingKey = $"{binding.senderId}:{binding.eventType}";
                                    });
                                }                                       
                                x.Consumer<EGTEventConsumer>(
                                ()=> { 
                                    var consumer = provider.GetRequiredService<EGTEventConsumer>(); 
                                    consumer.Url = queue.url;
                                    return consumer;
                                },
                                consumer => {
                                    consumer.Message<IEGTEvent>(msg => msg.UseScheduledRedelivery(Retry.Incremental(10, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))));
                                });                                                                
                            });
                        };                
                    }));

                    // Register the bus
                    services.AddSingleton<IPublishEndpoint>(provider => provider.GetRequiredService<IBusControl>());
                    services.AddSingleton<ISendEndpointProvider>(provider => provider.GetRequiredService<IBusControl>());
                    services.AddSingleton<IBus>(provider => provider.GetRequiredService<IBusControl>());

                    // Register the consumer
                    services.AddScoped(provider => provider.GetRequiredService<IBus>().CreateRequestClient<EGTEventConsumer>());

                    // Register the service
                     services.AddHostedService<Dispatcher>();
               
                })
                .RunConsoleAsync();
        }    
    }
}


