using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;

namespace OrleansContrib.RavenDb.Tester;

public static class StoreHolder
{
    public const string DatabaseCategory = "RavenDb";
    
    public const string BaseDir = "./tmp/raven/";
    public const string DefaultDatabaseName = "OrleansEmbedded";
    public const string DebuggerServerUrl = "http://127.0.0.1:48088";
    public const long WaitForNonStaleMillis = 15_000;

    private static volatile int _dbCounter;
    private static Lazy<EmbeddedServer> CurrentInstanceLazy =
        new(CreateEmbeddedServer, LazyThreadSafetyMode.ExecutionAndPublication);
    private static EmbeddedServer CurrentInstance => CurrentInstanceLazy.Value;

    public static EmbeddedServer CreateEmbeddedServer()
    {
        var workingDir = Directory.GetCurrentDirectory(); 
        var fullBaseDir = Path.Combine(workingDir, BaseDir);
        var logsDir = Path.Combine(fullBaseDir, "./logs/");
        var dataDir = Path.Combine(fullBaseDir, "./data/");
        if (Directory.Exists(dataDir))
            Directory.Delete(dataDir, true);

        Directory.CreateDirectory(dataDir);
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);
        
        var options = new ServerOptions
        {
            LogsPath = logsDir,
            DataDirectory = dataDir
        };
        if (Debugger.IsAttached)
            options.ServerUrl = DebuggerServerUrl;
        
        EmbeddedServer.Instance.StartServer(options);
        return EmbeddedServer.Instance;
    }

    public static IDocumentStore CreateDocumentStore(IServiceProvider _)
        => CurrentInstance.GetDocumentStore(DefaultDatabaseName);

    public static string CreateNextKeyPrefix()
    {
        var nextNum = Interlocked.Add(ref _dbCounter, 1);
        var dbName = $"TestPrefix-{nextNum}";
        return dbName;
    }
}