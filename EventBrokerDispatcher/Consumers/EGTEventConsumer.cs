using System.Threading.Tasks;
using EventBrokerInterfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EventBrokerDispatcher.Consumers
{
    public class EGTEventConsumer : IConsumer<IEGTEvent>
    {
        private readonly ILogger _logger;
        private readonly IBusControl _busControl;  

        public string Url { get; set; }          
        
        public EGTEventConsumer(IBusControl busControl, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EGTEventConsumer>();
            _busControl = busControl;
        }

        public Task Consume(ConsumeContext<IEGTEvent> context)
        {
            _logger.LogInformation($"Received event: {context.Message.SenderId} : {context.Message.EventType}");
            return Task.CompletedTask;
        }
    }
}