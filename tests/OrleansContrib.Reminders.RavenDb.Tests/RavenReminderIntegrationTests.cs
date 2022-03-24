using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using OrleansContrib.Reminders.RavenDb.Extensions;
using OrleansContrib.Tester;
using OrleansContrib.Tester.Reminders;
using Xunit;

namespace OrleansContrib.Reminders.RavenDb.Tests;

[TestCategory(TestConstants.Category.Reminders), TestCategory(StoreHolder.DatabaseCategory)]
public class RavenReminderIntegrationTests : BaseReminderIntegrationTests, IClassFixture<RavenReminderIntegrationTests.Fixture>
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
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .ConfigureServices(services => services.AddSingleton(StoreHolder.DocumentStore))
                .UseRavenReminderService(ReminderConfigOptions.ConfigureDefaultStoreOptionsBuilder)
                ;
        }
    }

    public RavenReminderIntegrationTests(Fixture fixture) : base(fixture)
    {
    }
}