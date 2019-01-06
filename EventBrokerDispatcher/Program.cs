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

namespace EventBrokerDispatcher
{
    class Program
    {
        private static ManualResetEvent _Shutdown = new ManualResetEvent(false);
        private static ManualResetEventSlim _Complete = new ManualResetEventSlim();

        static int Main(string[] args)
        {
            try
            {
                var ended = new ManualResetEventSlim();
                var starting = new ManualResetEventSlim();

                AssemblyLoadContext.Default.Unloading += (obj) =>{
                    _Shutdown.Set();
                    _Complete.Wait();        
                };

                // Setup our ServiceCollection
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                // Setup our ServiceProvider
                var serviceProvider = serviceCollection.BuildServiceProvider();            
                                
                // Do the actual work here
                var dispatcher = serviceProvider.GetService<IDispatcher>();
                dispatcher.Start();
                
                // Wait for a singnal
                _Shutdown.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // Anything that MUST happen goes here
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


