using System.Threading.Tasks;
using System.Threading;

namespace EventBrokerDispatcher.Service
{
    public interface IDispatcher
    {
         Task Start(CancellationToken cancelationToken);
    }
}