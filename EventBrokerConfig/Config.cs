using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace EventBrokerConfig
{
    public class Config : IConfig
    {
        private readonly IConfigurationRoot _configuration;

        public Config()
        {
             // Setup config
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();                         
        }        

        public EventBrokerQueues eventBrokerQueues
        {
            get
            {
                EventBrokerQueues x = new EventBrokerQueues();
                _configuration.GetSection("eventBrokerQueues").Bind(x);
                return x;
            }
        } 
    }
}
