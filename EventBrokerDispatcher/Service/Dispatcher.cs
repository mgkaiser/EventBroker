using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace EventBrokerDispatcher.Service
{
    public class Dispatcher : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IBusControl _busControl;

        public Dispatcher(IBusControl busControl, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Dispatcher>();
            _busControl = busControl;
        }

        public void Dispose()
        {
            // Do nothing
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {          
            _logger.LogInformation("Starting Dispatcher Service");
            return _busControl.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Dispatcher Service");
            return _busControl.StopAsync(cancellationToken);
        }

    }
}