using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace OrleansContrib.Persistence.RavenDb.Options;

public class GrainStorageOptions
{
    private const string DefaultKeyPrefix = "GrainStorage";

    public string KeyPrefix { get; set; } = DefaultKeyPrefix;
    public string DatabaseName { get; set; } 

    public Func<IServiceProvider, IDocumentStore> DocumentStoreProvider { get; set; } = DefaultProvider;

    public Func<IAsyncDocumentSession, string, object, Task> OnSaving { get; set; } = DefaultEvent;
    public Func<IAsyncDocumentSession, string, object, Task> OnSaved { get; set; } = DefaultEvent;
    public Func<IAsyncDocumentSession, string, object, Task> OnDeleting { get; set; } = DefaultEvent;
    public Func<IAsyncDocumentSession, string, object, Task> OnDeleted { get; set; } = DefaultEvent;

    private static Task DefaultEvent(IAsyncDocumentSession s, string id, object o) => Task.CompletedTask;
    private static IDocumentStore DefaultProvider(IServiceProvider sp) => sp.GetRequiredService<IDocumentStore>();
}