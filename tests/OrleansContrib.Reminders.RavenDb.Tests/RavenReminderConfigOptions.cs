using Microsoft.Extensions.Options;
using OrleansContrib.RavenDb.Tester;
using OrleansContrib.Reminders.RavenDb.Options;
using Raven.Client.Documents;

namespace OrleansContrib.Reminders.RavenDb.Tests;

public class ReminderConfigOptions
{
    public IDocumentStore DocumentStore { get; }
    
    public ReminderConfigOptions()
    {
        DocumentStore = StoreHolder.CreateDocumentStore();
    }
    
    public void ConfigureDefaultStoreOptionsBuilder(OptionsBuilder<ReminderTableOptions> optionsBuilder)
    {
        optionsBuilder.Configure(ConfigureDefaultStoreOptions);
    }
    
    public void ConfigureDefaultStoreOptions(ReminderTableOptions options)
    {
        options.WaitForNonStaleMillis = StoreHolder.WaitForNonStaleMillis;
    }
}