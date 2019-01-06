using Microsoft.Extensions.Logging;
using EventBrokerConfig;
using System.Threading.Tasks;
using System.Threading;

namespace EventBrokerDispatcher.Service
{
    public class Dispatcher : IDispatcher
    {
        private readonly ILogger _logger;
        private readonly IConfig _config;

        public Dispatcher(ILoggerFactory loggerFactory, IConfig config)
        {
            _logger = loggerFactory.CreateLogger<Dispatcher>();
            _config = config;
        }

        public async Task Start()
        {          
            _logger.LogInformation("Starting Dispatcher Service");
            await TakeANap();
            _logger.LogInformation("Ending Dispatcher Service");
        }

        private async Task TakeANap()
        {
            _logger.LogInformation("BeginSlumber");                                                                
            await Task.Delay(20000);
            _logger.LogInformation("EndSlumber");
        }
    }
}