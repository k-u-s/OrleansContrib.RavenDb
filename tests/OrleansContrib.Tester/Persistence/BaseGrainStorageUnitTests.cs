
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.TestingHost.Utils;
using OrleansContrib.Tester.Persistence.Runners;
using OrleansContrib.Tester.Persistence.States;
using Xunit;

namespace OrleansContrib.Tester.Persistence;


[Collection(TestConstants.DefaultCollection)]
public abstract class BaseGrainStorageUnitTests : IAsyncLifetime
{
    protected readonly BasePersistenceTestClusterFixture ClusterFixture;
    private readonly ILogger logger;

    protected ILoggerFactory LoggerFactory;
    protected IOptions<ClusterOptions> ClusterOptions;
    protected IGrainStorage GrainStorage;

    /// <summary>
    /// The tests and assertions common across all back-ends are here.
    /// </summary>
    internal CommonStorageTestsRunner PersistenceStorageTests { get; set; }

    protected const string TestDatabaseName = "OrleansPersistenceTest"; //for relational storage

    protected BaseGrainStorageUnitTests(BasePersistenceTestClusterFixture clusterFixture)
    {
        ClusterFixture = clusterFixture;
        LoggerFactory = CreateLoggerFactory();
        logger = LoggerFactory.CreateLogger<BaseGrainStorageUnitTests>();
        var serviceId = $"{Guid.NewGuid()}/foo";
        var clusterId = $"test-{serviceId}/foo2";

        logger.Info("ClusterId={0}", clusterId);
        ClusterOptions = Options.Create(new ClusterOptions { ClusterId = clusterId, ServiceId = serviceId });

    }

    protected abstract Task<IGrainStorage> CreateGrainStorage();

    protected virtual ILoggerFactory CreateLoggerFactory() 
        => TestingUtils.CreateDefaultLoggerFactory($"{GetType()}.log");
    
    public virtual async Task InitializeAsync()
    {
        GrainStorage = await CreateGrainStorage();
        PersistenceStorageTests = new CommonStorageTestsRunner(
            ClusterFixture.GrainFactory, GrainStorage);
    }

