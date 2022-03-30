using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrleansContrib.Reminders.RavenDb.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;

namespace OrleansContrib.Reminders.RavenDb.Reminders;

internal class RemindersTableManager
{
    private readonly IDocumentStore _documentStore;
    private readonly ReminderTableOptions _options;
    public string ServiceId { get; }
    public string ClusterId { get; }
    public ILogger<ReminderTable> Logger { get; }
    public string TableName => _options.KeyPrefix;
        
    public RemindersTableManager(string serviceId,
        string clusterId,
        IDocumentStore documentStore,
        ReminderTableOptions options,
        ILoggerFactory loggerFactory)
    {
        ClusterId = clusterId;
        ServiceId = serviceId;
        Logger = loggerFactory.CreateLogger<ReminderTable>();
        _documentStore = documentStore;
        _options = options;
    }

    internal async Task<List<(ReminderTableEntry Entity, string ETag)>> FindReminderEntries(long begin, long end)
    {
        using var session = CreateSession();
        var baseQuery = session.Query<ReminderTableEntry>()
            .Statistics(out var stats);
        if (_options.WaitForNonStaleMillis.HasValue)
            baseQuery = baseQuery.Customize(
                x => x.WaitForNonStaleResults(
                        TimeSpan.FromMilliseconds(_options.WaitForNonStaleMillis.Value)));
            
        var query = baseQuery.Where(el => el.ServiceId == ServiceId);

        if (begin < end)
            query = query.Where(e => e.GrainRefConsistentHash > begin && e.GrainRefConsistentHash <= end);
        else if(begin != end)
            // For begin == end
            // Query the entire range so in this case do nothing
            // For (begin > end)
            // Query wraps around the ends of the range, so the query is the union of two disjunct queries
            // Include everything outside of the (begin, end] range, which wraps around to become:
            //  [partitionKeyLowerBound, end] OR (begin, partitionKeyUpperBound]
            query = query.Where(e => e.GrainRefConsistentHash > begin || e.GrainRefConsistentHash <= end);
            
        var queryEntriesResults = await query.ToListAsync();
        if(stats.IsStale)
            Logger.LogDebug($"Using stale data for range {{{nameof(begin)}}} to {{{nameof(end)}}} reminders {{@{nameof(stats)}}}", 
                begin , end, stats);
        
        var queryResults = queryEntriesResults.Select(e => (e, session.Advanced.GetChangeVectorFor(e))).ToList();
        return queryResults;
    }

    internal async Task<List<(ReminderTableEntry Entity, string ETag)>> FindReminderEntries(string grainKey)
    {
        using var session = CreateSession();
        var baseQuery = session.Query<ReminderTableEntry>()
                .Statistics(out var stats)
            ;
        if (_options.WaitForNonStaleMillis.HasValue)
            baseQuery = baseQuery.Customize(
                x => x.WaitForNonStaleResults(
                    TimeSpan.FromMilliseconds(_options.WaitForNonStaleMillis.Value)));
            
        var query = baseQuery
            .Where(el => el.ServiceId == ServiceId)
            .Where(el => el.GrainReference == grainKey);
            
        var queryResults = await query.ToListAsync();
        if(stats.IsStale)
            Logger.LogDebug($"Using stale data for {{{nameof(grainKey)}}} reminder {{@{nameof(stats)}}}", 
                grainKey, stats);

        return queryResults.Select(e => (e, session.Advanced.GetChangeVectorFor(e))).ToList();
    }

    internal async Task<(ReminderTableEntry Entity, string ETag)> FindReminderEntry(string grainKey, string reminderName)
    {
        var rowKey = ReminderTableEntry.ConstructRowKey(_options.KeyPrefix, grainKey, reminderName);
        using var session = CreateSession();
        var result = await session.LoadAsync<ReminderTableEntry>(rowKey);
        if (result is null)
            return (null, string.Empty);
        
        return (result, session.Advanced.GetChangeVectorFor(result));
    }

    internal async Task<string> UpsertRow(ReminderTableEntry reminderEntry)
    {
        using var session = CreateSession();
            
        await session.StoreAsync(reminderEntry);
        await session.SaveChangesAsync();
            
        var etag = session.Advanced.GetChangeVectorFor(reminderEntry);
        return etag;
    }

    internal async Task<bool> DeleteReminderEntryConditionally(ReminderTableEntry reminderEntry, string eTag)
    {
        using var session = CreateSession();
        var exist = await session.Advanced.ExistsAsync(reminderEntry.Id);
        if (!exist)
            return false;

        var entry = await session.LoadAsync<ReminderTableEntry>(reminderEntry.Id);
        var currentEtag = session.Advanced.GetChangeVectorFor(entry);
        if (currentEtag != eTag)
            return false;
        
        session.Delete(reminderEntry.Id, eTag);
        await session.SaveChangesAsync();
            
        return true;
    }

    internal async Task DeleteTableEntries()
    {
        var collectionName = _documentStore.Conventions.GetCollectionName(typeof(ReminderTableEntry));
        var databaseName = _documentStore.Database;
            
        await _documentStore.Operations
            .ForDatabase(databaseName)
            .SendAsync(new DeleteByQueryOperation($"from {collectionName}"));
    }
    
    private IAsyncDocumentSession CreateSession() 
        => string.IsNullOrEmpty(_options.DatabaseName)
            ? _documentStore.OpenAsyncSession()
            : _documentStore.OpenAsyncSession(_options.DatabaseName);
}