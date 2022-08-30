using System;
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
using OrleansContrib.RavenDb.Tester;
using OrleansContrib.Tester;
using OrleansContrib.Tester.Persistence;
using Raven.Client.Documents;
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
        private PersistenceConfigOptions _databaseOptions;

        public SiloConfigurator()
        {
            _databaseOptions = new();
        }
        
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .ConfigureServices(services => services.AddSingleton(StoreHolder.CreateDocumentStore))
                .AddRavenGrainStorage(
                    TestConstants.StorageProviderForTest,
                    _databaseOptions.ConfigureDefaultStoreOptions)
                ;
        }
    }

    private PersistenceConfigOptions _databaseOptions;

    public RavenGrainStorageUnitTests(Fixture clusterFixture)
        : base(clusterFixture)
    {
        _databaseOptions = new();
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
        _databaseOptions.ConfigureDefaultStoreOptions(ravenOptions);

        var sp = default(IServiceProvider);
        var db = StoreHolder.CreateDocumentStore(sp);
        ravenOptions.DocumentStoreProvider = _ => db;
        
        var logger = LoggerFactory.CreateLogger<GrainStorage>();
        
        var storage = new GrainStorage(
            TestConstants.StorageProviderForTest,
            ravenOptions,
            sp,
            ClusterOptions,
            logger);
        return Task.FromResult((IGrainStorage)storage);
    }
}