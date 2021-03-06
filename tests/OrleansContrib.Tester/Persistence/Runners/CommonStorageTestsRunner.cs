using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;
using OrleansContrib.Tester.Internals;
using OrleansContrib.Tester.Persistence.Grains;
using OrleansContrib.Tester.Persistence.States;
using Xunit;

namespace OrleansContrib.Tester.Persistence.Runners;

public class CommonStorageTestsRunner
{
    private readonly IGrainFactory grainFactory;

    /// <summary>
    /// The default constructor.
    /// </summary>
    /// <param name="grainFactory"></param>
    /// <param name="storage"></param>
    public CommonStorageTestsRunner(IGrainFactory grainFactory, IGrainStorage storage)
    {
        this.grainFactory = grainFactory;
        Storage = storage;
    }

    /// <summary>
    /// The storage provider under test.
    /// </summary>
    public IGrainStorage Storage { get; }

    /// <summary>
    /// Creates a new grain and a grain reference pair.
    /// </summary>
    /// <param name="grainId">The grain ID.</param>
    /// <param name="version">The initial version of the state.</param>
    /// <returns>A grain reference and a state pair.</returns>
    internal Tuple<GrainReference, GrainState<TestState1>> GetTestReferenceAndState(long grainId, string version)
    {
        var grain = grainFactory.GetGrain<IGrainStorageTestGrainLongKey>(grainId);
        return Tuple.Create(
            (GrainReference)grain,
            new GrainState<TestState1> { State = new TestState1(), ETag = version });
    }

    /// <summary>
    /// Creates a new grain and a grain reference pair.
    /// </summary>
    /// <param name="grainId">The grain ID.</param>
    /// <param name="version">The initial version of the state.</param>
    /// <returns>A grain reference and a state pair.</returns>
    internal Tuple<GrainReference, GrainState<TestState1>> GetTestReferenceAndState(string grainId, string version)
    {
        var grain = grainFactory.GetGrain<IGrainStorageTestGrainStringKey>(grainId);
        return Tuple.Create(
            (GrainReference)grain,
            new GrainState<TestState1> { State = new TestState1(), ETag = version });
    }

    /// <summary>
    /// Writes a known inconsistent state to the storage and asserts an exception will be thrown.
    /// </summary>
    /// <returns></returns>
    internal async Task PersistenceStorage_WriteReadIdCyrillic()
    {
        var grainTypeName = GrainTypeGenerator.GetGrainType<Guid>();
        var grainReference = GetTestReferenceAndState(0, null);
        var grainState = grainReference.Item2;
        await Storage.WriteStateAsync(grainTypeName, grainReference.Item1, grainState).ConfigureAwait(false);
        var storedGrainState = new GrainState<TestState1> { State = new TestState1() };
        await Storage.ReadStateAsync(grainTypeName, grainReference.Item1, storedGrainState).ConfigureAwait(false);

        Assert.Equal(grainState.ETag, storedGrainState.ETag);
        Assert.Equal(grainState.State, storedGrainState.State);
    }

    /// <summary>
    /// Writes to storage and tries to re-write the same state with NULL as ETag, as if the
    /// grain was just created.
    /// </summary>
    /// <returns>
    /// The <see cref="InconsistentStateException"/> thrown by the provider. This can be further
    /// inspected by the storage specific asserts.
    /// </returns>
    internal async Task<InconsistentStateException>
        PersistenceStorage_WriteDuplicateFailsWithInconsistentStateException()
    {
        //A grain with a random ID will be arranged to the database. Then its state is set to null to simulate the fact
        //it is like a second activation after a one that has succeeded to write.
        string grainTypeName = GrainTypeGenerator.GetGrainType<Guid>();
        var inconsistentState = GetTestReferenceAndState(RandomUtilities.GetRandom<long>(), null);
        var grainReference = inconsistentState.Item1;
        var grainState = inconsistentState.Item2;

        await Store_WriteRead(grainTypeName, inconsistentState.Item1, inconsistentState.Item2).ConfigureAwait(false);
        grainState.ETag = null;
        var exception = await Record.ExceptionAsync(() => Store_WriteRead(grainTypeName, grainReference, grainState))
            .ConfigureAwait(false);

        Assert.NotNull(exception);
        Assert.IsType<InconsistentStateException>(exception);

        return (InconsistentStateException)exception;
    }

    /// <summary>
    /// Writes a known inconsistent state to the storage and asserts an exception will be thrown.
    /// </summary>
    /// <returns>
    /// The <see cref="InconsistentStateException"/> thrown by the provider. This can be further
    /// inspected by the storage specific asserts.
    /// </returns>
    internal async Task<InconsistentStateException>
        PersistenceStorage_WriteInconsistentFailsWithInconsistentStateException()
    {
        //Some version not expected to be in the storage for this type and ID.
        var inconsistentStateVersion = RandomUtilities.GetRandom<int>().ToString(CultureInfo.InvariantCulture);

        var inconsistentState =
            GetTestReferenceAndState(RandomUtilities.GetRandom<long>(), inconsistentStateVersion);
        string grainTypeName = GrainTypeGenerator.GetGrainType<Guid>();
        var exception = await Record
            .ExceptionAsync(() => Store_WriteRead(grainTypeName, inconsistentState.Item1, inconsistentState.Item2))
            .ConfigureAwait(false);

        Assert.NotNull(exception);
        Assert.IsType<InconsistentStateException>(exception);

        return (InconsistentStateException)exception;
    }

