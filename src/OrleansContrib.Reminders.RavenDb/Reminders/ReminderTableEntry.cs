using System;
using System.Text;
using Orleans;
using Orleans.Runtime;
using OrleansContrib.Reminders.RavenDb.Extensions;

namespace OrleansContrib.Reminders.RavenDb.Reminders;

// I case of introducing sharding to db GrainRefConsistentHash needs to be taken into account since it is 
// used by FindReminderEntries(uint begin, uint end) to get correct range of reminders by ReminderService
// As inspiration partitioning key from Orleans.Reminders.AzureStorage can be used     
internal class ReminderTableEntry 
{
    public string Id { get; set; }
    public string GrainReference        { get; set; }    // Part of RowKey
    public string ReminderName          { get; set; }    // Part of RowKey
    public string ServiceId             { get; set; }    // Part of PartitionKey
    public string DeploymentId          { get; set; }
    public string StartAt               { get; set; }
    public string Period                { get; set; }
    // TODO: Create issue in Raven github for invalid uint handling.
    // It used to be uint but when it was it failed to properly create linq expression:
    //  query = query.Where(e => e.GrainRefConsistentHash >= 0 && e.GrainRefConsistentHash < uint.MaxValue)
    // not always returned whole set
    public long GrainRefConsistentHash { get; set; }    // Part of PartitionKey

    public DateTimeOffset? Timestamp { get; set; }
    [Newtonsoft.Json.JsonIgnore]
    public string ETag { get; set; }

    public ReminderTableEntry() { }

    public ReminderTableEntry(RemindersTableManager rem, ReminderEntry remEntry)
    {
        var grainKey = remEntry.GrainRef.ToKeyString();
        var rowKey = ConstructRowKey(rem.TableName, grainKey, remEntry.ReminderName);

        Id = rowKey;

        ServiceId = rem.ServiceId;
        DeploymentId = rem.ClusterId;
        GrainReference = grainKey;
        ReminderName = remEntry.ReminderName;

        StartAt = LogFormatter.PrintDate(remEntry.StartAt);
        Period = remEntry.Period.ToString();

        GrainRefConsistentHash = remEntry.GrainRef.GetUniformHashCode();
        ETag = remEntry.ETag;
    }

    public static string ConstructRowKey(string keyPrefix, string grainKey, string reminderName)
    {
        var key = $"{grainKey}-{reminderName}";
        return $"{keyPrefix}/{key.Sanitize()}";
    }

    public static (string LowerBound, string UpperBound) ConstructPartitionKeyBounds(string serviceId)
    {
        var baseKey = serviceId.Sanitize();
        return ($"{baseKey}_", baseKey + (char)('_' + 1));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("Reminder [");
        sb.Append(" Id=").Append(Id);

        sb.Append(" GrainReference=").Append(GrainReference);
        sb.Append(" ReminderName=").Append(ReminderName);
        sb.Append(" Deployment=").Append(DeploymentId);
        sb.Append(" ServiceId=").Append(ServiceId);
        sb.Append(" StartAt=").Append(StartAt);
        sb.Append(" Period=").Append(Period);
        sb.Append(" GrainRefConsistentHash=").Append(GrainRefConsistentHash);
        sb.Append("]");

        return sb.ToString();
    }
}