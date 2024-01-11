using Flowsy.EventSourcing.Abstractions;
using Marten;

namespace Flowsy.EventSourcing.Sql;

public class EventRepository : IEventRepository
{
    private readonly IDocumentSession _documentSession;
    private readonly IEventPublisher? _eventPublisher;
    private bool _disposed;

    public EventRepository(IDocumentSession documentSession, IEventPublisher? eventPublisher)
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

    public virtual async Task StoreAsync<TEventSource>(
        TEventSource eventSource,
        CancellationToken cancellationToken
        ) where TEventSource : class, IEventSource
    {
        var events = eventSource.Events.Cast<object>().ToArray();

        if (eventSource.IsNew)
            _documentSession.Events.StartStream<TEventSource>(eventSource.Id, events);
        else
            _documentSession.Events.Append(eventSource.Id, eventSource.Version, events);
        
        await _documentSession.SaveChangesAsync(cancellationToken);
        
        _eventPublisher?.PublishAndForget(eventSource);
        
        eventSource.Flush();
    }

    public virtual Task<TEventSource?> LoadAsync<TEventSource>(
        string id,
        CancellationToken cancellationToken
        ) where TEventSource : class, IEventSource
        => LoadAsync<TEventSource>(id, null, null, null, cancellationToken);

    public virtual Task<TEventSource?> LoadAsync<TEventSource>(
        string id,
        long? fromVersion,
        long? toVersion,
        DateTimeOffset? timestamp,
        CancellationToken cancellationToken
        ) where TEventSource : class, IEventSource
        => _documentSession.Events.AggregateStreamAsync<TEventSource>(
            id,
            version: toVersion ?? 0,
            timestamp: timestamp,
            fromVersion: fromVersion ?? 0,
            token: cancellationToken
            );
}