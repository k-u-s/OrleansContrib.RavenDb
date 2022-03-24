using OrleansContrib.Tester.Internals;

namespace OrleansContrib.Tester;

public abstract class OrleansTestingBase
{
    public static class Random
    {
        public static int Next() => ThreadSafeRandom.Next();
        public static int Next(int maxValue) => ThreadSafeRandom.Next(maxValue);
        public static double NextDouble() => ThreadSafeRandom.NextDouble();
    }

    public static long GetRandomGrainId()
    {
        return ThreadSafeRandom.Next();
    }
}