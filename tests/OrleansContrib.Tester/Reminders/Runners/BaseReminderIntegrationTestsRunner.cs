using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Internal;
using Orleans.Runtime;
using Orleans.TestingHost;
using OrleansContrib.Tester.Reminders.Grains;
using Xunit;

namespace OrleansContrib.Tester.Reminders.Runners;

public class BaseReminderIntegrationTestsRunner : IDisposable
{
    protected TestCluster HostedCluster { get; private set; }

    public IGrainFactory GrainFactory { get; }

    public static readonly TimeSpan
        Leeway = TimeSpan
            .FromMilliseconds(100); // the experiment shouldnt be that long that the sums of leeways exceeds a period

    public static readonly TimeSpan Endwait = TimeSpan.FromMinutes(5);

    public const string Dr = "DEFAULT_REMINDER";
    public const string R1 = "REMINDER_1";
    public const string R2 = "REMINDER_2";

    protected const long Retries = 3;

    protected const long
        FailAfter = 2; // NOTE: match this sleep with 'failCheckAfter' used in PerGrainFailureTest() so you dont try to get counter immediately after failure as new activation may not have the reminder statistics

    protected const long FailCheckAfter = 6; // safe value: 9

    protected ILogger Log;

    public BaseReminderIntegrationTestsRunner(BaseReminderTestClusterFixture fixture, ILogger logger)
    {
        HostedCluster = fixture.HostedCluster;
        GrainFactory = fixture.GrainFactory;

        Log = logger;
    }

    public void Dispose()
    {
        // ReminderTable.Clear() cannot be called from a non-Orleans thread,
        // so we must proxy the call through a grain.
        var controlProxy = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        controlProxy.EraseReminderTable().WaitWithThrow(TestConstants.InitTimeout);
    }


    public async Task Reminders_Basic_StopByRef()
    {
        var grain = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());

        var r1 = await grain.StartReminder(Dr);
        var r2 = await grain.StartReminder(Dr);
        try
        {
            // First handle should now be out of date once the seconf handle to the same reminder was obtained
            await grain.StopReminder(r1);
            Assert.True(false, "Removed reminder1, which shouldn't be possible.");
        }
        catch (Exception exc)
        {
            Log.Info("Couldn't remove {0}, as expected. Exception received = {1}", r1, exc);
        }

        await grain.StopReminder(r2);
        Log.Info("Removed reminder2 successfully");

        // trying to see if readreminder works
        _ = await grain.StartReminder(Dr);
        _ = await grain.StartReminder(Dr);
        _ = await grain.StartReminder(Dr);
        _ = await grain.StartReminder(Dr);

