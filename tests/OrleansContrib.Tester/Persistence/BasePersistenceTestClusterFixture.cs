using Orleans.TestingHost;

namespace OrleansContrib.Tester.Persistence;

public class BasePersistenceTestClusterFixture : BaseTestClusterFixture
{
    protected override void ConfigureTestCluster(TestClusterBuilder builder)
    {
        // Application parts: just reference one of the grain implementations that we use
        builder.AddSiloBuilderConfigurator<PersistenceSiloBuilderConfiguration>();
        builder.AddClientBuilderConfigurator<PersistenceClientBuilderConfiguration>();
        
        ConfigurePersistenceTestCluster(builder);
    }
    

    protected virtual void ConfigurePersistenceTestCluster(TestClusterBuilder builder)
    {
    }
}