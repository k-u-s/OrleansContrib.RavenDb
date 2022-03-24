using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.Storage;
using Orleans.TestingHost;
using Orleans.TestingHost.Utils;
using OrleansContrib.Persistence.RavenDb.Extensions;
using OrleansContrib.Persistence.RavenDb.Options;
using OrleansContrib.Persistence.RavenDb.StorageProviders;
using OrleansContrib.Reminders.RavenDb.Tests;
using OrleansContrib.Tester;
using OrleansContrib.Tester.Persistence;
using Xunit;

namespace OrleansContrib.Persistence.RavenDb.Tests;

/// <summary>
/// Tests for operation of Orleans grain storage using RavenDb
/// </summary>
[TestCategory(TestConstants.Category.Persistence), TestCategory(StoreHolder.DatabaseCategory)]
public class RavenGrainStorageUnitTests : BaseGrainStorageUnitTests, IClassFixture<RavenGrainStorageUnitTests.Fixture>
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

    public RavenGrainStorageUnitTests(Fixture clusterFixture)
        : base(clusterFixture)
    {
    }

    protected override ILoggerFactory CreateLoggerFactory()
        => TestingUtils.CreateDefaultLoggerFactory($"{GetType()}.log", CreateFilters());

    private static LoggerFilterOptions CreateFilters()
    {
        var filters = new LoggerFilterOptions();
        filters.AddFilter("OrleansContrib", LogLevel.Trace);
        filters.AddFilter("RavenGrainStorage", LogLevel.Trace);
        filters.AddFilter("OrleansSiloInstanceManager", LogLevel.Trace);
        filters.AddFilter("Storage", LogLevel.Trace);
        return filters;
    }

    protected override Task<IGrainStorage> CreateGrainStorage()
    {
        var ravenOptions = new GrainStorageOptions();
        PersistenceConfigOptions.ConfigureDefaultStoreOptions(ravenOptions);
        
        var documentStore = StoreHolder.DocumentStore;
        var logger = LoggerFactory.CreateLogger<GrainStorage>();
        
        var storage = new GrainStorage(
            TestConstants.StorageProviderForTest,
            ravenOptions,
            ClusterOptions,
            documentStore,
            logger);
        return Task.FromResult((IGrainStorage)storage);
    }
}