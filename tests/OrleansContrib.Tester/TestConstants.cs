using System;
using System.Diagnostics;

namespace OrleansContrib.Tester;

// used for test constants
public static class TestConstants
{
    public const string DefaultCollection = "DefaultTestEnvironment";
    
    public const string StorageProviderDefault = "DefaultStore";
    public const string StorageProviderMemory = "MemoryStore";
    public const string StorageProviderMissing = "MissingProvider";
    public const string StorageProviderErrorInjector = "ErrorInjector";
    public const string StorageProviderForTest = "GrainStorageForTest";

    public static readonly TimeSpan InitTimeout =
        Debugger.IsAttached ? TimeSpan.FromMinutes(3) : TimeSpan.FromMinutes(1);

    public static TimeSpan CreationTimeout = 
        Debugger.IsAttached ? TimeSpan.FromMinutes(2) : TimeSpan.FromMilliseconds(5_000);
    
    public static class Category
    {
        public const string Persistence = "GrainStorage";
        public const string Reminders = "Reminders";
    }
}