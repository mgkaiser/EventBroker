using System;
using EventBrokerDispatcher.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.IO;
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
        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Get the config settings
                    var eventBrokerQueues = new EventBrokerQueues();
                    hostContext.Configuration.GetSection("eventBrokerQueues").Bind(eventBrokerQueues);

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
                        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();                        
                        var logger = loggerFactory.CreateLogger<Program>();                        

                        var host = cfg.Host(hostContext.Configuration.GetSection("rabbitMQSetting:RabbitMQServer")?.Value, hostContext.Configuration.GetSection("rabbitMQSetting:RabbitMQVirtualHost")?.Value ?? "/", h => { 
                            h.Username(hostContext.Configuration.GetSection("rabbitMQSetting:RabbitMQUsername")?.Value);
                            h.Password(hostContext.Configuration.GetSection("rabbitMQSetting:RabbitMQPassword")?.Value);
                        });

                        cfg.UseDelayedExchangeMessageScheduler();                        
                                                
                        foreach (var queue in eventBrokerQueues.queues)
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
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", reloadOnChange: true, optional: true);
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("Application", hostContext.Configuration.GetSection("ElasticConfiguration:Application")?.Value)
                        .Enrich.WithProperty("FriendlyName", System.AppDomain.CurrentDomain.FriendlyName)
                        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(hostContext.Configuration.GetSection("ElasticConfiguration:Uri")?.Value))
                        {
                            AutoRegisterTemplate = true,
                        })
                        .WriteTo.Console()
                        .WriteTo.File($"{hostContext.Configuration.GetSection("Serilog:LogRoot")?.Value}log-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                    configLogging.AddSerilog();
                })
                .RunConsoleAsync();
        }    
    }
}


