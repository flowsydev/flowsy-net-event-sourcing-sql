using System.Text.Json;
using Flowsy.EventSourcing.Abstractions;

namespace Flowsy.EventSourcing.Sql;

internal abstract class DbEventRecordManager
{
    private static DbEventRecordManagerV1? _v1;
    
    protected DbEventRecordManager()
    {
    }
    
    public abstract string Version { get; }
    public abstract EventMetadata GetMetadata(object @event);
    public abstract EventMetadata? GetMetadata(string json, JsonSerializerOptions? options = null);
    public abstract TPayload GetPayload<TPayload>(string json, EventMetadata metadata, JsonSerializerOptions? options = null);

    public static DbEventRecordManager GetInstance(string version = "1.0")
        => version switch
        {
            "1.0" => _v1 ??= new DbEventRecordManagerV1(),
            _ => throw new NotSupportedException(string.Format(Resources.Strings.EventRecordVersionNotSupported, version))
        };
}