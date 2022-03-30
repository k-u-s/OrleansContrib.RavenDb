using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using OrleansContrib.Reminders.RavenDb.Options;
using OrleansContrib.Reminders.RavenDb.Utilities;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace OrleansContrib.Reminders.RavenDb.Reminders;

// Inspired by Orleans.Reminders.AzureStorage
public class ReminderTable : IReminderTable
{
    private readonly IGrainReferenceConverter _grainReferenceConverter;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ClusterOptions _clusterOptions;
    private readonly ReminderTableOptions _storageOptions;
    private readonly RemindersTableManager _remTableManager;
        
    public ReminderTable(
        IGrainReferenceConverter grainReferenceConverter,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IOptions<ClusterOptions> clusterOptions,
        IOptions<ReminderTableOptions> storageOptions)
    {
        _grainReferenceConverter = grainReferenceConverter;
        _logger = loggerFactory.CreateLogger<ReminderTable>();
        _loggerFactory = loggerFactory;
        _clusterOptions = clusterOptions.Value;
        _storageOptions = storageOptions.Value;
        var documentStore = _storageOptions.DocumentStoreProvider(serviceProvider);
        _remTableManager = new RemindersTableManager(
            _clusterOptions.ServiceId,
            _clusterOptions.ClusterId,
            documentStore,
            _storageOptions,
            _loggerFactory);
    }

    private ReminderTableData ConvertFromTableEntryList(List<(ReminderTableEntry Entity, string ETag)> entries)
    {
        var remEntries = new List<ReminderEntry>();
        foreach (var entry in entries)
        {
            try
            {
                var converted = ConvertFromTableEntry(entry.Entity, entry.ETag);
                remEntries.Add(converted);
            }
            catch (Exception)
            {
                // Ignoring...
            }
        }
        return new ReminderTableData(remEntries);
    }

    private ReminderEntry ConvertFromTableEntry(ReminderTableEntry tableEntry, string eTag)
    {
        try
        {
            return new ReminderEntry
            {
                GrainRef = _grainReferenceConverter.GetGrainFromKeyString(tableEntry.GrainReference),
                ReminderName = tableEntry.ReminderName,
                StartAt = LogFormatter.ParseDate(tableEntry.StartAt),
                Period = TimeSpan.Parse(tableEntry.Period),
                ETag = eTag,
            };
        }
        catch (Exception exc)
        {
            var error =
                $"Failed to parse ReminderTableEntry: {tableEntry}. This entry is corrupt, going to ignore it.";
            _logger.Error((int)ReminderErrorCode.RavenTable_49, error, exc);
            throw;
        }
        finally
        {
            var serviceIdStr = _remTableManager.ServiceId;
            if (!tableEntry.ServiceId.Equals(serviceIdStr))
            {
                var error =
                    $"Read a reminder entry for wrong Service id. Read {tableEntry}, but my service id is {serviceIdStr}. Going to discard it.";
                _logger.Warn((int)ReminderErrorCode.RavenTable_ReadWrongReminder, error);
                throw new OrleansException(error);
            }
        }
    }

    public Task TestOnlyClearTable()
    {
        return _remTableManager.DeleteTableEntries();
    }

    public Task Init() => Task.CompletedTask;

    public async Task<ReminderTableData> ReadRows(GrainReference grainRef)
    {
        try
        {
            var refKey = grainRef.ToKeyString();
            var entries = await _remTableManager.FindReminderEntries(refKey);
            var data = ConvertFromTableEntryList(entries);
            if (_logger.IsEnabled(LogLevel.Trace)) 
                _logger.Trace($"Read for grain {{0}} Table={Environment.NewLine}{{1}}", grainRef, data.ToString());
                
            return data;
        }
        catch (Exception exc)
        {
            _logger.Warn((int)ReminderErrorCode.RavenTable_47,
                $"Intermediate error reading reminders for grain {grainRef} in table {_remTableManager.TableName}.", exc);
            throw;
        }
    }

    public async Task<ReminderTableData> ReadRows(uint begin, uint end)
    {
        try
        {
            var entries = await _remTableManager.FindReminderEntries(begin, end);
            var data = ConvertFromTableEntryList(entries);
            if (_logger.IsEnabled(LogLevel.Trace)) 
                _logger.Trace($"Read in {{range}} Table={Environment.NewLine}{{data}}", RangeFactory.CreateRange(begin, end), data);
                
            return data;
        }
        catch (Exception exc)
        {
            _logger.Warn((int)ReminderErrorCode.RavenTable_40,
                $"Intermediate error reading reminders in range {RangeFactory.CreateRange(begin, end)} for table {_remTableManager.TableName}.", exc);
            throw;
        }
    }

    public async Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug)) 
                _logger.Debug("ReadRow grainRef = {0} reminderName = {1}", grainRef, reminderName);

            var grainKey = grainRef.ToKeyString();
            var result = await _remTableManager.FindReminderEntry(grainKey, reminderName);
            return result.Entity is null ? null : ConvertFromTableEntry(result.Entity, result.ETag);
        }
        catch (Exception exc)
        {
            _logger.Warn((int)ReminderErrorCode.RavenTable_46,
                $"Intermediate error reading row with grainId = {grainRef} reminderName = {reminderName} from table {_remTableManager.TableName}.", exc);
            throw;
        }
    }

    public async Task<string> UpsertRow(ReminderEntry entry)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug)) 
                _logger.Debug("UpsertRow entry = {0}", entry.ToString());
            
            var remTableEntry = new ReminderTableEntry(_remTableManager, entry);

            var result = await _remTableManager.UpsertRow(remTableEntry);
            if (result == null)
            {
                _logger.Warn((int)ReminderErrorCode.RavenTable_45,
                    $"Upsert failed on the reminder table. Will retry. Entry = {entry.ToString()}");
            }
            return result;
        }
        catch (Exception exc)
        {
            _logger.Warn((int)ReminderErrorCode.RavenTable_42,
                $"Intermediate error upserting reminder entry {entry.ToString()} to the table {_remTableManager.TableName}.", exc);
            throw;
        }
    }

    public async Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
    {
        var grainKey = grainRef.ToKeyString();
        var entry = new ReminderTableEntry
        {
            Id = ReminderTableEntry.ConstructRowKey(_storageOptions.KeyPrefix, grainKey, reminderName),
            ETag = eTag,
        };
        try
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.Trace("RemoveRow entry = {0}", entry.ToString());

            var result = await _remTableManager.DeleteReminderEntryConditionally(entry, eTag);
            if (result == false)
            {
                _logger.Warn((int)ReminderErrorCode.RavenTable_43,
                    $"Delete failed on the reminder table. Will retry. Entry = {entry}");
            }
            return result;
        }
        catch (Exception exc)
        {
            _logger.Warn((int)ReminderErrorCode.RavenTable_44,
                $"Intermediate error when deleting reminder entry {entry} to the table {_remTableManager.TableName}.", exc);
            throw;
        }
    }
}