using Microsoft.Extensions.Logging;

namespace EventBrokerDispatcher.Service
{
    public class Dispatcher : IDispatcher
    {
        private readonly ILogger _logger;

        public Dispatcher(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Dispatcher>();
        }

        public void Start()
        {          
            _logger.LogInformation("Starting EventBrokerDispatcher");
            _logger.LogInformation("Ending EventBrokerDispatcher");
        }
    }
}