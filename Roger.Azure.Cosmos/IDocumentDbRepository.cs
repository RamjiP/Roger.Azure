using Roger.Common.Persistence;

namespace Roger.Azure.Cosmos
{
    public interface IDocumentDbRepository<T> : ITokenRepository<T>
        where T: new()
    {
    }
}