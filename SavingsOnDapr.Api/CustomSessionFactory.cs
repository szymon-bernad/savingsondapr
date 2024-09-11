using Marten;

namespace SavingsOnDapr.Api;

public class CustomSessionFactory : ISessionFactory
{
    private readonly IDocumentStore _store;

    // This is important! You will need to use the
    // IDocumentStore to open sessions
    public CustomSessionFactory(IDocumentStore store)
    {
        _store = store;
    }

    public IQuerySession QuerySession()
    {
        return _store.QuerySession();
    }

    public IDocumentSession OpenSession()
    {
        // Opting for the "lightweight" session
        // option with no identity map tracking
        // and choosing to use Serializable transactions
        // just to be different
        return _store.LightweightSession(System.Data.IsolationLevel.RepeatableRead);
    }
}
