using System.Threading.Tasks;
using EventBrokerInterfaces;
using MassTransit;

namespace EventBrokerDispatcher.Consumers
{
    public class EGTEventConsumer : IConsumer<IEGTEvent>
    {
        public Task Consume(ConsumeContext<IEGTEvent> context)
        {
            throw new System.NotImplementedException();
        }
    }
}