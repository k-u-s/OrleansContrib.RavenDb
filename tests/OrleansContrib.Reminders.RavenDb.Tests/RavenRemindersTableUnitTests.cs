using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.TestingHost;
using Orleans.TestingHost.Utils;
using OrleansContrib.RavenDb.Tester;
using OrleansContrib.Reminders.RavenDb.Extensions;
using OrleansContrib.Reminders.RavenDb.Options;
using OrleansContrib.Reminders.RavenDb.Reminders;
using OrleansContrib.Tester;
using OrleansContrib.Tester.Reminders;
using Raven.Client.Documents;
using Xunit;

namespace OrleansContrib.Reminders.RavenDb.Tests;

/// <summary>
/// Tests for operation of Orleans Reminders Table using RavenDb
/// </summary>
[TestCategory(TestConstants.Category.Reminders), TestCategory(StoreHolder.DatabaseCategory)]
public class RavenRemindersTableUnitTests : BaseReminderTableUnitTests, IClassFixture<RavenRemindersTableUnitTests.Fixture>
{
    public class Fixture : BaseReminderTestClusterFixture
    {
        protected override void ConfigureReminderTestCluster(TestClusterBuilder builder)
        {
            builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        }
    }

    public class SiloConfigurator : ISiloConfigurator
    {
        private ReminderConfigOptions _databaseOptions;

        public SiloConfigurator()
        {
            _databaseOptions = new();
        }

        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .ConfigureServices(services => services.AddSingleton(StoreHolder.CreateDocumentStore))
                .UseRavenReminderService(_databaseOptions.ConfigureDefaultStoreOptionsBuilder)
                ;
        }
    }

    private ReminderConfigOptions _databaseOptions = new();

    public RavenRemindersTableUnitTests(Fixture environment) : base(environment) { }

    protected override ILoggerFactory CreateLoggerFactory()
        => TestingUtils.CreateDefaultLoggerFactory($"{GetType()}.log", CreateFilters());

    private static LoggerFilterOptions CreateFilters()
    {
        var filters = new LoggerFilterOptions();
        filters.AddFilter("OrleansContrib", LogLevel.Trace);
        filters.AddFilter("RavenReminderTable", LogLevel.Trace);
        filters.AddFilter("OrleansSiloInstanceManager", LogLevel.Trace);
        filters.AddFilter("Storage", LogLevel.Trace);
        return filters;
    }

    protected override bool DeleteEntriesAfterTest => true;

    protected override IReminderTable CreateRemindersTable()
    {
        var ravenOptions = new ReminderTableOptions();
        _databaseOptions.ConfigureDefaultStoreOptions(ravenOptions);
        
        var options = Microsoft.Extensions.Options.Options.Create(ravenOptions);
        var converter = ClusterFixture.HostedCluster.ServiceProvider.GetRequiredService<IGrainReferenceConverter>();
        //var documentStore = ClusterFixture.HostedCluster.ServiceProvider.GetRequiredService<IDocumentStore>();
        var sp = default(IServiceProvider);
        var db = StoreHolder.CreateDocumentStore(sp);
        ravenOptions.DocumentStoreProvider = _ => db;

        return new ReminderTable(
            converter,
            sp,
            LoggerFactory,
            ClusterOptions,
            options);
    }
}