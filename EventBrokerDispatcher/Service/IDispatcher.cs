using System.Threading.Tasks;

namespace EventBrokerDispatcher.Service
{
    public interface IDispatcher
    {
         Task Start();
    }
}