using System;
using Microsoft.Extensions.Configuration;

namespace EventBrokerConfig
{
    public interface IConfig
    {
        string LoggingRoot { get; }
        IConfigurationSection Logging { get; }
        string RabbitMQServer { get; }
        string RabbitMQVirtualHost { get; }    
        string RabbitMQUsername { get; }    
        string RabbitMQPassword { get; }    
        EventBrokerQueues eventBrokerQueues { get; }
    }
}