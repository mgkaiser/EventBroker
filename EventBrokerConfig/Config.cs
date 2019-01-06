using System;
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

        public bool IsDevelopment{
            get
            {
                return (_configuration.GetSection("appSettings:IsDevelopment")?.Value ?? "true").ToUpper() == "TRUE";
            }
        }

        public string LoggingRoot 
        {
            get
            {
                return _configuration.GetSection("appSettings:LoggingRoot")?.Value;
            }
        }

        public IConfigurationSection Logging 
        {
            get
            {
                return _configuration.GetSection("Logging");
            }
        }

        public string RabbitMQServer
        {
            get
            {
                return _configuration.GetSection("appSettings:RabbitMQServer")?.Value;
            }
        }

        public string RabbitMQVirtualHost
        {
            get
            {
                return _configuration.GetSection("appSettings:RabbitMQVirtualHost")?.Value ?? "/";
            }
        }

        public string RabbitMQUsername
        {
            get
            {
                return _configuration.GetSection("appSettings:RabbitMQUsername")?.Value;
            }
        }

        public string RabbitMQPassword
        {
            get
            {
                return _configuration.GetSection("appSettings:RabbitMQPassword")?.Value;
            }
        }

    }
}
