using System;
using Orleans;
using Orleans.Runtime;
using OrleansContrib.Tester.Internals;
using Xunit;

namespace OrleansContrib.Tester.Persistence.States;

public sealed class StorageDataSetGenericHuge<TGrainKey, TStateData> : TheoryData<TGrainKey, Func<IGrainFactory, GrainReference>, GrainState<TestStateGeneric1<TStateData>>>
{
    private static Range<long> CountOfCharacters { get; } = new Range<long>(1000000, 1000000);

    public StorageDataSetGenericHuge()
    {
        AddRow(
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory => RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory)),
            new GrainState<TestStateGeneric1<TStateData>> { State = new TestStateGeneric1<TStateData> { SomeData = RandomUtilities.GetRandom<TStateData>(CountOfCharacters), A = "Data1", B = 1, C = 4 } });
        AddRow(
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory => RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory)),
            new GrainState<TestStateGeneric1<TStateData>> { State = new TestStateGeneric1<TStateData> { SomeData = RandomUtilities.GetRandom<TStateData>(CountOfCharacters), A = "Data2", B = 2, C = 5 } });
        AddRow(
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory => RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory)),
            new GrainState<TestStateGeneric1<TStateData>> { State = new TestStateGeneric1<TStateData> { SomeData = RandomUtilities.GetRandom<TStateData>(CountOfCharacters), A = "Data3", B = 3, C = 6 } });
    }
}