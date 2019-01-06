using System.Threading;
using System.Threading.Tasks;
using EventBrokerDispatcher.Service;
using Microsoft.Extensions.DependencyInjection;

namespace EventBrokerDispatcher
{
    partial class Program
    {
        private static async Task RunServices(CancellationToken token)
        {
            var dispatcher = _serviceProvider.Value.GetService<IDispatcher>();
            await dispatcher.Start(_cts.Token);
        }   

        private static void ConfigureServices(IServiceCollection services)
        {
            services                
                .AddSingleton<IDispatcher, Dispatcher>();                
        }
    }
}