using System;

namespace EventBrokerInterfaces
{
    public interface IEGTEvent
    {
        string EventType { get; set; }
        string SenderId { get; set; }
        string Message { get; set; }
    }
}
