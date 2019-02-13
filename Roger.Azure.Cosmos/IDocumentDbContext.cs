using System;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Roger.Azure.Cosmos
{
    public interface IDocumentDbContext
    {
        DocumentClient Client { get; }
        string DatabaseName { get; }
        Uri DatabaseUri { get; }
        Database Db { get; }
    }
}
