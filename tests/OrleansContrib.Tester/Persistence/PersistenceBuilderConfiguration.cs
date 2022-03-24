using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using OrleansContrib.Tester.Persistence.Grains;

namespace OrleansContrib.Tester.Persistence;

internal class PersistenceSiloBuilderConfiguration : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder
            .AddMemoryGrainStorage(TestConstants.StorageProviderMemory)
            .AddMemoryGrainStorage(TestConstants.StorageProviderDefault)
            .ConfigureApplicationParts(parts =>
                parts.AddApplicationPart(typeof(GrainStorageTestGrain).Assembly).WithReferences());
    }
}

internal class PersistenceClientBuilderConfiguration : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder
            .ConfigureApplicationParts(parts =>
                parts.AddApplicationPart(typeof(IGrainStorageTestGrain).Assembly).WithReferences());
    }
}