        var r = await grain.GetReminderObject(Dr);
        await grain.StopReminder(r);
        Log.Info("Removed got reminder successfully");
    }

    public async Task Reminders_Basic_ListOps()
    {
        var id = Guid.NewGuid();
        Log.Info("Start Grain Id = {0}", id);
        var grain = GrainFactory.GetGrain<IReminderTestGrain>(id);
        const int count = 5;
        var startReminderTasks = new Task<IGrainReminder>[count];
        for (var i = 0; i < count; i++)
        {
            startReminderTasks[i] = grain.StartReminder($"{Dr}_{i}");
            Log.Info("Started {0}_{1}", Dr, i);
        }

        await Task.WhenAll(startReminderTasks);
        // do comparison on strings
        var registered = (from reminder in startReminderTasks select reminder.Result.ReminderName).ToList();

        Log.Info("Waited");

        var remindersList = await grain.GetRemindersList();
        var fetched = (from reminder in remindersList select reminder.ReminderName).ToList();

        foreach (var remRegistered in registered)
        {
            Assert.True(fetched.Remove(remRegistered),
                $"Couldn't get reminder {remRegistered}. Registered list: {Utils.EnumerableToString(registered)}, fetched list: {Utils.EnumerableToString(remindersList, r => r.ReminderName)}");
        }

        Assert.True(fetched.Count == 0, $"More than registered reminders. Extra: {Utils.EnumerableToString(fetched)}");

        // do some time tests as well
        Log.Info("Time tests");
        var period = await grain.GetReminderPeriod(Dr);
        await Task.Delay(period.Multiply(2.5) + Leeway); // giving some leeway
        for (var i = 0; i < count; i++)
        {
            var curr = await grain.GetCounter($"{Dr}_{i}");
            Assert.Equal(2, curr); // string.Format("Incorrect ticks for {0}_{1}", DR, i));
        }
    }

    public async Task Reminders_1J_MultiGrainMultiReminders()
    {
        var g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        var g2 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        var g3 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        var g4 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        var g5 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());

        var period = await g1.GetReminderPeriod(Dr);

        Task<bool>[] tasks =
        {
            Task.Run(() => PerGrainMultiReminderTestChurn(g1)),
            Task.Run(() => PerGrainMultiReminderTestChurn(g2)),
            Task.Run(() => PerGrainMultiReminderTestChurn(g3)),
            Task.Run(() => PerGrainMultiReminderTestChurn(g4)),
            Task.Run(() => PerGrainMultiReminderTestChurn(g5)),
        };

        Thread.Sleep(period.Multiply(5));
        // start another silo ... although it will take it a while before it stabilizes
        Log.Info("Starting another silo");
        await HostedCluster.StartAdditionalSilosAsync(1, true);

        //Block until all tasks complete.
        await Task.WhenAll(tasks).WithTimeout(Endwait);
    }

    public async Task Reminders_ReminderNotFound()
    {
        var g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());

        // request a reminder that does not exist
        var reminder = await g1.GetReminderObject("blarg");
        Assert.Null(reminder);
    }

    public async Task Rem_Basic()
    {
        // start up a test grain and get the period that it's programmed to use.
        IReminderTestGrain grain = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        TimeSpan period = await grain.GetReminderPeriod(Dr);
        // start up the 'DR' reminder and wait for two ticks to pass.
        await grain.StartReminder(Dr);
        Thread.Sleep(period.Multiply(2) + Leeway); // giving some leeway
        // retrieve the value of the counter-- it should match the sequence number which is the number of periods
        // we've waited.
        long last = await grain.GetCounter(Dr);
        Assert.Equal(2, last);
        // stop the timer and wait for a whole period.
        await grain.StopReminder(Dr);
        Thread.Sleep(period.Multiply(1) + Leeway); // giving some leeway
        // the counter should not have changed.
        long curr = await grain.GetCounter(Dr);
        Assert.Equal(last, curr);
    }

    public async Task Rem_Basic_Restart()
    {
        IReminderTestGrain grain = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        TimeSpan period = await grain.GetReminderPeriod(Dr);
        await grain.StartReminder(Dr);
        Thread.Sleep(period.Multiply(2) + Leeway); // giving some leeway
        long last = await grain.GetCounter(Dr);
        Assert.Equal(2, last);

        await grain.StopReminder(Dr);
        TimeSpan sleepFor = period.Multiply(1) + Leeway;
        Thread.Sleep(sleepFor); // giving some leeway
        long curr = await grain.GetCounter(Dr);
        Assert.Equal(last, curr);
        AssertIsInRange(curr, last, last + 1, grain, Dr, sleepFor);

        // start the same reminder again
        await grain.StartReminder(Dr);
        sleepFor = period.Multiply(2) + Leeway;
        Thread.Sleep(sleepFor); // giving some leeway
        curr = await grain.GetCounter(Dr);
        AssertIsInRange(curr, 2, 3, grain, Dr, sleepFor);
        await grain.StopReminder(Dr); // cleanup
    }

    public async Task Rem_MultipleReminders()
    {
        IReminderTestGrain grain = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        await PerGrainMultiReminderTest(grain);
    }

    public async Task Rem_2J_MultiGrainMultiReminders()
    {
        IReminderTestGrain g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g2 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g3 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g4 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g5 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());

        TimeSpan period = await g1.GetReminderPeriod(Dr);

        Task<bool>[] tasks =
        {
            Task.Run(() => PerGrainMultiReminderTestChurn(g1)),
            Task.Run(() => PerGrainMultiReminderTestChurn(g2)),
            Task.Run(() => PerGrainMultiReminderTestChurn(g3)),
            Task.Run(() => PerGrainMultiReminderTestChurn(g4)),
            Task.Run(() => PerGrainMultiReminderTestChurn(g5)),
        };

        await Task.Delay(period.Multiply(5));

        // start two extra silos ... although it will take it a while before they stabilize
        Log.Info("Starting 2 extra silos");

        await HostedCluster.StartAdditionalSilosAsync(2, true);
        await HostedCluster.WaitForLivenessToStabilizeAsync();

        //Block until all tasks complete.
        await Task.WhenAll(tasks).WithTimeout(Endwait);
    }

    public async Task Rem_MultiGrainMultiReminders()
    {
        IReminderTestGrain g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g2 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g3 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g4 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g5 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());

        Task<bool>[] tasks =
        {
            Task.Run(() => PerGrainMultiReminderTest(g1)),
            Task.Run(() => PerGrainMultiReminderTest(g2)),
            Task.Run(() => PerGrainMultiReminderTest(g3)),
            Task.Run(() => PerGrainMultiReminderTest(g4)),
            Task.Run(() => PerGrainMultiReminderTest(g5)),
        };

        //Block until all tasks complete.
        await Task.WhenAll(tasks).WithTimeout(Endwait);
    }

    public async Task Rem_1F_Basic()
    {
        IReminderTestGrain g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());

        TimeSpan period = await g1.GetReminderPeriod(Dr);

        Task<bool> test = Task.Run(async () =>
        {
            await PerGrainFailureTest(g1);
            return true;
        });

        Thread.Sleep(period.Multiply(FailAfter));
        // stop the secondary silo
        Log.Info("Stopping secondary silo");
        await HostedCluster.StopSiloAsync(HostedCluster.SecondarySilos.First());

        await test; // Block until test completes.
    }

    public async Task Rem_2F_MultiGrain()
    {
        List<SiloHandle> silos = await HostedCluster.StartAdditionalSilosAsync(2, true);

        IReminderTestGrain g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g2 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g3 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g4 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g5 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());

        TimeSpan period = await g1.GetReminderPeriod(Dr);

        Task[] tasks =
        {
            Task.Run(() => PerGrainFailureTest(g1)),
            Task.Run(() => PerGrainFailureTest(g2)),
            Task.Run(() => PerGrainFailureTest(g3)),
            Task.Run(() => PerGrainFailureTest(g4)),
            Task.Run(() => PerGrainFailureTest(g5)),
        };

        Thread.Sleep(period.Multiply(FailAfter));

        // stop a couple of silos
        Log.Info("Stopping 2 silos");
        int i = OrleansTestingBase.Random.Next(silos.Count);
        await HostedCluster.StopSiloAsync(silos[i]);
        silos.RemoveAt(i);
        await HostedCluster.StopSiloAsync(silos[OrleansTestingBase.Random.Next(silos.Count)]);

        await Task.WhenAll(tasks).WithTimeout(Endwait); // Block until all tasks complete.
    }

    public async Task Rem_1F1J_MultiGrain()
    {
        List<SiloHandle> silos = await HostedCluster.StartAdditionalSilosAsync(1);
        await HostedCluster.WaitForLivenessToStabilizeAsync();

        IReminderTestGrain g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g2 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g3 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g4 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g5 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());

        TimeSpan period = await g1.GetReminderPeriod(Dr);

        Task[] tasks =
        {
            Task.Run(() => PerGrainFailureTest(g1)),
            Task.Run(() => PerGrainFailureTest(g2)),
            Task.Run(() => PerGrainFailureTest(g3)),
            Task.Run(() => PerGrainFailureTest(g4)),
            Task.Run(() => PerGrainFailureTest(g5)),
        };

        Thread.Sleep(period.Multiply(FailAfter));

        var siloToKill = silos[OrleansTestingBase.Random.Next(silos.Count)];
        // stop a silo and join a new one in parallel
        Log.Info("Stopping a silo and joining a silo");
        Task t1 = Task.Factory.StartNew(async () => await HostedCluster.StopSiloAsync(siloToKill));
        Task t2 = HostedCluster.StartAdditionalSilosAsync(1, true).ContinueWith(t =>
        {
            t.GetAwaiter().GetResult();
        });
        await Task.WhenAll(new[] { t1, t2 }).WithTimeout(Endwait);

        await Task.WhenAll(tasks).WithTimeout(Endwait); // Block until all tasks complete.
        Log.Info("\n\n\nReminderTest_1F1J_MultiGrain passed OK.\n\n\n");
    }

    public async Task Rem_RegisterSameReminderTwice()
    {
        IReminderTestGrain grain = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        Task<IGrainReminder> promise1 = grain.StartReminder(Dr);
        Task<IGrainReminder> promise2 = grain.StartReminder(Dr);
        Task<IGrainReminder>[] tasks = { promise1, promise2 };
        await Task.WhenAll(tasks).WithTimeout(TimeSpan.FromSeconds(15));
        //Assert.NotEqual(promise1.Result, promise2.Result);
        // TODO: write tests where period of a reminder is changed
    }

    public async Task Rem_GT_Basic()
    {
        IReminderTestGrain g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestCopyGrain g2 = GrainFactory.GetGrain<IReminderTestCopyGrain>(Guid.NewGuid());
        TimeSpan period = await g1.GetReminderPeriod(Dr); // using same period

        await g1.StartReminder(Dr);
        Thread.Sleep(period.Multiply(2) + Leeway); // giving some leeway
        await g2.StartReminder(Dr);
        Thread.Sleep(period.Multiply(2) + Leeway); // giving some leeway
        long last1 = await g1.GetCounter(Dr);
        Assert.Equal(4, last1);
        long last2 = await g2.GetCounter(Dr);
        Assert.Equal(2, last2); // CopyGrain fault

        await g1.StopReminder(Dr);
        Thread.Sleep(period.Multiply(2) + Leeway); // giving some leeway
        await g2.StopReminder(Dr);
        long curr1 = await g1.GetCounter(Dr);
        Assert.Equal(last1, curr1);
        long curr2 = await g2.GetCounter(Dr);
        Assert.Equal(4, curr2); // CopyGrain fault
    }

    public async Task Rem_GT_1F1J_MultiGrain()
    {
        List<SiloHandle> silos = await HostedCluster.StartAdditionalSilosAsync(1);
        await HostedCluster.WaitForLivenessToStabilizeAsync();

        IReminderTestGrain g1 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestGrain g2 = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        IReminderTestCopyGrain g3 = GrainFactory.GetGrain<IReminderTestCopyGrain>(Guid.NewGuid());
        IReminderTestCopyGrain g4 = GrainFactory.GetGrain<IReminderTestCopyGrain>(Guid.NewGuid());

        TimeSpan period = await g1.GetReminderPeriod(Dr);

        Task[] tasks =
        {
            Task.Run(() => PerGrainFailureTest(g1)),
            Task.Run(() => PerGrainFailureTest(g2)),
            Task.Run(() => PerCopyGrainFailureTest(g3)),
            Task.Run(() => PerCopyGrainFailureTest(g4)),
        };

        Thread.Sleep(period.Multiply(FailAfter));

        var siloToKill = silos[OrleansTestingBase.Random.Next(silos.Count)];
        // stop a silo and join a new one in parallel
        Log.Info("Stopping a silo and joining a silo");
        Task t1 = Task.Run(async () => await HostedCluster.StopSiloAsync(siloToKill));
        Task t2 = Task.Run(async () => await HostedCluster.StartAdditionalSilosAsync(1));
        await Task.WhenAll(new[] { t1, t2 }).WithTimeout(Endwait);

        await Task.WhenAll(tasks).WithTimeout(Endwait); // Block until all tasks complete.
    }

    public async Task Rem_Wrong_LowerThanAllowedPeriod()
    {
        IReminderTestGrain grain = GrainFactory.GetGrain<IReminderTestGrain>(Guid.NewGuid());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            grain.StartReminder(Dr, TimeSpan.FromMilliseconds(3000), true));
    }

    public async Task Rem_Wrong_Grain()
    {
        IReminderGrainWrong grain = GrainFactory.GetGrain<IReminderGrainWrong>(0);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            grain.StartReminder(Dr));
    }

    protected async Task<bool> PerGrainMultiReminderTestChurn(IReminderTestGrain g)
    {
        // for churn cases, we do execute start and stop reminders with retries as we don't have the queue-ing 
        // functionality implemented on the LocalReminderService yet
        var period = await g.GetReminderPeriod(Dr);
        Log.Info("PerGrainMultiReminderTestChurn Period={0} Grain={1}", period, g);

        // Start Default Reminder
        //g.StartReminder(DR, file + "_" + DR).Wait();
        await ExecuteWithRetries(g.StartReminder, Dr);
        var sleepFor = period.Multiply(2);
        await Task.Delay(sleepFor);
        // Start R1
        //g.StartReminder(R1, file + "_" + R1).Wait();
        await ExecuteWithRetries(g.StartReminder, R1);
        sleepFor = period.Multiply(2);
        await Task.Delay(sleepFor);
        // Start R2
        //g.StartReminder(R2, file + "_" + R2).Wait();
        await ExecuteWithRetries(g.StartReminder, R2);
        sleepFor = period.Multiply(2);
        await Task.Delay(sleepFor);

        sleepFor = period.Multiply(1);
        await Task.Delay(sleepFor);

        // Stop R1
        //g.StopReminder(R1).Wait();
        await ExecuteWithRetriesStop(g.StopReminder, R1);
        sleepFor = period.Multiply(2);
        await Task.Delay(sleepFor);
        // Stop R2
        //g.StopReminder(R2).Wait();
        await ExecuteWithRetriesStop(g.StopReminder, R2);
        sleepFor = period.Multiply(1);
        await Task.Delay(sleepFor);

        // Stop Default reminder
        //g.StopReminder(DR).Wait();
        await ExecuteWithRetriesStop(g.StopReminder, Dr);
        sleepFor = period.Multiply(1) + Leeway; // giving some leeway
        await Task.Delay(sleepFor);

        var last = await g.GetCounter(R1);
        AssertIsInRange(last, 4, 6, g, R1, sleepFor);

        last = await g.GetCounter(R2);
        AssertIsInRange(last, 4, 6, g, R2, sleepFor);

        last = await g.GetCounter(Dr);
        AssertIsInRange(last, 9, 10, g, Dr, sleepFor);

        return true;
    }

    protected async Task<bool> PerGrainFailureTest(IReminderTestGrain grain)
    {
        var period = await grain.GetReminderPeriod(Dr);

        Log.Info("PerGrainFailureTest Period={0} Grain={1}", period, grain);

        await grain.StartReminder(Dr);
        var sleepFor = period.Multiply(FailCheckAfter) + Leeway; // giving some leeway
        Thread.Sleep(sleepFor);
        var last = await grain.GetCounter(Dr);
        AssertIsInRange(last, FailCheckAfter - 1, FailCheckAfter + 1, grain, Dr, sleepFor);

        await grain.StopReminder(Dr);
        sleepFor = period.Multiply(2) + Leeway; // giving some leeway
        Thread.Sleep(sleepFor);
        var curr = await grain.GetCounter(Dr);

        AssertIsInRange(curr, last, last + 1, grain, Dr, sleepFor);

        return true;
    }

    protected async Task<bool> PerGrainMultiReminderTest(IReminderTestGrain g)
    {
        var period = await g.GetReminderPeriod(Dr);

        Log.Info("PerGrainMultiReminderTest Period={0} Grain={1}", period, g);

        // Each reminder is started 2 periods after the previous reminder
        // once all reminders have been started, stop them every 2 periods
        // except the default reminder, which we stop after 3 periods instead
        // just to test and break the symmetry

        // Start Default Reminder
        await g.StartReminder(Dr);
        var sleepFor = period.Multiply(2) + Leeway; // giving some leeway
        Thread.Sleep(sleepFor);
        var last = await g.GetCounter(Dr);
        AssertIsInRange(last, 1, 2, g, Dr, sleepFor);

        // Start R1
        await g.StartReminder(R1);
        Thread.Sleep(sleepFor);
        last = await g.GetCounter(R1);
        AssertIsInRange(last, 1, 2, g, R1, sleepFor);

        // Start R2
        await g.StartReminder(R2);
        Thread.Sleep(sleepFor);
        last = await g.GetCounter(R1);
        AssertIsInRange(last, 3, 4, g, R1, sleepFor);
        last = await g.GetCounter(R2);
        AssertIsInRange(last, 1, 2, g, R2, sleepFor);
        last = await g.GetCounter(Dr);
        AssertIsInRange(last, 5, 6, g, Dr, sleepFor);

        // Stop R1
        await g.StopReminder(R1);
        Thread.Sleep(sleepFor);
        last = await g.GetCounter(R1);
        AssertIsInRange(last, 3, 4, g, R1, sleepFor);
        last = await g.GetCounter(R2);
        AssertIsInRange(last, 3, 4, g, R2, sleepFor);
        last = await g.GetCounter(Dr);
        AssertIsInRange(last, 7, 8, g, Dr, sleepFor);

        // Stop R2
        await g.StopReminder(R2);
        sleepFor = period.Multiply(3) + Leeway; // giving some leeway
        Thread.Sleep(sleepFor);
        last = await g.GetCounter(R1);
        AssertIsInRange(last, 3, 4, g, R1, sleepFor);
        last = await g.GetCounter(R2);
        AssertIsInRange(last, 3, 4, g, R2, sleepFor);
        last = await g.GetCounter(Dr);
        AssertIsInRange(last, 10, 12, g, Dr, sleepFor);

        // Stop Default reminder
        await g.StopReminder(Dr);
        sleepFor = period.Multiply(1) + Leeway; // giving some leeway
        Thread.Sleep(sleepFor);
        last = await g.GetCounter(R1);
        AssertIsInRange(last, 3, 4, g, R1, sleepFor);
        last = await g.GetCounter(R2);
        AssertIsInRange(last, 3, 4, g, R2, sleepFor);
        last = await g.GetCounter(Dr);
        AssertIsInRange(last, 10, 12, g, Dr, sleepFor);

        return true;
    }

    protected async Task<bool> PerCopyGrainFailureTest(IReminderTestCopyGrain grain)
    {
        var period = await grain.GetReminderPeriod(Dr);

        Log.Info("PerCopyGrainFailureTest Period={0} Grain={1}", period, grain);

        await grain.StartReminder(Dr);
        Thread.Sleep(period.Multiply(FailCheckAfter) + Leeway); // giving some leeway
        var last = await grain.GetCounter(Dr);
        Assert.Equal(FailCheckAfter, last); // "{0} CopyGrain {1} Reminder {2}" // Time(), grain.GetPrimaryKey(), DR);

        await grain.StopReminder(Dr);
        Thread.Sleep(period.Multiply(2) + Leeway); // giving some leeway
        var curr = await grain.GetCounter(Dr);
        Assert.Equal(last, curr); // "{0} CopyGrain {1} Reminder {2}", Time(), grain.GetPrimaryKey(), DR);

        return true;
    }
    
    protected static string Time()
    {
        return DateTime.UtcNow.ToString("hh:mm:ss.fff");
    }

    protected void AssertIsInRange(long val, long lowerLimit, long upperLimit, IGrain grain, string reminderName,
        TimeSpan sleepFor)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("Grain: {0} Grain PrimaryKey: {1}, Reminder: {2}, SleepFor: {3} Time now: {4}",
            grain, grain.GetPrimaryKey(), reminderName, sleepFor, Time());
        sb.AppendFormat(
            " -- Expecting value in the range between {0} and {1}, and got value {2}.",
            lowerLimit, upperLimit, val);
        Log.Info(sb.ToString());

        var tickCountIsInsideRange = lowerLimit <= val && val <= upperLimit;

        Skip.IfNot(tickCountIsInsideRange, $"AssertIsInRange: {sb}  -- WHICH IS OUTSIDE RANGE.");
    }

    protected async Task ExecuteWithRetries(Func<string, TimeSpan?, bool, Task> function, string reminderName,
        TimeSpan? period = null, bool validate = false)
    {
        for (long i = 1; i <= Retries; i++)
        {
            try
            {
                await function(reminderName, period, validate).WithTimeout(TestConstants.InitTimeout);
                return; // success ... no need to retry
            }
            catch (AggregateException aggEx)
            {
                aggEx.Handle(exc => HandleError(exc, i));
            }
            catch (ReminderException exc)
            {
                HandleError(exc, i);
            }
        }

        // execute one last time and bubble up errors if any
        await function(reminderName, period, validate).WithTimeout(TestConstants.InitTimeout);
    }

    // Func<> doesnt take optional parameters, thats why we need a separate method
    protected async Task ExecuteWithRetriesStop(Func<string, Task> function, string reminderName)
    {
        for (long i = 1; i <= Retries; i++)
        {
            try
            {
                await function(reminderName).WithTimeout(TestConstants.InitTimeout);
                return; // success ... no need to retry
            }
            catch (AggregateException aggEx)
            {
                aggEx.Handle(exc => HandleError(exc, i));
            }
            catch (ReminderException exc)
            {
                HandleError(exc, i);
            }
        }

        // execute one last time and bubble up errors if any
        await function(reminderName).WithTimeout(TestConstants.InitTimeout);
    }

    private bool HandleError(Exception ex, long i)
    {
        if (ex is AggregateException)
        {
            ex = ((AggregateException)ex).Flatten().InnerException;
        }

        if (ex is ReminderException)
        {
            Log.Info("Retriable operation failed on attempt {0}: {1}", i, ex.ToString());
            Thread.Sleep(TimeSpan.FromMilliseconds(10)); // sleep a bit before retrying
            return true;
        }

        return false;
    }
}