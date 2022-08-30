using OrleansContrib.Persistence.RavenDb.Options;
using OrleansContrib.RavenDb.Tester;
using Raven.Client.Documents;

namespace OrleansContrib.Persistence.RavenDb.Tests;

public class PersistenceConfigOptions
{
    public IDocumentStore DocumentStore { get; }
    
    public PersistenceConfigOptions()
    {
        DocumentStore = StoreHolder.CreateDocumentStore();
    }
    
    public void ConfigureDefaultStoreOptions(GrainStorageOptions options) { }
}