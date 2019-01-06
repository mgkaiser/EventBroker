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

        public async Task Start(CancellationToken cancelationToken)
        {          
            _logger.LogInformation("Starting Dispatcher Service");
            await TakeANap(cancelationToken);
            _logger.LogInformation("Ending Dispatcher Service");
        }

        private async Task TakeANap(CancellationToken cancelationToken)
        {
            _logger.LogInformation("BeginSlumber");                                                                
            await Task.Delay(20000,cancelationToken);
            _logger.LogInformation("EndSlumber");
        }
    }
}