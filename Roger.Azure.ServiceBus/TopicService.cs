using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Roger.Azure.ServiceBus.Configuration;
using Roger.Common.Constants;
using Roger.Common.Extensions;
using Roger.Common.Messaging;
using Roger.Json.Extensions;

namespace Roger.Azure.ServiceBus
{
    public class TopicService<T> : ITopicService<T>
        where T: new()
    {
        private readonly AsbConfiguration _configuration;
        private readonly ITopicClient _client;

        public TopicService(AsbConfiguration configuration, string topicName)
        {
            _configuration = configuration;
            _client = new TopicClient(_configuration.ConnectionString, topicName); 
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
            return _client.SendAsync(message);
        }
    }
}
