using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Roger.Common.Constants;
using Roger.Common.Extensions;
using Roger.Json.Extensions;

namespace Roger.Azure.ServiceBus
{
    public class TopicService<T>
    {
        private readonly ITopicClient Client;

        public TopicService(IAzureServiceBusFactory azureServiceBusFactory, string topicName)
        {
            Client = azureServiceBusFactory.CreateTopicClient(topicName);
        }

        public Task SendAsync(T obj, IDictionary<string, object> properties = null)
        {
            var json = obj.ToJson();
            Message message = new Message(json.GetUtf8Bytes()) {ContentType = ContentTypes.Json};
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    message.UserProperties.Add(property);
                }
            }
            return Client.SendAsync(message);
        }
    }
}
