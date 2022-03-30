using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Orleans;
using Orleans.Internal;
using Orleans.Runtime;
using OrleansContrib.Tester.Internals;
using OrleansContrib.Tester.Reminders.Grains;
using Xunit;

namespace OrleansContrib.Tester.Reminders.Runners;

public class BaseReminderTableTestsRunner
{
    private readonly BaseReminderTestClusterFixture clusterFixture;
    private readonly IReminderTable remindersTable;

    public BaseReminderTableTestsRunner(IReminderTable remindersTable, BaseReminderTestClusterFixture clusterFixture)
    {
        this.clusterFixture = clusterFixture;
        this.remindersTable = remindersTable;
    }

    public async Task RemindersParallelUpsert()
    {
        var upserts = await Task.WhenAll(Enumerable.Range(0, 5).Select(i =>
        {
            var reminder = CreateReminder(MakeTestGrainReference(), i.ToString());
            return Task.WhenAll(Enumerable.Range(1, 5).Select(j =>
            {
                return RetryHelper.RetryOnExceptionAsync<string>(5, RetryOperation.Sigmoid,
                    async () => { return await remindersTable.UpsertRow(reminder); });
            }));
        }));
        Assert.DoesNotContain(upserts, i => i.Distinct().Count() != 5);
    }

    public async Task ReminderSimple()
    {
        var reminder = CreateReminder(MakeTestGrainReference(), "foo/bar\\#b_a_z?");
        await remindersTable.UpsertRow(reminder);

        var readReminder = await remindersTable.ReadRow(reminder.GrainRef, reminder.ReminderName);

        var etagTemp = reminder.ETag = readReminder.ETag;

        Assert.Equal(JsonConvert.SerializeObject(readReminder), JsonConvert.SerializeObject(reminder));

        Assert.NotNull(etagTemp);

        reminder.ETag = await remindersTable.UpsertRow(reminder);

        var removeRowRes = await remindersTable.RemoveRow(reminder.GrainRef, reminder.ReminderName, etagTemp);
        Assert.False(removeRowRes, "should have failed. Etag is wrong");
        removeRowRes = await remindersTable.RemoveRow(reminder.GrainRef, "bla", reminder.ETag);
        Assert.False(removeRowRes, "should have failed. reminder name is wrong");
        removeRowRes = await remindersTable.RemoveRow(reminder.GrainRef, reminder.ReminderName, reminder.ETag);
        Assert.True(removeRowRes, "should have succeeded. Etag is right");
        removeRowRes = await remindersTable.RemoveRow(reminder.GrainRef, reminder.ReminderName, reminder.ETag);
        Assert.False(removeRowRes, "should have failed. reminder shouldn't exist");
    }

    public async Task RemindersRange(int iterations = 1000)
    {
        var parallelOptions = RetryHelper.CreateDefaultParallelOptions();
        var eTags = new ConcurrentBag<string>();
        await Parallel.ForEachAsync(
            Enumerable.Range(1, iterations), 
            parallelOptions, 
            async (i, token) =>
            {
                var grainRef = MakeTestGrainReference();
                var reminder = CreateReminder(grainRef, i.ToString());

                var etag = await RetryHelper.RetryOnExceptionAsync(10, RetryOperation.Sigmoid, 
                    async () => await remindersTable.UpsertRow(reminder), token);
                Assert.NotEmpty(etag);
                eTags.Add(etag);
            });
        
        Assert.Equal(iterations, eTags.Count);

        var rows = await remindersTable.ReadRows(0, uint.MaxValue);
        var reminderRowCount = rows.Reminders.Count;
        
        Assert.Equal(reminderRowCount, iterations);

        // Test range that should return all
        rows = await remindersTable.ReadRows(0, 0);
        reminderRowCount = rows.Reminders.Count;
        
        Assert.Equal(reminderRowCount, iterations);

        var remindersHashes = rows.Reminders.Select(r => r.GrainRef.GetUniformHashCode()).ToArray();
        var random = new SafeRandom();

        var hash = remindersHashes[remindersHashes.Length / 2];
        
        // Test range that should return exactly one value
        await TestRemindersHashInterval(remindersTable, hash, hash + 1, remindersHashes);
        
        // Test range that should return all except one value
        await TestRemindersHashInterval(remindersTable, hash + 1, hash, remindersHashes);
        
        await Task.WhenAll(
            Enumerable.Range(0, iterations)
                .Select(i =>
                    TestRemindersHashInterval(remindersTable, (uint)random.Next(), (uint)random.Next(),
                        remindersHashes)));
    }

    public async Task TestRemindersHashInterval(IReminderTable reminderTable, uint beginHash, uint endHash,
        uint[] remindersHashes)
    {
        var rowsTask = reminderTable.ReadRows(beginHash, endHash);
        var expectedHashes = beginHash < endHash
            ? remindersHashes.Where(r => r > beginHash && r <= endHash)
            : remindersHashes.Where(r => r > beginHash || r <= endHash);

        var expectedSet = new HashSet<uint>(expectedHashes);
        var rows = await rowsTask;
        
        Assert.NotNull(rows);
        
        var returnedHashes = rows.Reminders.Select(r => r.GrainRef.GetUniformHashCode());
        var returnedSet = new HashSet<uint>(returnedHashes);

        Assert.Equal(expectedSet.Count, returnedSet.Count);
        var areSame = returnedSet.SetEquals(expectedSet);
        Assert.True(areSame);
    }
    
    public async Task VerifyHash(ReminderTableData rows, uint beginHash, uint endHash,
        uint[] remindersHashes)
    {
        var expectedHashes = beginHash < endHash
            ? remindersHashes.Where(r => r > beginHash && r <= endHash)
            : remindersHashes.Where(r => r > beginHash || r <= endHash);

        var expectedSet = new HashSet<uint>(expectedHashes);
        
        Assert.NotNull(rows);
        
        var returnedHashes = rows.Reminders.Select(r => r.GrainRef.GetUniformHashCode());
        var returnedSet = new HashSet<uint>(returnedHashes);

        Assert.Equal(expectedSet.Count, returnedSet.Count);
        var areSame = returnedSet.SetEquals(expectedSet);
        Assert.True(areSame);
    }
    
    

    private static ReminderEntry CreateReminder(GrainReference grainRef, string reminderName)
    {
        var now = DateTime.UtcNow;
        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        return new ReminderEntry
        {
            GrainRef = grainRef,
            Period = TimeSpan.FromMinutes(1),
            StartAt = now,
            ReminderName = reminderName
        };
    }

    private GrainReference MakeTestGrainReference()
    {
        var grainKey = Guid.NewGuid();
        var grain = clusterFixture.Client.GetGrain<IReminderTestGrain>(grainKey);
        return (GrainReference)grain;
    }
}