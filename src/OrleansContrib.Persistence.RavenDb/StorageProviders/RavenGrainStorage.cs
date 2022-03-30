using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Storage;
using OrleansContrib.Persistence.RavenDb.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace OrleansContrib.Persistence.RavenDb.StorageProviders;

public class GrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly string _storageName;
    private readonly GrainStorageOptions _options;
    private readonly ClusterOptions _clusterOptions;
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<GrainStorage> _logger;

    public GrainStorage(string storageName, 
        GrainStorageOptions options, 
        IServiceProvider serviceProvider,
        IOptions<ClusterOptions> clusterOptions, 
        ILogger<GrainStorage> logger)
    {
        _storageName = storageName;
        _options = options;
        _clusterOptions = clusterOptions.Value;
        _documentStore = options.DocumentStoreProvider(serviceProvider);
        _logger = logger;
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(OptionFormattingUtilities.Name<GrainStorage>(_storageName),
            ServiceLifecycleStage.ApplicationServices,
            Init);
    }
        
    private Task Init(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var grainId = GetKeyString(grainType, grainReference);
        try
        {
            using var session = CreateSession();

            var state = await session.LoadAsync<object>(grainId);
            if (state is null)
            {
                grainState.State = Activator.CreateInstance(grainState.State.GetType());
                grainState.RecordExists = false;
                return;
            }

            grainState.State = state;
            grainState.ETag = session.Advanced.GetChangeVectorFor(state);
            grainState.RecordExists = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{{{nameof(_storageName)}}}] Grain ReadStateAsync Error for {{{nameof(grainType)}}} with id {{{nameof(grainId)}}} ", 
                _storageName, grainType, grainId);
            throw;
        }
    }

    public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var grainId = GetKeyString(grainType, grainReference);
        try
        {
            using var session = CreateSession();

            var eTag = grainState.ETag;
            var state = grainState.State;
            if (string.IsNullOrEmpty(eTag))
            {
                // TODO: Verify if it can be done in one call vis StoreAsync (eq. passing string.Empty ?)
                var exists = await session.Advanced.ExistsAsync(grainId);
                if (exists)
                    throw new InconsistentStateException("Expected for state to not exists");
            }
            
            await session.StoreAsync(state, eTag, grainId);
            await _options.OnSaving(session, grainId, state);
            await session.SaveChangesAsync();
            grainState.ETag = session.Advanced.GetChangeVectorFor(state);
            grainState.RecordExists = true;
            await _options.OnSaved(session, grainId, state);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, $"[{{{nameof(_storageName)}}}] Grain WriteStateAsync Error for {{{nameof(grainType)}}} with id {{{nameof(grainId)}}} ", 
                _storageName, grainType, grainId);
            throw new InconsistentStateException(ex.ExpectedChangeVector, ex.ActualChangeVector, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{{{nameof(_storageName)}}}] Grain WriteStateAsync Error for {{{nameof(grainType)}}} with id {{{nameof(grainId)}}} ", 
                _storageName, grainType, grainId);
            throw;
        }
    }

    public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var grainId = GetKeyString(grainType, grainReference);
        try
        {
            using var session = CreateSession();
            var state = await session.LoadAsync<object>(grainId);

            if (state is null)
                return;
            
            if(grainState.ETag != session.Advanced.GetChangeVectorFor(state))
                throw new InconsistentStateException($"Version conflict (ClearState): ServiceId={_clusterOptions.ServiceId} ProviderName={_storageName} GrainType={grainType} GrainReference={grainReference.ToKeyString()}.");

            session.Delete(grainId, grainState.ETag);
            await _options.OnDeleting(session, grainId, state);
            await session.SaveChangesAsync();
            grainState.ETag = default;
            grainState.State = Activator.CreateInstance(grainState.State.GetType());
            grainState.RecordExists = false;
            await _options.OnDeleted(session, grainId, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{{{nameof(_storageName)}}}] Grain ClearStateAsync Error for {{{nameof(grainType)}}} with id {{{nameof(grainId)}}} ", 
                _storageName, grainType, grainId);
            throw;
        }
    }
        
    private string GetKeyString(string grainType, GrainReference grainReference)
    {
        const string separator = ".";
        const string prefixSeparator = "/";
        
        return string.IsNullOrWhiteSpace(_options.KeyPrefix)
            ? $"{_clusterOptions.ServiceId}{separator}{grainReference.ToKeyString()}{separator}{grainType}"
            : $"{_options.KeyPrefix}{prefixSeparator}{_clusterOptions.ServiceId}{separator}{grainReference.ToKeyString()}{separator}{grainType}";
    }
    
    private IAsyncDocumentSession CreateSession() 
        => string.IsNullOrEmpty(_options.DatabaseName)
            ? _documentStore.OpenAsyncSession()
            : _documentStore.OpenAsyncSession(_options.DatabaseName);
}