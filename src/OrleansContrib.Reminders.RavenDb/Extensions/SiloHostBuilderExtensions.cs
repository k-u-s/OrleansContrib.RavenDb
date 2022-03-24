using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;
using OrleansContrib.Reminders.RavenDb.Options;
using OrleansContrib.Reminders.RavenDb.Reminders;

namespace OrleansContrib.Reminders.RavenDb.Extensions;

public static class SiloHostBuilderExtensions
{
    public static ISiloBuilder UseRavenReminderService(this ISiloBuilder builder, Action<OptionsBuilder<ReminderTableOptions>> configureOptions)
        => builder.ConfigureServices(services => services.UseRavenReminderService(configureOptions));

    public static ISiloHostBuilder UseRavenReminderService(this ISiloHostBuilder builder, Action<OptionsBuilder<ReminderTableOptions>> configureOptions)
        => builder.ConfigureServices(services => services.UseRavenReminderService(configureOptions));

    public static IServiceCollection UseRavenReminderService(this IServiceCollection services, Action<OptionsBuilder<ReminderTableOptions>> configureOptions)
    {
        services.AddSingleton<IReminderTable, ReminderTable>();
        configureOptions(services.AddOptions<ReminderTableOptions>());
        return services;
    }
}