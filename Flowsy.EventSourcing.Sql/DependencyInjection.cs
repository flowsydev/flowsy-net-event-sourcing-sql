using Microsoft.Extensions.DependencyInjection;

namespace Flowsy.EventSourcing.Sql;

public static class DependencyInjection
{
    public static DbEventSourcingBuilder AddDbEventSourcing(
        this IServiceCollection services,
        Action<DbEventStoreConfiguration>? configureDefaults = null
        )
    {
        var defaults = new DbEventStoreConfiguration();
        configureDefaults?.Invoke(defaults);
        return new DbEventSourcingBuilder(services, defaults);
    }
}