    internal async Task PersistenceStorage_WriteReadWriteReadStatesInParallel(
        string prefix = nameof(PersistenceStorage_WriteReadWriteReadStatesInParallel), int countOfGrains = 100)
    {
        //As data is written and read the Version numbers (ETags) are as checked for correctness (they change).
        //Additionally the Store_WriteRead tests does its validation.
        var grainTypeName = GrainTypeGenerator.GetGrainType<Guid>();
        int startOfRange = 33900;
        int countOfRange = countOfGrains;
        string grainIdTemplate = $"{prefix}-{{0}}";

        //Since the version is NULL, storage provider tries to insert this data
        //as new state. If there is already data with this class, the writing fails
        //and the storage provider throws. Essentially it means either this range
        //is ill chosen or the test failed due to another problem.
        var grainStates = Enumerable.Range(startOfRange, countOfRange)
            .Select(i => GetTestReferenceAndState(string.Format(grainIdTemplate, i), null)).ToList();

        // Avoid parallelization of the first write to not stress out the system with deadlocks
        // on INSERT
        foreach (var grainData in grainStates)
        {
            //A sanity checker that the first version really has null as its state. Then it is stored
            //to the database and a new version is acquired.
            var firstVersion = grainData.Item2.ETag;
            Assert.Null(firstVersion);

            await Store_WriteRead(grainTypeName, grainData.Item1, grainData.Item2).ConfigureAwait(false);
            var secondVersion = grainData.Item2.ETag;
            Assert.NotEqual(firstVersion, secondVersion);
        }

        ;

        int maxNumberOfThreads = Environment.ProcessorCount * 3;
        // The purpose of Parallel.ForEach is to ensure the storage provider will be tested from
        // multiple threads concurrently, as would happen in running system also. Nevertheless
        // limit the degree of parallelization (concurrent threads) to avoid unnecessarily
        // starving and growing the thread pool (which is very slow) if a few threads coupled
        // with parallelization via tasks can force most concurrency scenarios.

        Parallel.ForEach(grainStates, new ParallelOptions { MaxDegreeOfParallelism = maxNumberOfThreads },
            async grainData =>
            {
                // This loop writes the state consecutive times to the database to make sure its
                // version is updated appropriately.
                for (int k = 0; k < 10; ++k)
                {
                    var versionBefore = grainData.Item2.ETag;
                    await RetryHelper.RetryOnExceptionAsync(5, RetryOperation.Sigmoid, async () =>
                    {
                        await Store_WriteRead(grainTypeName, grainData.Item1, grainData.Item2);
                        return Task.CompletedTask;
                    });

                    var versionAfter = grainData.Item2.ETag;
                    Assert.NotEqual(versionBefore, versionAfter);
                }
            });
    }

    /// <summary>
    /// Writes to storage, clears and reads back and asserts both the version and the state.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="grainTypeName">The type of the grain.</param>
    /// <param name="grainReference">The grain reference as would be given by Orleans.</param>
    /// <param name="grainState">The grain state the grain would hold and Orleans pass.</param>
    /// <returns></returns>
    internal async Task Store_WriteClearRead<T>(string grainTypeName, GrainReference grainReference,
        GrainState<T> grainState) where T : new()
    {
        //A legal situation for clearing has to be arranged by writing a state to the storage before
        //clearing it. Writing and clearing both change the ETag, so they should differ.
        await Storage.WriteStateAsync(grainTypeName, grainReference, grainState);
        var writtenStateVersion = grainState.ETag;
        var recordExitsAfterWriting = grainState.RecordExists;

        await Storage.ReadStateAsync(grainTypeName, grainReference, grainState).ConfigureAwait(false);
        var stateVersionReadAfterWriting = grainState.ETag;
        var recordExitsAfterWritingAndReading = grainState.RecordExists;

        await Storage.ClearStateAsync(grainTypeName, grainReference, grainState).ConfigureAwait(false);
        var clearedStateVersion = grainState.ETag;
        var recordExitsAfterClearing = grainState.RecordExists;

        await Storage.ReadStateAsync(grainTypeName, grainReference, grainState).ConfigureAwait(false);
        var stateVersionAfterClearingAndReading = grainState.ETag;
        var recordExitsAfterClearingAndReading = grainState.RecordExists;

        Assert.NotEqual(writtenStateVersion, clearedStateVersion);
        Assert.Equal(writtenStateVersion, stateVersionReadAfterWriting);
        Assert.Equal(stateVersionAfterClearingAndReading, clearedStateVersion);
        Assert.Equal(grainState.State, Activator.CreateInstance<T>());
        Assert.True(recordExitsAfterWriting);
        Assert.True(recordExitsAfterWritingAndReading);
        Assert.False(recordExitsAfterClearing);
        Assert.False(recordExitsAfterClearingAndReading);
    }

    /// <summary>
    /// Writes to storage, reads back and asserts both the version and the state.
    /// </summary>
    /// <typeparam name="T">The grain state type.</typeparam>
    /// <param name="grainTypeName">The type of the grain.</param>
    /// <param name="grainReference">The grain reference as would be given by Orleans.</param>
    /// <param name="grainState">The grain state the grain would hold and Orleans pass.</param>
    /// <returns></returns>
    internal async Task Store_WriteRead<T>(string grainTypeName, GrainReference grainReference,
        GrainState<T> grainState) where T : new()
    {
        await Storage.WriteStateAsync(grainTypeName, grainReference, grainState).ConfigureAwait(false);
        var storedGrainState = new GrainState<T> { State = new T() };
        await Storage.ReadStateAsync(grainTypeName, grainReference, storedGrainState).ConfigureAwait(false);

        Assert.Equal(grainState.ETag, storedGrainState.ETag);
        Assert.Equal(grainState.State, storedGrainState.State);
        Assert.True(storedGrainState.RecordExists);
    }
}