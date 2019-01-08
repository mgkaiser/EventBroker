using System;
using EventBrokerDispatcher.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Options;
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
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace EventBrokerDispatcher
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }

        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {                          
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", reloadOnChange: true, optional: true);

                    Configuration = builder.Build();

                    var elasticUri = Configuration.GetSection("ElasticConfiguration:Uri")?.Value;

                    Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("Application", Configuration.GetSection("ElasticConfiguration:Application")?.Value)
                        .Enrich.WithProperty("FriendlyName", System.AppDomain.CurrentDomain.FriendlyName)
                        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
                        {
                            AutoRegisterTemplate = true,
                        })
                    .CreateLogger();

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
                        var config = provider.GetRequiredService<IConfig>();
                        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger<Program>();

                        var host = cfg.Host(Configuration.GetSection("rabbitMQSetting:RabbitMQServer")?.Value, Configuration.GetSection("rabbitMQSetting:RabbitMQVirtualHost")?.Value ?? "/", h => { 
                            h.Username(Configuration.GetSection("rabbitMQSetting:RabbitMQUsername")?.Value);
                            h.Password(Configuration.GetSection("rabbitMQSetting:RabbitMQPassword")?.Value);
                        });

                        cfg.UseDelayedExchangeMessageScheduler();
                                                
                        foreach (var queue in config.eventBrokerQueues.queues)
                        {
                            cfg.ReceiveEndpoint(host, $"EventBrokerDispatcher_{queue.queueName}_queue", x =>
                            {                                
                                x.BindMessageExchanges = false;        
                                foreach (var binding in queue.bindings)   
                                {
                                    logger.LogInformation($"Binding {queue.queueName} to SenderId {binding.senderId} : EventType {binding.eventType}.");
                                    x.Bind("EventBrokerInterfaces:IEGTEvent", bindingConfig =>
                                    {
                                        bindingConfig.ExchangeType = ExchangeType.Direct;
                                        bindingConfig.RoutingKey = $"{binding.senderId}:{binding.eventType}";
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


