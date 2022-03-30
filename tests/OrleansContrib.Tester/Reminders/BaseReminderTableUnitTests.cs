using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Internal;
using Orleans.Runtime;
using Orleans.TestingHost.Utils;
using OrleansContrib.Tester.Reminders.Runners;
using Xunit;

namespace OrleansContrib.Tester.Reminders;


[Collection(TestConstants.DefaultCollection)]
public abstract class BaseReminderTableUnitTests : IAsyncLifetime
{
    protected IReminderTable remindersTable;
    protected ILoggerFactory LoggerFactory;
    protected BaseReminderTestClusterFixture ClusterFixture;
    protected IOptions<ClusterOptions> ClusterOptions;
    private readonly BaseReminderTableTestsRunner runner;

    protected abstract bool DeleteEntriesAfterTest { get; }
    
    protected BaseReminderTableUnitTests(
        BaseReminderTestClusterFixture clusterFixture)
    {
        ClusterFixture = clusterFixture;
        LoggerFactory = CreateLoggerFactory();
        var logger = LoggerFactory.CreateLogger<BaseReminderTableUnitTests>();
        var serviceId = $"{Guid.NewGuid()}/foo";
        var clusterId = $"test-{serviceId}/foo2";

        logger.Info("ClusterId={0}", clusterId);
        ClusterOptions = Options.Create(new ClusterOptions { ClusterId = clusterId, ServiceId = serviceId });

        remindersTable = CreateRemindersTable();
        runner = new BaseReminderTableTestsRunner(remindersTable, clusterFixture);
    }

    protected virtual ILoggerFactory CreateLoggerFactory() 
        => TestingUtils.CreateDefaultLoggerFactory($"{GetType()}.log");
    
    public virtual async Task InitializeAsync()
    {
        await remindersTable.Init().WithTimeout(TimeSpan.FromMinutes(1));
    }

    public virtual async Task DisposeAsync()
    {
        if (DeleteEntriesAfterTest)
        {
            await remindersTable.TestOnlyClearTable();
        }
    }

    protected abstract IReminderTable CreateRemindersTable();


    [SkippableFact]
    public void RemindersTable_Init()
    {
        // Empty to just verify that initialization done by framework passes
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task RemindersTable_RemindersRange()
    {
        await runner.RemindersRange(100);
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task RemindersTable_RemindersParallelUpsert()
    {
        await runner.RemindersParallelUpsert();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task RemindersTable_ReminderSimple()
    {
        await runner.ReminderSimple();
    }
}
