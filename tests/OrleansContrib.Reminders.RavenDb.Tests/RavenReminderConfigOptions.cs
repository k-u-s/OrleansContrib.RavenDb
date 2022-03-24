using Microsoft.Extensions.Options;
using OrleansContrib.Reminders.RavenDb.Options;

namespace OrleansContrib.Reminders.RavenDb.Tests;

public class ReminderConfigOptions
{
    public static void ConfigureDefaultStoreOptionsBuilder(OptionsBuilder<ReminderTableOptions> optionsBuilder)
    {
        optionsBuilder.Configure(ConfigureDefaultStoreOptions);
    }
    
    public static void ConfigureDefaultStoreOptions(ReminderTableOptions options)
    {
        options.WaitForNonStaleMillis = StoreHolder.WaitForNonStaleMillis;
    }
}