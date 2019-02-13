using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Roger.Azure.Cosmos.Attributes;
using Roger.Common.Persistence;
using Roger.Json.Extensions;

namespace Roger.Azure.Cosmos
{
    public abstract class DocumentDbRepository<T> : IDocumentDbRepository<T>
        where T : new()
    {
        private readonly string _collectionName;
        private readonly Uri CollectionUri;

        private readonly IDocumentDbContext _context;
        protected readonly ILogger Logger;
        protected readonly DocumentCollection DocumentCollection;

        public DocumentDbRepository(IDocumentDbContext context, ILogger logger)
        {
            _context = context;
            Logger = logger;
            var attr = GetAttribute();
            _collectionName = attr?.Name;
            Logger.LogInformation($"Creating collection {_collectionName} if not exists under database {context.DatabaseUri}");
            var resDc = _context.Client.CreateDocumentCollectionIfNotExistsAsync(context.DatabaseUri,
                new DocumentCollection() { Id = _collectionName, DefaultTimeToLive = attr?.DefaultTimeToLive }).Result;
            CollectionUri = UriFactory.CreateDocumentCollectionUri(_context.DatabaseName, _collectionName);
            DocumentCollection = resDc.Resource;
        }

        public async Task<T> CreateAsync(T model)
        {
            var response = await _context.Client.CreateDocumentAsync(CollectionUri, model);
            return response.Resource.ToString().Deserialize<T>();
        }

        public async Task<T> CreateOrUpdateAsync(T model)
        {
            var response = await _context.Client.UpsertDocumentAsync(CollectionUri, model);
            return (response.Resource.ToString().Deserialize<T>());
        }


        public async Task<T> UpdateAsync(string id, T model)
        {
            var response = await _context.Client.ReplaceDocumentAsync(GetDocumentUri(id), model);
            return (response.Resource.ToString().Deserialize<T>());
        }

        public async Task<T> GetByIdAsync(string id, bool throwException = true)
        {
            try
            {
                var response = await _context.Client.ReadDocumentAsync(GetDocumentUri(id));
                return (response.Resource.ToString().Deserialize<T>());
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound && !throwException)
                {
                    return default(T);
                }

                throw;
            }
        }

        protected async Task<ITokenPagedResult<T>> GetAsync(string sqlQuery, int maxItemCount = 10, string continuationToken = null)
        {
            var result = new TokenPagedResult<T>();
            using (var queryable = _context.Client.CreateDocumentQuery<T>(DocumentCollection.SelfLink, sqlQuery,
                    new FeedOptions()
                    {
                        MaxItemCount = maxItemCount,
                        RequestContinuation = continuationToken
                    })
                .AsDocumentQuery())
            {
                while (queryable.HasMoreResults)
                {
                    var data = await queryable.ExecuteNextAsync<T>();
                    result.Token = data.ResponseContinuation;
                    result.Data = data.ToList();
                }
            }

            return result;
        }


        public Task<ITokenPagedResult<T>> GetAllAsync(int maxItemCount = 10, string continuationToken = null)
        {
            return GetAsync($"SELECT TOP {maxItemCount} * from c", maxItemCount, continuationToken);
        }

        public Task DeleteAsync(string id)
        {
            return _context.Client.DeleteDocumentAsync(GetDocumentUri(id));
        }

        private Uri GetDocumentUri(string id)
        {
            return UriFactory.CreateDocumentUri(_context.DatabaseName, _collectionName, id);
        }


        private CollectionNameAttribute GetAttribute()
        {
            return (CollectionNameAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(CollectionNameAttribute));
        }
    }
}