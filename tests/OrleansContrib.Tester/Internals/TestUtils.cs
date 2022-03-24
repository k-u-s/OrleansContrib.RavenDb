using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace OrleansContrib.Tester.Internals;

public class TestUtils
{
    public static readonly Random Random = new Random();

    public static long GetRandomGrainId()
    {
        return Random.Next();
    }

    public static double CalibrateTimings()
    {
        const int numLoops = 10000;
        TimeSpan baseline = TimeSpan.FromTicks(80); // Baseline from jthelin03D
        int n;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < numLoops; i++)
        {
            n = i;
        }

        sw.Stop();
        double multiple = 1.0 * sw.ElapsedTicks / baseline.Ticks;
        Console.WriteLine("CalibrateTimings: {0} [{1} Ticks] vs {2} [{3} Ticks] = x{4}",
            sw.Elapsed, sw.ElapsedTicks,
            baseline, baseline.Ticks,
            multiple);
        return multiple > 1.0 ? multiple : 1.0;
    }

    public static TimeSpan TimeRun(int numIterations, TimeSpan baseline, string what, Action action)
    {
        var stopwatch = new Stopwatch();

        long startMem = GC.GetTotalMemory(true);
        stopwatch.Start();

        action();

        stopwatch.Stop();
        long stopMem = GC.GetTotalMemory(false);
        long memUsed = stopMem - startMem;
        TimeSpan duration = stopwatch.Elapsed;

        string timeDeltaStr = "";
        if (baseline > TimeSpan.Zero)
        {
            double delta = (duration - baseline).TotalMilliseconds / baseline.TotalMilliseconds;
            timeDeltaStr = String.Format("-- Change = {0}%", 100.0 * delta);
        }

        Console.WriteLine("Time for {0} loops doing {1} = {2} {3} Memory used={4}", numIterations, what, duration,
            timeDeltaStr, memUsed);
        return duration;
    }

    public static void ConfigureClientThreadPoolSettingsForStorageTests(int numDotNetPoolThreads = 200)
    {
        ThreadPool.SetMinThreads(numDotNetPoolThreads, numDotNetPoolThreads);
        ServicePointManager.Expect100Continue = false;
        ServicePointManager.DefaultConnectionLimit = numDotNetPoolThreads; // 1000;
        ServicePointManager.UseNagleAlgorithm = false;
    }

    public static async Task<int> GetActivationCount(IGrainFactory grainFactory, string fullTypeName)
    {
        int result = 0;

        IManagementGrain mgmtGrain = grainFactory.GetGrain<IManagementGrain>(0);
        SimpleGrainStatistic[] stats = await mgmtGrain.GetSimpleGrainStatistics();
        foreach (var stat in stats)
        {
            if (stat.GrainType == fullTypeName)
                result += stat.ActivationCount;
        }

        return result;
    }
}

public static class RequestContextTestUtils
{
    public static void SetActivityId(Guid id)
    {
        RequestContext.ActivityId = id;
    }

    public static Guid GetActivityId()
    {
        return RequestContext.ActivityId;
    }

    public static void ClearActivityId()
    {
        Trace.CorrelationManager.ActivityId = Guid.Empty;
        RequestContext.ActivityId = Guid.Empty;
    }
}