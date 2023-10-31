using System.Text.Json;

namespace Flowsy.EventSourcing.Sql;

public sealed class DbEventStoreConfiguration
{
    public DbEventStoreConfiguration(
        string? connectionKey = null,
        string? targetSchema = null,
        string targetTable = "event",
        string idColumn = "id",
        string keyColumn = "key",
        string typeColumn = "type",
        string payloadColumn = "payload",
        string metadataColumn = "metadata",
        string timestampColumn = "timestamp",
        string versionColumn = "version",
        string correlationIdColumn = "correlationId",
        JsonSerializerOptions? jsonSerializerOptions = null 
    )
    {
        ConnectionKey = connectionKey;
        TargetSchema = targetSchema;
        TargetTable = targetTable;
        IdColumn = idColumn;
        KeyColumn = keyColumn;
        TypeColumn = typeColumn;
        PayloadColumn = payloadColumn;
        MetadataColumn = metadataColumn;
        TimestampColumn = timestampColumn;
        VersionColumn = versionColumn;
        CorrelationIdColumn = correlationIdColumn;
        JsonSerializerOptions = jsonSerializerOptions;
    }

    public string? ConnectionKey { get; set; }
    public string? TargetSchema { get; set; }
    public string TargetTable { get; set; }
    public string TargetTableFqn => $"{(TargetSchema is not null ? TargetSchema + "." : string.Empty)}{TargetTable}";
    public string IdColumn { get; set; }
    public string KeyColumn { get; set; }
    public string TypeColumn { get; set; }
    public string PayloadColumn { get; set; }
    public string MetadataColumn { get; set; }
    public string TimestampColumn { get; set; }
    public string VersionColumn { get; set; }
    public string CorrelationIdColumn { get; set; }
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public DbEventStoreConfiguration Clone()
        => new(
            ConnectionKey,
            TargetSchema,
            TargetTable,
            IdColumn,
            KeyColumn,
            TypeColumn,
            PayloadColumn,
            MetadataColumn,
            TimestampColumn,
            VersionColumn,
            CorrelationIdColumn,
            JsonSerializerOptions
            );

    private static readonly IDictionary<Type, DbEventStoreConfiguration> Configurations =
        new Dictionary<Type, DbEventStoreConfiguration>();

    internal static void Register(Type type, DbEventStoreConfiguration configuration) 
    {
        Configurations[type] = configuration;
    }

    public static DbEventStoreConfiguration GetForType(Type type)
    {
        if (!Configurations.ContainsKey(type))
            throw new InvalidOperationException(string.Format(Resources.Strings.ConfigurationNotFoundForType, type.Name));

        return Configurations[type];
    }
}