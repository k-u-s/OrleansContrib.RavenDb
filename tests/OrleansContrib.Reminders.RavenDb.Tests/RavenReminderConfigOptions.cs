using Microsoft.Extensions.Options;
using OrleansContrib.RavenDb.Tester;
using OrleansContrib.Reminders.RavenDb.Options;

namespace OrleansContrib.Reminders.RavenDb.Tests;

public class ReminderConfigOptions
{
    private string _keyPrefix;
    
    public ReminderConfigOptions()
    {
        _keyPrefix = StoreHolder.CreateNextKeyPrefix();
    }
    
    public void ConfigureDefaultStoreOptionsBuilder(OptionsBuilder<ReminderTableOptions> optionsBuilder)
    {
        optionsBuilder.Configure(ConfigureDefaultStoreOptions);
    }
    
    public void ConfigureDefaultStoreOptions(ReminderTableOptions options)
    {
        options.KeyPrefix = StoreHolder.CreateNextKeyPrefix();
        options.WaitForNonStaleMillis = StoreHolder.WaitForNonStaleMillis;
    }
}