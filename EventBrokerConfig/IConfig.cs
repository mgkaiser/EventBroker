using System;
using Microsoft.Extensions.Configuration;

namespace EventBrokerConfig
{
    public interface IConfig
    {          
        EventBrokerQueues eventBrokerQueues { get; }
    }
}