using Microsoft.Extensions.Logging;
using EventBrokerConfig;

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

        public void Start()
        {          
            _logger.LogInformation("Starting EventBrokerDispatcher");
            _logger.LogInformation("Ending EventBrokerDispatcher");
        }
    }
}