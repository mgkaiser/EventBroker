using System;
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

namespace EventBrokerDispatcher
{
    class Program
    {
        private static ManualResetEvent _Shutdown = new ManualResetEvent(false);
        private static ManualResetEventSlim _Complete = new ManualResetEventSlim();
        
        private static Lazy<ILogger> _logger = new Lazy<ILogger>(()=>{            
            return _serviceProvider.Value.GetService<ILoggerFactory>().CreateLogger<Program>();
        });
        
        private static Lazy<IServiceProvider> _serviceProvider = new Lazy<IServiceProvider>(() => {
            // Setup our ServiceCollection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Setup our ServiceProvider
            return serviceCollection.BuildServiceProvider(); 
        });

        static async Task<int> Main(string[] args)
        {                        
            try
            {
                _logger.Value.LogInformation("Starting EventBrokerDispatcher");            

                var ended = new ManualResetEventSlim();
                var starting = new ManualResetEventSlim();

                AssemblyLoadContext.Default.Unloading += (obj) =>{
                    _logger.Value.LogInformation("Received Shutdown Signal");
                    _Shutdown.Set();
                    _Complete.Wait();        
                };
                                  
                // Do the actual work here
                var dispatcher = _serviceProvider.Value.GetService<IDispatcher>();
                await dispatcher.Start();
                
                // Wait for a singnal
                _Shutdown.WaitOne();
            }
            catch (Exception ex)
            {
                _logger.Value.LogError(ex, ex.Message);                
            }
            finally
            {
                _logger.Value.LogInformation("Ending EventBrokerDispatcher");
            }
            
            _Complete.Set();
 
            return 0;
            
        }
        
        private static void ConfigureServices(IServiceCollection services)
        {                                 
            // Setup config
            IConfig config = new Config();
                                  
            // Setup services
            services
                .AddLogging(lb =>
                {
                    lb.AddConfiguration(config.Logging);
                    lb.AddFile(o => o.RootPath = (config.LoggingRoot ?? Directory.GetCurrentDirectory()));                                        
                    if (config.IsDevelopment)
                    {
                        lb.AddConsole();
                        lb.AddDebug();
                    }
                })                        
                .AddSingleton<IDispatcher, Dispatcher>()
                .AddSingleton<IConfig>(config);
        }

    }
}


