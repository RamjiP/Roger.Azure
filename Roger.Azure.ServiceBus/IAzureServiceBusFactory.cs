using Microsoft.Azure.ServiceBus;

namespace Roger.Azure.ServiceBus
{
    public interface IAzureServiceBusFactory
    {
        ITopicClient CreateTopicClient(string topicName);
        ISubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName);
    }
}