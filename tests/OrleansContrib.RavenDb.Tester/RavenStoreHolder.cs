using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Raven.Client.Documents;
using Raven.Embedded;

namespace OrleansContrib.Reminders.RavenDb.Tests;

public static class StoreHolder
{
    public const string DatabaseCategory = "RavenDb";
    
    public const string BaseDir = "./tmp/raven/";
    public const string DefaultDatabaseName = "OrleansEmbedded";
    public const string DebuggerServerUrl = "http://127.0.0.1:48088";
    public const long WaitForNonStaleMillis = 10_000;
    
    private static IDocumentStore CreateDocumentStore()
    {
        var workingDir = Directory.GetCurrentDirectory(); 
        var fullBaseDir = Path.Combine(workingDir, BaseDir);
        var options = new ServerOptions
        {
            LogsPath = Path.Combine(fullBaseDir, "./logs/"),
            DataDirectory = Path.Combine(fullBaseDir, "./data/")
        };
        if (Debugger.IsAttached)
            options.ServerUrl = DebuggerServerUrl;
        
        EmbeddedServer.Instance.StartServer();
        return EmbeddedServer.Instance.GetDocumentStore(DefaultDatabaseName);
    }

    private static readonly Lazy<IDocumentStore> DocumentStoreLazy =
        new(CreateDocumentStore, LazyThreadSafetyMode.ExecutionAndPublication);
    
    public static IDocumentStore DocumentStore => DocumentStoreLazy.Value;
}