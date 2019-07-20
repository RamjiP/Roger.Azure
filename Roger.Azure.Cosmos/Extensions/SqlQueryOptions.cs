using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Roger.Common.Persistence;

namespace Roger.Azure.Cosmos.Extensions
{
    public static class SqlQueryOptionsExtensions
    {
        public static FeedOptions FeedOptions(this SqlQueryOptions sqlQueryOptions)
        {
            var options = new FeedOptions()
            {
                MaxItemCount = sqlQueryOptions.PageSize,
                RequestContinuation = sqlQueryOptions.ContinuationToken
            };

            if (!string.IsNullOrWhiteSpace((sqlQueryOptions.PartitionKey)))
            {
                options.PartitionKey = new PartitionKey(sqlQueryOptions.PartitionKey);
            }

            return options;
        }

        public static string GetSqlWithPageOffset(this SqlQueryOptions sqlQueryOptions, string sql) =>
            $"{sql} OFFSET {sqlQueryOptions.PageIndex * sqlQueryOptions.PageSize} LIMIT {sqlQueryOptions.PageSize}";
    }
}
