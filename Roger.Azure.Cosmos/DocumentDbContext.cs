using System;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Roger.Azure.Cosmos.Configuration;
using Roger.Json;

namespace Roger.Azure.Cosmos
{
    public class DocumentDbContext : IDocumentDbContext
    {
        private readonly ILogger _logger;
        private readonly DocumentDbConfiguration _configuration;
        public DocumentClient Client { get; }
        public Database Db { get; }
        public string DatabaseName { get; }
        public Uri DatabaseUri { get; }
        public DocumentDbContext(IOptions<DocumentDbConfiguration> options, ILogger<DocumentDbContext> logger)
        {
            _logger = logger;
            _configuration = options.Value;
            _logger.LogInformation($"Creating database {_configuration.DatabaseName} if not exists in {_configuration.EndpointUrl}");
            Client = new DocumentClient(_configuration.EndpointUri, _configuration.PrimaryKey, SerializerSettings.Default);
            var resDb = Client.CreateDatabaseIfNotExistsAsync(new Database() { Id = _configuration.DatabaseName }, new RequestOptions() {JsonSerializerSettings = SerializerSettings.Default, OfferThroughput = 400 }).Result;
            Db = resDb.Resource;
            DatabaseName = _configuration.DatabaseName;
            DatabaseUri = UriFactory.CreateDatabaseUri(_configuration.DatabaseName);
        }
    }
}