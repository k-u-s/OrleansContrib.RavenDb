using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration.Overrides;
using Orleans.Storage;
using OrleansContrib.Persistence.RavenDb.Options;

namespace OrleansContrib.Persistence.RavenDb.StorageProviders;

public class GrainStorageFactory
{
    internal static IGrainStorage Create(IServiceProvider services, string name)
    {
        var optionsSnapshot = services.GetRequiredService<IOptionsSnapshot<GrainStorageOptions>>();
        return ActivatorUtilities.CreateInstance<GrainStorage>(services, name, optionsSnapshot.Get(name),
            services.GetProviderClusterOptions(name));
    }
}