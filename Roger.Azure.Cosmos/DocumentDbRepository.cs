using System;
using System.Linq;
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
        private readonly Uri _collectionUri;

        protected readonly IDocumentDbContext Context;
        protected readonly ILogger Logger;
        protected readonly DocumentCollection DocumentCollection;

        public DocumentDbRepository(IDocumentDbContext context, ILogger logger)
        {
            Context = context;
            Logger = logger;
            var attr = GetAttribute();
            _collectionName = attr?.Name;
            Logger.LogInformation($"Creating collection {_collectionName} if not exists under database {context.DatabaseUri}");
            var resDc = Context.Client.CreateDocumentCollectionIfNotExistsAsync(context.DatabaseUri,
                new DocumentCollection() { Id = _collectionName, DefaultTimeToLive = attr?.DefaultTimeToLive }).Result;
            _collectionUri = UriFactory.CreateDocumentCollectionUri(Context.DatabaseName, _collectionName);
            DocumentCollection = resDc.Resource;
        }

        public async Task<T> CreateAsync(T model)
        {
            var response = await Context.Client.CreateDocumentAsync(_collectionUri, model);
            return response.Resource.ToString().Deserialize<T>();
        }

        public async Task<T> CreateOrUpdateAsync(T model)
        {
            var response = await Context.Client.UpsertDocumentAsync(_collectionUri, model);
            return (response.Resource.ToString().Deserialize<T>());
        }

        public async Task<T> UpdateAsync(string id, T model)
        {
            var response = await Context.Client.ReplaceDocumentAsync(GetDocumentUri(id), model);
            return (response.Resource.ToString().Deserialize<T>());
        }

        public async Task<T> GetByIdAsync(string id, bool throwException = true)
        {
            try
            {
                var response = await Context.Client.ReadDocumentAsync(GetDocumentUri(id));
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
            using (var queryable = Context.Client.CreateDocumentQuery<T>(DocumentCollection.SelfLink, sqlQuery,
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
                    result.Data.AddRange(data.ToList());
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
            return Context.Client.DeleteDocumentAsync(GetDocumentUri(id));
        }

        private Uri GetDocumentUri(string id)
        {
            return UriFactory.CreateDocumentUri(Context.DatabaseName, _collectionName, id);
        }


        private CollectionNameAttribute GetAttribute()
        {
            return (CollectionNameAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(CollectionNameAttribute));
        }

        public async Task<IPagedResult<T>> GetAllAsync(string sqlQuery, int pageNumber, int pageSize)
        {
            var pageIndex = pageNumber - 1;
            pageIndex = pageIndex < 0 ? 0 : pageIndex;
            sqlQuery = $"{sqlQuery} OFFSET {pageIndex * pageSize} LIMIT {pageSize}";

            var dataTask = GetAsync(sqlQuery, pageSize);
            var count = 0;
            if (pageNumber == 1)
            {
                var cTask = GetCountAsync(sqlQuery);
                await Task.WhenAll(dataTask, cTask);
                count = cTask.Result;
            }

            var result = dataTask.Result;
            return new PagedResult<T>()
            {
                Data = result.Data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasNextPage = !string.IsNullOrWhiteSpace(result.Token),
                TotalCount = count
            };
        }

        protected async Task<int> GetCountAsync(string sqlQuery)
        {
            var indx = sqlQuery.IndexOf("from", StringComparison.OrdinalIgnoreCase);
            var query = "SELECT VALUE Count(1) " + sqlQuery.Substring(indx);
            using (var queryable = Context.Client.CreateDocumentQuery<T>(DocumentCollection.SelfLink, query,
                    new FeedOptions()
                    {
                        MaxItemCount = 1,
                        RequestContinuation = null
                    })
                .AsDocumentQuery())
            {
                if (queryable.HasMoreResults)
                {
                    var data = await queryable.ExecuteNextAsync<int>();
                    return data.First();
                }
            }

            return 0;
        }
    }
}