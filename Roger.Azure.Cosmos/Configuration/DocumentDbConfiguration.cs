using System;

namespace Roger.Azure.Cosmos.Configuration
{
    public class DocumentDbConfiguration
    {
        public string EndpointUrl { get; set; }
        public string PrimaryKey { get; set; }
        public string DatabaseName { get; set; }

        public Uri EndpointUri => new Uri(EndpointUrl);
    }
}
