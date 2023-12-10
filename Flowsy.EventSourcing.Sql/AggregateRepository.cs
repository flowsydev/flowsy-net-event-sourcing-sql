using Flowsy.EventSourcing.Abstractions;
using Marten;

namespace Flowsy.EventSourcing.Sql;

public class AggregateRepository : IAggregateRepository
{
    private readonly IDocumentSession _documentSession;
    private readonly IEventPublisher? _eventPublisher;
    private bool _disposed;

    public AggregateRepository(IDocumentSession documentSession, IEventPublisher? eventPublisher)
    {
        _documentSession = documentSession;
        _eventPublisher = eventPublisher;
    }

    ~AggregateRepository()
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

    public virtual async Task StoreAsync<TAggregate>(
        TAggregate aggregate,
        CancellationToken cancellationToken
        ) where TAggregate : AggregateRoot
    {
        var events = aggregate.Events.ToArray();

        if (aggregate.IsNew)
            _documentSession.Events.StartStream<TAggregate>(aggregate.Id, events.Cast<object>().ToArray());
        else
            _documentSession.Events.Append(aggregate.Id, aggregate.Version, events.Cast<object>().ToArray());
        
        await _documentSession.SaveChangesAsync(cancellationToken);
        
        _eventPublisher?.PublishAndForget(events);
        
        aggregate.Flush();
    }
    
    public virtual async Task StoreAsync<TAggregate>(
        IEnumerable<TAggregate> aggregates,
        CancellationToken cancellationToken
        ) where TAggregate : AggregateRoot
    {
        var aggregateArray = aggregates.ToArray();
        var eventEntries = aggregateArray
            .SelectMany(a => a.Events.Select(e => new
            {
                Aggregate = a,
                Event = e
            }))
            .OrderBy(entry => entry.Event.InitiationInstant)
            .ToArray();

        if (eventEntries.Length == 0)
            return;

        var startedStreams = new HashSet<AggregateRoot>();
        var unpublishedEvents = new List<IEvent>();

        foreach (var entry in eventEntries)
        {
            if (entry.Aggregate.IsNew && !startedStreams.Contains(entry.Aggregate))
            {
                _documentSession.Events.StartStream(entry.Aggregate.GetType(), entry.Aggregate.Id, entry.Event);
                startedStreams.Add(entry.Aggregate);
            }
            else
                _documentSession.Events.Append(entry.Aggregate.Id, entry.Aggregate.Version, entry.Event);

            unpublishedEvents.Add(entry.Event);
        }

        await _documentSession.SaveChangesAsync(cancellationToken);
        
        if (unpublishedEvents.Count != 0)
            _eventPublisher?.PublishAndForget(unpublishedEvents);

        foreach (var aggregate in aggregateArray)
            aggregate.Flush();
    }

    public virtual Task<TAggregate?> LoadAsync<TAggregate>(
        string id,
        CancellationToken cancellationToken
        ) where TAggregate : AggregateRoot
        => LoadAsync<TAggregate>(id, null, null, null, cancellationToken);

    public virtual Task<TAggregate?> LoadAsync<TAggregate>(
        string id,
        long? fromVersion,
        long? toVersion,
        DateTimeOffset? timestamp,
        CancellationToken cancellationToken
        ) where TAggregate : AggregateRoot
        => _documentSession.Events.AggregateStreamAsync<TAggregate>(
            id,
            version: toVersion ?? 0,
            timestamp: timestamp,
            fromVersion: fromVersion ?? 0,
            token: cancellationToken
            );
}