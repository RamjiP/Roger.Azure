using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Roger.Common.Models;
using Roger.Common.Persistence;

namespace Roger.Azure.Cosmos
{
    public interface IDocumentDbRepository<T> : ITokenRepository<T>
        where T: new()
    {
    }
}