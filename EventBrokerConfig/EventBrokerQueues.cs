using System.Collections.Generic;

namespace EventBrokerConfig
{
    public class EventBrokerQueues
    {
        public List<Queue> queues { get; set;}
    }

    public class Queue
    {
        public string queueName { get; set; }
        public string url { get; set; }
        public List<Bindings> bindings { get; set; }
    }

    public class Bindings{
        public string senderId { get; set; }
        public string eventType { get; set; }
    }
}