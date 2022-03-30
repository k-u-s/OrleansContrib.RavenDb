using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using OrleansContrib.Persistence.RavenDb.Extensions;
using OrleansContrib.RavenDb.Tester;
using OrleansContrib.Tester;
using OrleansContrib.Tester.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace OrleansContrib.Persistence.RavenDb.Tests;

[TestCategory(TestConstants.Category.Persistence), TestCategory(StoreHolder.DatabaseCategory)]
public class RavenPersistenceIntegrationTests : BasePersistenceGrainIntegrationTests,
    IClassFixture<RavenPersistenceIntegrationTests.Fixture>
{
    public class Fixture : BasePersistenceTestClusterFixture
    {
        protected override void ConfigurePersistenceTestCluster(TestClusterBuilder builder)
        {
            builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        }
    }

    public class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .ConfigureServices(services => services.AddSingleton(StoreHolder.DocumentStore))
                .AddRavenGrainStorage(
                    TestConstants.StorageProviderForTest,
                    PersistenceConfigOptions.ConfigureDefaultStoreOptions)
                ;
        }
    }

    public RavenPersistenceIntegrationTests(ITestOutputHelper output, Fixture fixture) 
        : base(output, fixture) { }
}