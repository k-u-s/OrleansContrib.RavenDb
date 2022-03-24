using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using OrleansContrib.Tester.Reminders.Grains;

namespace OrleansContrib.Tester.Reminders;

internal class ReminderSiloBuilderConfiguration : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder
            .ConfigureApplicationParts(parts =>
                parts.AddApplicationPart(typeof(ReminderTestGrain).Assembly).WithReferences());
    }
}

internal class ReminderClientBuilderConfiguration : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder
            .ConfigureApplicationParts(parts =>
                parts.AddApplicationPart(typeof(IReminderTestGrain).Assembly).WithReferences());
    }
}