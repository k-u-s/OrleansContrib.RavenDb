using System.Threading.Tasks;
using OrleansContrib.Tester.Persistence.Runners;
using Xunit;
using Xunit.Abstractions;

namespace OrleansContrib.Tester.Persistence;


[Collection(TestConstants.DefaultCollection)]
public abstract class BasePersistenceGrainIntegrationTests : OrleansTestingBase
{
    private readonly GrainPersistenceTestsRunner basicPersistenceTestsRunner;

    protected BasePersistenceGrainIntegrationTests(
        ITestOutputHelper output,
        BasePersistenceTestClusterFixture fixture)
    {
        basicPersistenceTestsRunner = new GrainPersistenceTestsRunner(output, fixture);
        
        fixture.EnsurePreconditionsMet();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_GrainStore_Delete()
    {
        return basicPersistenceTestsRunner.Grain_GrainStorage_Delete();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_GrainStore_Read()
    {
        return basicPersistenceTestsRunner.Grain_GrainStorage_Read();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_GuidKey_GrainStore_Read_Write()
    {
        return basicPersistenceTestsRunner.Grain_GuidKey_GrainStorage_Read_Write();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_LongKey_GrainStore_Read_Write()
    {
        return basicPersistenceTestsRunner.Grain_LongKey_GrainStorage_Read_Write();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_LongKeyExtended_GrainStore_Read_Write()
    {
        return basicPersistenceTestsRunner.Grain_LongKeyExtended_GrainStorage_Read_Write();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_GuidKeyExtended_GrainStore_Read_Write()
    {
        return basicPersistenceTestsRunner.Grain_GuidKeyExtended_GrainStorage_Read_Write();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_Generic_GrainStore_Read_Write()
    {
        return basicPersistenceTestsRunner.Grain_Generic_GrainStorage_Read_Write();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_Generic_GrainStore_DiffTypes()
    {
        return basicPersistenceTestsRunner.Grain_Generic_GrainStorage_DiffTypes();
    }

    [SkippableFact, TestCategory("Functional")]
    public virtual Task Grain_GrainStore_SiloRestart()
    {
        return basicPersistenceTestsRunner.Grain_GrainStorage_SiloRestart();
    }
}
