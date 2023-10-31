using System.Text.Json;
using Flowsy.EventSourcing.Abstractions;

namespace Flowsy.EventSourcing.Sql;

internal class DbEventRecordManagerV1 : DbEventRecordManager
{
    internal DbEventRecordManagerV1()
    {
    }

    public override string Version => "1.0";

    public override EventMetadata GetMetadata(object @event)
    {
        var type = @event.GetType();
        return new EventMetadata(Version, type.Name, type.FullName!);
    }

    public override EventMetadata? GetMetadata(string json, JsonSerializerOptions? options = null)
        => JsonSerializer.Deserialize<EventMetadata>(json, options);

    public override TPayload GetPayload<TPayload>(string json, EventMetadata metadata, JsonSerializerOptions? options = null)
    {
        var type = Type.GetType(metadata.FullyQualifiedName);
        if (type is null)
            throw new InvalidOperationException(string.Format(Resources.Strings.TypeNotFound, metadata.FullyQualifiedName));
        
        var obj = JsonSerializer.Deserialize(json, type, options);
        if (obj is null)
            throw new InvalidOperationException(Resources.Strings.CouldNotReadEventPayload);

        return (TPayload) obj;
    }
}