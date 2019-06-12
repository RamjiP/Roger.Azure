using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Roger.Azure.ServiceBus.Configuration;

namespace Roger.Azure.ServiceBus
{
    public class AzureServiceBusFactory : IAzureServiceBusFactory
    {
        private readonly AsbConfiguration _configuration;

        public AzureServiceBusFactory(IOptions<AsbConfiguration> configuration)
        {
            _configuration = configuration.Value;
        }

        public ITopicClient CreateTopicClient(string topicName)
        {
            return new TopicClient(_configuration.ConnectionString, topicName);
        }

        public ISubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
        {
            return new SubscriptionClient(_configuration.ConnectionString, topicName, subscriptionName);
        }
    }
}
