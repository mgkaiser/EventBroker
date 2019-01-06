using System;
using Microsoft.Extensions.Configuration;

namespace EventBrokerConfig
{
    public interface IConfig
    {
        bool IsDevelopment { get; }
        string LoggingRoot { get; }
        IConfigurationSection Logging{ get;}
    }
}