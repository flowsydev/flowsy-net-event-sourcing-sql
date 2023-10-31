using Microsoft.Extensions.DependencyInjection;

namespace Flowsy.EventSourcing.Sql;

public class DbEventSourcingBuilder
{
    private readonly IServiceCollection _services;
    private readonly DbEventStoreConfiguration? _defaultConfiguration;

    internal DbEventSourcingBuilder(
        IServiceCollection services,
        DbEventStoreConfiguration? defaultConfiguration = null
        )
    {
        _services = services;
        _defaultConfiguration = defaultConfiguration;
    }

    public DbEventSourcingBuilder UseEventStore<TAbstraction, TImplementation>(
        Action<DbEventStoreConfiguration>? configure = null
        )
        where TAbstraction : class
        where TImplementation : class, TAbstraction
    {
        var config = _defaultConfiguration?.Clone() ?? new DbEventStoreConfiguration();
        configure?.Invoke(config);
        DbEventStoreConfiguration.Register(typeof(TImplementation), config);
        _services.AddTransient<TAbstraction, TImplementation>();
        return this;
    }
}