    public virtual async Task DisposeAsync()
    {
        if (GrainStorage is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
    }

    [SkippableFact]
    [TestCategory("Functional")]
    public virtual async Task Relational_WriteReadWriteRead100StatesInParallel()
    {
        await PersistenceStorageTests.PersistenceStorage_WriteReadWriteReadStatesInParallel(
            nameof(Relational_WriteReadWriteRead100StatesInParallel));
    }

    [SkippableFact]
    [TestCategory("Functional")]
    public virtual async Task Relational_HashCollisionTests()
    {
        await PersistenceStorageTests.PersistenceStorage_WriteReadWriteReadStatesInParallel(
            nameof(Relational_HashCollisionTests), 2);
    }

    [SkippableFact]
    [TestCategory("Functional")]
    public virtual async Task Relational_WriteDuplicateFailsWithInconsistentStateException()
    {
        var exception = await PersistenceStorageTests
            .PersistenceStorage_WriteDuplicateFailsWithInconsistentStateException();
        AssertRelationalInconsistentExceptionMessage(exception);
    }

    [SkippableFact]
    [TestCategory("Functional")]
    public virtual async Task Relational_WriteInconsistentFailsWithIncosistentStateException()
    {
        var exception = await PersistenceStorageTests
            .PersistenceStorage_WriteInconsistentFailsWithInconsistentStateException();
        AssertRelationalInconsistentExceptionMessage(exception);
    }

    [SkippableFact]
    [TestCategory("Functional")]
    public virtual async Task WriteReadCyrillic()
    {
        await PersistenceStorageTests.PersistenceStorage_Relational_WriteReadIdCyrillic();
    }

    [SkippableTheory, ClassData(typeof(StorageDataSetPlain<long>))]
    [TestCategory("Functional")]
    public virtual async Task StorageDataSetPlain_IntegerKey_WriteClearRead(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestState1> grainState)
    {
        await PersistenceStorageTests.Store_WriteClearRead(grainType,
            getGrainReference(ClusterFixture.GrainFactory), grainState);
    }


    [SkippableTheory, ClassData(typeof(StorageDataSetPlain<Guid>))]
    [TestCategory("Functional")]
    public virtual async Task StorageDataSetPlain_GuidKey_WriteClearRead(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestState1> grainState)
    {
        await PersistenceStorageTests.Store_WriteClearRead(grainType,
            getGrainReference(ClusterFixture.GrainFactory), grainState);
    }


    [SkippableTheory, ClassData(typeof(StorageDataSetPlain<string>))]
    [TestCategory("Functional")]
    public virtual async Task PersistenceStorage_StorageDataSetPlain_StringKey_WriteClearRead(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestState1> grainState)
    {
        await PersistenceStorageTests.Store_WriteClearRead(grainType,
            getGrainReference(ClusterFixture.GrainFactory), grainState);
    }


    [SkippableTheory, ClassData(typeof(StorageDataSet2CyrillicIdsAndGrainNames<string>))]
    [TestCategory("Functional")]
    public virtual async Task DataSet2_Cyrillic_WriteClearRead(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestStateGeneric1<string>> grainState)
    {
        await PersistenceStorageTests.Store_WriteClearRead(grainType,
            getGrainReference(ClusterFixture.GrainFactory), grainState);
    }


    [SkippableTheory, ClassData(typeof(StorageDataSetGeneric<long, string>))]
    [TestCategory("Functional")]
    public virtual async Task StorageDataSetGeneric_IntegerKey_Generic_WriteClearRead(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestStateGeneric1<string>> grainState)
    {
        await PersistenceStorageTests.Store_WriteClearRead(grainType,
            getGrainReference(ClusterFixture.GrainFactory), grainState);
    }


    [SkippableTheory, ClassData(typeof(StorageDataSetGeneric<Guid, string>))]
    [TestCategory("Functional")]
    public virtual async Task StorageDataSetGeneric_GuidKey_Generic_WriteClearRead(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestStateGeneric1<string>> grainState)
    {
        await PersistenceStorageTests.Store_WriteClearRead(grainType,
            getGrainReference(ClusterFixture.GrainFactory), grainState);
    }


    [SkippableTheory, ClassData(typeof(StorageDataSetGeneric<string, string>))]
    [TestCategory("Functional")]
    public virtual async Task StorageDataSetGeneric_StringKey_Generic_WriteClearRead(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestStateGeneric1<string>> grainState)
    {
        await PersistenceStorageTests.Store_WriteClearRead(grainType,
            getGrainReference(ClusterFixture.GrainFactory), grainState);
    }


    [SkippableTheory, ClassData(typeof(StorageDataSetGeneric<string, string>))]
    [TestCategory("Functional")]
    public virtual async Task StorageDataSetGeneric_Json_WriteRead(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestStateGeneric1<string>> grainState)
    {
        var grainReference = getGrainReference(ClusterFixture.GrainFactory);
        await PersistenceStorageTests.Store_WriteRead(grainType, grainReference, grainState);
    }


    [SkippableTheory, ClassData(typeof(StorageDataSetGenericHuge<string, string>))]
    [TestCategory("Functional")]
    public virtual async Task StorageDataSetGenericHuge_Json_WriteReadStreaming(string grainType,
        Func<IGrainFactory, GrainReference> getGrainReference, GrainState<TestStateGeneric1<string>> grainState)
    {
        await PersistenceStorageTests.Store_WriteRead(grainType, getGrainReference(ClusterFixture.GrainFactory),
            grainState);
    }
    
    /// <summary>
    /// Asserts certain information is present in the <see cref="Orleans.Storage.InconsistentStateException"/>.
    /// </summary>
    /// <param name="exceptionMessage">The exception message to assert.</param>
    public virtual void AssertRelationalInconsistentExceptionMessage(Exception exception)
    {
        Assert.NotNull(exception);
    }
}