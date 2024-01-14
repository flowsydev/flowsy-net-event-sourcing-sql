using Flowsy.EventSourcing.Abstractions;
using Flowsy.EventSourcing.Sql.Resources;
using Marten;

namespace Flowsy.EventSourcing.Sql;

public class EventRepository : IEventRepository
{
    private readonly IDocumentSession _documentSession;
    private readonly IEventPublisher? _eventPublisher;
    private bool _deferringPersistence;
    private readonly List<IEventSource> _deferredEventSources = [];
    private bool _disposed;

    public EventRepository(IDocumentSession documentSession)
    {
        _documentSession = documentSession;
    }
    
    public EventRepository(IDocumentSession documentSession, IEventPublisher eventPublisher)
    {
        _documentSession = documentSession;
        _eventPublisher = eventPublisher;
    }

    ~EventRepository()
    {
        Dispose(false);
    }
    

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _documentSession.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _documentSession.DisposeAsync();
        Dispose(false);
        GC.SuppressFinalize(this);
    }
    
    public void BeginPersistence()
    {
        if (_deferringPersistence)
            throw new InvalidOperationException(Strings.PersistenceOperationActiveMustBeCompleted);
        
        _deferringPersistence = true;
    }
    
    public async Task StoreAsync<TEventSource>(
        TEventSource eventSource,
        CancellationToken cancellationToken
        ) where TEventSource : class, IEventSource
    {
        var events = eventSource.Events.Cast<object>().ToArray();

        if (eventSource.IsNew)
            _documentSession.Events.StartStream<TEventSource>(eventSource.Id, events);
        else
            _documentSession.Events.Append(eventSource.Id, eventSource.Version, events);

        if (_deferringPersistence)
        {
            _deferredEventSources.Add(eventSource);
            return;
        }
        
        await _documentSession.SaveChangesAsync(cancellationToken);
        _eventPublisher?.PublishAndForget(eventSource);
        eventSource.Flush();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (!_deferringPersistence)
            throw new InvalidOperationException(Strings.PersistenceOperationMustBeStartedAndEventsMustBeStored);
        
        if (_deferredEventSources.Count == 0)
        {
            _deferringPersistence = false;
            return;
        }
        
        await _documentSession.SaveChangesAsync(cancellationToken);
        
        var events = _deferredEventSources
            .SelectMany(es => es.Events)
            .OrderBy(e => e.InitiationInstant);
        
        _eventPublisher?.PublishAndForget(events);
        
        foreach (var eventSource in _deferredEventSources)
            eventSource.Flush();
        
        _deferredEventSources.Clear();
        _deferringPersistence = false;
    }

    public virtual Task<TEventSource?> LoadAsync<TEventSource>(
        string id,
        CancellationToken cancellationToken
        ) where TEventSource : class, IEventSource
        => LoadAsync<TEventSource>(id, null, null, null, cancellationToken);

    public virtual Task<TEventSource?> LoadAsync<TEventSource>(
        string id,
        Action<TEventSource> configure,
        CancellationToken cancellationToken
        ) where TEventSource : class, IEventSource
        => LoadAsync(id, null, null, null, configure, cancellationToken);

    public virtual Task<TEventSource?> LoadAsync<TEventSource>(
        string id,
        long? fromVersion = null,
        long? toVersion = null,
        DateTimeOffset? timestamp = null,
        CancellationToken cancellationToken = default
        ) where TEventSource : class, IEventSource
        => _documentSession.Events.AggregateStreamAsync<TEventSource>(
            id,
            version: toVersion ?? 0,
            timestamp: timestamp,
            fromVersion: fromVersion ?? 0,
            token: cancellationToken
            );

    public virtual async Task<TEventSource?> LoadAsync<TEventSource>(
        string id,
        long? fromVersion = null,
        long? toVersion = null,
        DateTimeOffset? timestamp = null,
        Action<TEventSource>? configure = null,
        CancellationToken cancellationToken = default
        ) where TEventSource : class, IEventSource
    {
        var eventSource = await _documentSession.Events.AggregateStreamAsync<TEventSource>(
            id,
            version: toVersion ?? 0,
            timestamp: timestamp,
            fromVersion: fromVersion ?? 0,
            token: cancellationToken
            );
        
        if (eventSource is not null && configure is not null)
            configure(eventSource);
        
        return eventSource;
    }
}