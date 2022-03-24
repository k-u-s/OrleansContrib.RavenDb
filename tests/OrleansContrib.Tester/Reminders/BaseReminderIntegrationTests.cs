using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost.Utils;
using OrleansContrib.Tester.Reminders.Runners;
using Xunit;

namespace OrleansContrib.Tester.Reminders;


public abstract class BaseReminderIntegrationTests : OrleansTestingBase, IDisposable
{
    protected ILogger Log;
    private readonly BaseReminderIntegrationTestsRunner runner;

    protected BaseReminderIntegrationTests(BaseReminderTestClusterFixture fixture)
    {
        fixture.EnsurePreconditionsMet();

        var filters = new LoggerFilterOptions();
#if DEBUG
        filters.AddFilter("Storage", LogLevel.Trace);
        filters.AddFilter("Reminder", LogLevel.Trace);
#endif

        Log = TestingUtils.CreateDefaultLoggerFactory(
                TestingUtils.CreateTraceFileName("client", DateTime.Now.ToString("yyyyMMdd_hhmmss")), filters)
            .CreateLogger<BaseReminderIntegrationTests>();

        runner = new BaseReminderIntegrationTestsRunner(fixture, Log);
    }

    public void Dispose()
    {
        runner.Dispose();
    }

    // Basic tests

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Reminders_Basic_StopByRef()
    {
        await runner.Reminders_Basic_StopByRef();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Reminders_Basic_ListOps()
    {
        await runner.Reminders_Basic_ListOps();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Reminders_1J_MultiGrainMultiReminders()
    {
        await runner.Reminders_1J_MultiGrainMultiReminders();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Reminders_ReminderNotFound()
    {
        await runner.Reminders_ReminderNotFound();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_Basic()
    {
        await runner.Rem_Basic();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_Basic_Restart()
    {
        await runner.Rem_Basic_Restart();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_MultipleReminders()
    {
        await runner.Rem_MultipleReminders();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_2J_MultiGrainMultiReminders()
    {
        await runner.Rem_2J_MultiGrainMultiReminders();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_MultiGrainMultiReminders()
    {
        await runner.Rem_MultiGrainMultiReminders();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_1F_Basic()
    {
        await runner.Rem_1F_Basic();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_2F_MultiGrain()
    {
        await runner.Rem_2F_MultiGrain();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_1F1J_MultiGrain()
    {
        await runner.Rem_1F1J_MultiGrain();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_RegisterSameReminderTwice()
    {
        await runner.Rem_RegisterSameReminderTwice();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_GT_Basic()
    {
        await runner.Rem_GT_Basic();
    }

    [SkippableFact(Skip = "https://github.com/dotnet/orleans/issues/4319"), TestCategory("Functional")]
    public async Task Test_Rem_GT_1F1J_MultiGrain()
    {
        await runner.Rem_GT_1F1J_MultiGrain();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_Wrong_LowerThanAllowedPeriod()
    {
        await runner.Rem_Wrong_LowerThanAllowedPeriod();
    }

    [SkippableFact, TestCategory("Functional")]
    public async Task Test_Rem_Wrong_Grain()
    {
        await runner.Rem_Wrong_Grain();
    }
}