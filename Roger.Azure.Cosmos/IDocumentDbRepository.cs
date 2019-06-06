using Roger.Common.Persistence;

namespace Roger.Azure.Cosmos
{
    public interface IDocumentDbRepository<T> : ITokenRepository<T>, IPagedRepository<T>
        where T: new()
    {
    }
}