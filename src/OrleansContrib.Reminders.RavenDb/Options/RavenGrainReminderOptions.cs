using System;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace OrleansContrib.Reminders.RavenDb.Options;

public class ReminderTableOptions
{
    private const string DefaultKeyPrefix = "OrleansReminder";

    public string KeyPrefix { get; set; } = DefaultKeyPrefix;
    public string DatabaseName { get; set; } 
    public long? WaitForNonStaleMillis { get; set; }
    
    public Func<IServiceProvider, IDocumentStore> DocumentStoreProvider { get; set; } = DefaultProvider;

    private static IDocumentStore DefaultProvider(IServiceProvider sp) => sp.GetRequiredService<IDocumentStore>();
}