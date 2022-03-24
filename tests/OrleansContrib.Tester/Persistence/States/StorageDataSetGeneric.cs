using System;
using Orleans;
using Orleans.Runtime;
using OrleansContrib.Tester.Internals;
using Xunit;

namespace OrleansContrib.Tester.Persistence.States;

public sealed class StorageDataSetGeneric<TGrainKey, TStateData> : TheoryData<TGrainKey, Func<IGrainFactory, GrainReference>, GrainState<TestStateGeneric1<TStateData>>>
{
    public StorageDataSetGeneric()
    {
        AddRow(
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory => RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory)),
            new GrainState<TestStateGeneric1<TStateData>> { State = new TestStateGeneric1<TStateData> { SomeData = RandomUtilities.GetRandom<TStateData>(), A = "Data1", B = 1, C = 4 } });
        AddRow(
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory => RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory)),
            new GrainState<TestStateGeneric1<TStateData>> { State = new TestStateGeneric1<TStateData> { SomeData = RandomUtilities.GetRandom<TStateData>(), A = "Data2", B = 2, C = 5 } });
        AddRow(
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory => RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory)),
            new GrainState<TestStateGeneric1<TStateData>> { State = new TestStateGeneric1<TStateData> { SomeData = RandomUtilities.GetRandom<TStateData>(), A = "Data3", B = 3, C = 6 } });
    }
}