using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Storage;
using OrleansContrib.Persistence.RavenDb.Options;
using OrleansContrib.Persistence.RavenDb.StorageProviders;

namespace OrleansContrib.Persistence.RavenDb.Extensions;

public static class SiloHostBuilderExtensions
{
    public static ISiloBuilder AddRavenGrainStorage(this ISiloBuilder builder, string providerName, Action<GrainStorageOptions> options)
        => builder.ConfigureServices(services => services.AddRavenGrainStorage(providerName, options));

    public static ISiloHostBuilder AddRavenGrainStorage(this ISiloHostBuilder builder, string providerName, Action<GrainStorageOptions> options)
        => builder.ConfigureServices(services => services.AddRavenGrainStorage(providerName, options));

    public static IServiceCollection AddRavenGrainStorage(this IServiceCollection services, string providerName, Action<GrainStorageOptions> options)
    {
        services.AddOptions<GrainStorageOptions>(providerName).Configure(options);
        return services
            .AddSingletonNamedService(providerName, GrainStorageFactory.Create)
            .AddSingletonNamedService(providerName, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
    }
}