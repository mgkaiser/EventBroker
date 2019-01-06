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
    ///////////////////////////////////////////////////////////////////////////
    // DON'T change anything in this.  Define a partial class to finish this
    //
    // Provide the following:
    // private static async Task RunServices(CancellationToken token)
    // private static void ConfigureServices(IServiceCollection services)
    ///////////////////////////////////////////////////////////////////////////
    partial class Program
    {
        private static ManualResetEvent _Shutdown = new ManualResetEvent(false);
        private static ManualResetEventSlim _Complete = new ManualResetEventSlim();
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        
        private static Lazy<ILogger> _logger = new Lazy<ILogger>(()=>{            
            return _serviceProvider.Value.GetService<ILoggerFactory>().CreateLogger<Program>();
        });
        
        private static Lazy<IServiceProvider> _serviceProvider = new Lazy<IServiceProvider>(() => {
            // Setup our ServiceCollection
            var serviceCollection = new ServiceCollection();
            ConfigureServicesMain(serviceCollection);

            // Setup our ServiceProvider
            return serviceCollection.BuildServiceProvider(); 
        });
                
        static async Task<int> Main(string[] args)
        {                                    
            try
            {                
                _logger.Value.LogInformation($"Starting {System.AppDomain.CurrentDomain.FriendlyName}");            

                var ended = new ManualResetEventSlim();
                var starting = new ManualResetEventSlim();

                AssemblyLoadContext.Default.Unloading += (obj) =>{
                    _logger.Value.LogInformation("Received Shutdown Signal");
                    _cts.Cancel();
                    _Shutdown.Set();
                    _Complete.Wait();        
                };
                                  
                // Do the actual work here
                await RunServices(_cts.Token);
                                
                // Wait for a singnal
                _Shutdown.WaitOne();
            }
            catch (TaskCanceledException taskCaneledException)
            {
                _logger.Value.LogWarning(taskCaneledException.Message);
            }
            catch (Exception ex)
            {
                _logger.Value.LogError(ex, ex.Message);                
            }
            finally
            {
                _logger.Value.LogInformation($"Ending {System.AppDomain.CurrentDomain.FriendlyName}");            
            }
            
            _Complete.Set();
 
            return 0;
            
        }
        
        private static void ConfigureServicesMain(IServiceCollection services)
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
                .AddSingleton<IConfig>(config);

            // Pass along to the service's setup
            ConfigureServices(services);
        }

    }
}


