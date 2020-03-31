using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Roger.Azure.Cosmos.Attributes;
using Roger.Azure.Cosmos.Extensions;
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
            var collection = new DocumentCollection()
            {
                Id = _collectionName,
                DefaultTimeToLive = attr?.DefaultTimeToLive,

            };

            if (!string.IsNullOrWhiteSpace(attr?.PartitionKeyPath))
            {
                collection.PartitionKey = new PartitionKeyDefinition()
                {
                    Paths = new Collection<string>() { attr.PartitionKeyPath },
                    Version = PartitionKeyDefinitionVersion.V2
                };
            }

            var resDc = Context.Client.CreateDocumentCollectionIfNotExistsAsync(context.DatabaseUri, collection).Result;
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
                var result = await GetPagedResultAsync($"SELECT * from c where c.id = '{id}'", new SqlQueryOptions()
                {
                    PageNumber = 1,
                    PageSize = 1
                });
                if (result.Data?.Count > 0)
                {
                    return result.Data.First();
                }

                if (throwException)
                {
                    throw new Exception($"Docment {id} not found");
                }

                return default(T);
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


        public Task<ITokenPagedResult<T>> GetTokenisedResultAsync(SqlQueryOptions sqlQueryOptions)
        {
            return GetAsync($"SELECT TOP {sqlQueryOptions.PageSize} * from c", sqlQueryOptions);
        }

        public Task DeleteAsync(string id)
        {
            return Context.Client.DeleteDocumentAsync(GetDocumentUri(id), new RequestOptions() { PartitionKey = new PartitionKey(Undefined.Value) });
        }

        public async Task<IPagedResult<T>> GetPagedResultAsync(string sql, SqlQueryOptions sqlQueryOptions)
        {
            var dataTask = GetAsync(sqlQueryOptions.GetSqlWithPageOffset(sql), sqlQueryOptions);
            var count = 0;
            if (sqlQueryOptions.RequiresTotalCount)
            {
                var cTask = GetCountAsync(sql);
                await Task.WhenAll(dataTask, cTask);
                count = cTask.Result;
            }

            var result = dataTask.Result;
            return new PagedResult<T>()
            {
                Data = result.Data,
                PageNumber = sqlQueryOptions.PageNumber,
                PageSize = sqlQueryOptions.PageSize,
                HasNextPage = !string.IsNullOrWhiteSpace(result.Token),
                TotalCount = count
            };
        }

        protected Task DeleteWithPartitionKeyAsync(string id, object partitionKey)
        {
            return Context.Client.DeleteDocumentAsync(GetDocumentUri(id), new RequestOptions() { PartitionKey = new PartitionKey(partitionKey) });
        }

        protected async Task<int> GetCountAsync(string sqlQuery, string partitionKey = null)
        {
            var indx = sqlQuery.IndexOf("from", StringComparison.OrdinalIgnoreCase);
            var query = "SELECT VALUE Count(1) " + sqlQuery.Substring(indx);
            var options = new FeedOptions()
            {
                MaxItemCount = 1,
                RequestContinuation = null
            };

            if (!string.IsNullOrWhiteSpace(partitionKey))
            {
                options.PartitionKey = new PartitionKey(partitionKey);
            }
            else
            {
                options.EnableCrossPartitionQuery = true;
            }
            using (var queryable = Context.Client.CreateDocumentQuery<T>(DocumentCollection.SelfLink, query, options)
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

        protected async Task<ITokenPagedResult<T>> GetAsync(string sql, SqlQueryOptions sqlQueryOptions)
        {
            var result = new TokenPagedResult<T>();
            using (var queryable = Context.Client.CreateDocumentQuery<T>(DocumentCollection.SelfLink, sql, sqlQueryOptions.FeedOptions())
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

        private Uri GetDocumentUri(string id)
        {
            return UriFactory.CreateDocumentUri(Context.DatabaseName, _collectionName, id);
        }


        private CollectionNameAttribute GetAttribute()
        {
            return (CollectionNameAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(CollectionNameAttribute));
        }

    }
}