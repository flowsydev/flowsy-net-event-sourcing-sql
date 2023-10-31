using System.Data;
using System.Data.Common;
using System.Text.Json;
using Dapper;
using Flowsy.Db.Abstractions;
using Flowsy.EventSourcing.Abstractions;

namespace Flowsy.EventSourcing.Sql;

public abstract class DbEventStore<TEvent> : IEventStore<TEvent>
    where TEvent : IEvent
{
    private readonly IDbConnection _dbConnection;
    private readonly IDbTransaction? _dbTransaction;
    private bool _disposed;

    protected DbEventStore(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnection = dbConnectionFactory.GetConnection(Configuration.ConnectionKey);
    }
    
    protected DbEventStore(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    protected DbEventStore(IDbTransaction dbTransaction)
    {
        _dbTransaction = dbTransaction;
        _dbConnection = dbTransaction.Connection;
    }

    ~DbEventStore()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing && _dbTransaction is null)
        {
            _dbConnection.Dispose();
        }

        _disposed = true;
    }

    protected virtual async Task DisposeAsync(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing && _dbTransaction is null)
        {
            if (_dbConnection is DbConnection dbConnection)
                await dbConnection.DisposeAsync();
            else
                _dbConnection.Dispose();
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
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected DbEventStoreConfiguration Configuration
        => DbEventStoreConfiguration.GetForType(GetType());

    protected virtual string SerializeJson<TValue>(TValue value)
        => JsonSerializer.Serialize(value, Configuration.JsonSerializerOptions);
    
    public virtual Task SaveAsync(string key, TEvent @event, CancellationToken cancellationToken)
        => SaveAsync(key, @event, null, cancellationToken);

    public virtual async Task SaveAsync(string key, TEvent @event, string? correlationId, CancellationToken cancellationToken)
    {
        var config = Configuration;
        var table = config.TargetTableFqn;
        var idColumn = config.IdColumn;
        var keyColumn = config.KeyColumn;
        var typeColumn = config.TypeColumn;
        var payloadColumn = config.PayloadColumn;
        var metadataColumn = config.MetadataColumn;
        var timestampColumn = config.TimestampColumn;
        var versionColumn = config.VersionColumn;
        var correlationIdColumn = config.CorrelationIdColumn;

        var columns = string.Join(
            ", ",
            idColumn, keyColumn, typeColumn, 
            payloadColumn, metadataColumn, 
            timestampColumn, versionColumn, correlationIdColumn
            );
        const string values = "default, @Key, @Type, @Payload, @Metadata, default, @Version, @CorrelationId";

        var recordManager = DbEventRecordManager.GetInstance();
        var metadata = recordManager.GetMetadata(@event);

        await _dbConnection.ExecuteAsync(
            $"insert into {table} ({columns}) values ({values})", 
            new
            {
                Key = key,
                Type = metadata.EventType,
                Payload = SerializeJson(@event),
                Metadata = SerializeJson(metadata),
                Version = recordManager.Version,
                CorrelationId = correlationId
            }, 
            commandType: CommandType.Text
            );
    }

    public virtual Task SaveAsync(string key, IEnumerable<TEvent> events, CancellationToken cancellationToken)
        => SaveAsync(key, events, null, cancellationToken);

    public virtual async Task SaveAsync(string key, IEnumerable<TEvent> events, string? correlationId, CancellationToken cancellationToken)
    {
        foreach (var e in events)
            await SaveAsync(key, e, correlationId, cancellationToken);
    }

    public virtual async Task<IEnumerable<TEvent>> LoadEventsAsync(string key, CancellationToken cancellationToken)
    {
        var records = await LoadRecordsAsync(key, cancellationToken);
        return records.Select(record => record.Payload);
    }

    public virtual async Task<IEnumerable<EventRecord<TEvent>>> LoadRecordsAsync(string key, CancellationToken cancellationToken)
    {
        var config = Configuration;
        var table = config.TargetTableFqn;
        var idColumn = config.IdColumn;
        var keyColumn = config.KeyColumn;
        var typeColumn = config.TypeColumn;
        var payloadColumn = config.PayloadColumn;
        var metadataColumn = config.MetadataColumn;
        var timestampColumn = config.TimestampColumn;
        var versionColumn = config.VersionColumn;
        var correlationIdColumn = config.CorrelationIdColumn;
        var columns = string.Join(
            ", ",
            idColumn, typeColumn, 
            payloadColumn, metadataColumn, 
            timestampColumn, versionColumn, correlationIdColumn
        );
        
        var records = await _dbConnection.QueryAsync<IDictionary<string, object>>(
            $"select {columns} from {table} where {keyColumn} = @Key order by {idColumn}",
            new
            {
                Key = key
            },
            commandType: CommandType.Text
        );

        var eventRecords = new List<EventRecord<TEvent>>();

        foreach (var r in records)
        {
            var version = r[versionColumn].ToString();
            if (version is null)
                throw new InvalidOperationException(Resources.Strings.EventRecordVersionNotSpecified);
            
            var metadataJson = r[metadataColumn].ToString();
            if (metadataJson is null)
                throw new InvalidOperationException(Resources.Strings.EventMetadataNotFound);

            var timestampRaw = r[timestampColumn];
            if (timestampRaw is not DateTime timestamp)
                throw new InvalidOperationException(string.Format(Resources.Strings.InvalidEventTimestamp, timestampRaw));
                
            var recordManager = DbEventRecordManager.GetInstance(version);
            
            var metadata = recordManager.GetMetadata(metadataJson, config.JsonSerializerOptions);
            if (metadata is null)
                throw new InvalidOperationException(Resources.Strings.CouldNotReadEventMetadata);
            
            var payloadJson = r[payloadColumn].ToString();
            if (payloadJson is null)
                throw new InvalidOperationException(Resources.Strings.EventPayloadNotFound);

            var payload = recordManager.GetPayload<TEvent>(payloadJson, metadata, config.JsonSerializerOptions);

            var id = r[idColumn];
            
            eventRecords.Add(new EventRecord<TEvent>(
                id switch
                {
                    long l => l,
                    int i => i,
                    short s => s,
                    _ => 0
                },
                key,
                metadata.EventType,
                payload,
                metadata,
                timestamp,
                version,
                r[correlationIdColumn].ToString()
            ));
        }
        
        return eventRecords;
    }
}