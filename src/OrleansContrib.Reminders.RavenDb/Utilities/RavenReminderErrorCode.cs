namespace OrleansContrib.Reminders.RavenDb.Utilities;

internal enum ReminderErrorCode
{
    RavenRuntime = 100000,
    RavenTableBase = RavenRuntime + 2800,

    // reminders related
    RavenTable_38 = RavenTableBase + 38,
    RavenTable_39 = RavenTableBase + 39,
    RavenTable_40 = RavenTableBase + 40,
    RavenTable_42 = RavenTableBase + 42,
    RavenTable_43 = RavenTableBase + 43,
    RavenTable_44 = RavenTableBase + 44,
    RavenTable_45 = RavenTableBase + 45,
    RavenTable_46 = RavenTableBase + 46,
    RavenTable_47 = RavenTableBase + 47,
    RavenTable_49 = RavenTableBase + 49,

    RavenTable_ReadWrongReminder = RavenTableBase + 64
}