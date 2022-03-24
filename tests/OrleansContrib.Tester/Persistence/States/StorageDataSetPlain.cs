using System;
using System.Collections;
using System.Collections.Generic;
using Orleans;
using Orleans.Runtime;
using OrleansContrib.Tester.Internals;

namespace OrleansContrib.Tester.Persistence.States;

/// <summary>
/// A set of simple test data set wit and without extension keys.
/// </summary>
/// <typeparam name="TGrainKey">The grain type (integer, guid or string)</typeparam>.
public sealed class StorageDataSetPlain<TGrainKey> : IEnumerable<object[]>
{
    /// <summary>
    /// The symbol set this data set uses.
    /// </summary>
    private static SymbolSet Symbols { get; } = new SymbolSet(SymbolSet.Latin1);

    /// <summary>
    /// The length of random string drawn form <see cref="Symbols"/>.
    /// </summary>
    private const long StringLength = 15L;


    private IEnumerable<object[]> DataSet { get; } = new[]
    {
        new object[]
        {
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory =>
                RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory, extensionKey: false)),
            new GrainState<TestState1>
            {
                State = new TestState1 { A = RandomUtilities.GetRandomCharacters(Symbols, StringLength), B = 1, C = 4 }
            }
        },
        new object[]
        {
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory =>
                RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory, true)),
            new GrainState<TestState1>
            {
                State = new TestState1 { A = RandomUtilities.GetRandomCharacters(Symbols, StringLength), B = 2, C = 5 }
            }
        },
        new object[]
        {
            GrainTypeGenerator.GetGrainType<TGrainKey>(),
            (Func<IGrainFactory, GrainReference>)(grainFactory =>
                RandomUtilities.GetRandomGrainReference<TGrainKey>(grainFactory, true)),
            new GrainState<TestState1>
            {
                State = new TestState1 { A = RandomUtilities.GetRandomCharacters(Symbols, StringLength), B = 3, C = 6 }
            }
        }
    };

    public IEnumerator<object[]> GetEnumerator()
    {
        return DataSet.GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}