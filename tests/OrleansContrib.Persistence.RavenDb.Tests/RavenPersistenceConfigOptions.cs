using OrleansContrib.Persistence.RavenDb.Options;
using OrleansContrib.RavenDb.Tester;

namespace OrleansContrib.Persistence.RavenDb.Tests;

public class PersistenceConfigOptions
{
    private string _keyPrefix;
    
    public PersistenceConfigOptions()
    {
        _keyPrefix = StoreHolder.CreateNextKeyPrefix();
    }
    
    public void ConfigureDefaultStoreOptions(GrainStorageOptions options)
    {
        options.KeyPrefix = _keyPrefix;
    }
}