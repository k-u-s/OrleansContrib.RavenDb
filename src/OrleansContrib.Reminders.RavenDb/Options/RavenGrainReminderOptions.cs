namespace OrleansContrib.Reminders.RavenDb.Options;

public class ReminderTableOptions
{
    private const string DefaultKeyPrefix = "OrleansReminder";

    public string KeyPrefix { get; set; } = DefaultKeyPrefix;
    public long? WaitForNonStaleMillis { get; set; }
}