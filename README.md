# Flowsy Event Sourcing Sql

This package provides a basic but yet customizable SQL-based implementation for the abstractions defined in
[Flowsy.EventSourcing.Abstractions](https://www.nuget.org/packages/Flowsy.EventSourcing.Abstractions) and the
following code snippets are a continuation of its examples.


## Defining Our Events

```csharp
using Flowsy.EventSourcing.Abstractions;

public abstract class ShoppingCartEvent : IEvent
{
    protected ShoppingCartEvent()
    {
    }
}

public sealed class ShoppingCartCreated : ShoppingCartEvent
{
    // Class members
}

public sealed class ShoppingCartItemAdded : ShoppingCartEvent
{
    // Class members
}

public sealed class ShoppingCartItemRemoved : ShoppingCartEvent
{
    // Class members
}

public sealed class ShoppingCartOrderPlaced : ShoppingCartEvent
{
    // Class members
}

public sealed class ShoppingCart : AggregateRoot<ShoppingCartEvent>
{
    // Class members
}
```

**Note:** The full examples can be found in the README file for the package 
[Flowsy.EventSourcing.Abstractions](https://www.nuget.org/packages/Flowsy.EventSourcing.Abstractions). 


## Defining Our Event Stores
First, we're going to define a custom interface as our store for shopping cart events.

```csharp
using Flowsy.EventSourcing.Abstractions;

// This interface will allow to store events derived from ShoppingCartEvent
public interface IShoppingCartEventStore : IEventStore<ShoppingCartEvent>
{
    // By inheriting from IEventStore<TEvent>, our new interface has the following members:
    
    // Task SaveAsync(string key, TEvent @event, CancellationToken cancellationToken);
    // Task SaveAsync(string key, TEvent @event, string? correlationId, CancellationToken cancellationToken);
    // Task SaveAsync(string key, IEnumerable<TEvent> events, CancellationToken cancellationToken);
    // Task SaveAsync(string key, IEnumerable<TEvent> events, string? correlationId, CancellationToken cancellationToken);
    // Task<IEnumerable<TEvent>> LoadEventsAsync(string key, CancellationToken cancellationToken);
    // Task<IEnumerable<EventRecord<TEvent>>> LoadRecordsAsync(string key, CancellationToken cancellationToken);
    
    // If required, you can add new members to fit your needs.
}
```

That's it, our interface is ready and we can proceed to create a class that implements it.
Besides implementing our interface, we can inherit our class from the DbEventStore\<TEvent>
abstract class, so we can reuse most of the required functionality to save and load events
from the underlying database.

```csharp
using Flowsy.Db.Abstractions;
using Flowsy.EventSourcing.Sql;

// Implementing the shopping cart event store
public class ShoppingCartDbEventStore : DbEventStore<ShoppingCartEvent>, IShoppingCartEventStore
{
    // Create the event store with factory to get database connections from 
    public ShoppingCartDbEventStore(IDbConnectionFactory dbConnectionFactory)
        : base(dbConnectionFactory)
    {
    }
    
    // Create the event store with a database connection
    public ShoppingCartDbEventStore(IDbConnection dbConnection)
        : base(dbConnection)
    {
    }

    // Create the event store with a database transaction
    public ShoppingCartDbEventStore(IDbTransaction dbTransaction)
        : base(dbTransaction)
    {
    }
    
    // Most of the members of DbEventStore<TEvent> are virtual,
    // so we can override the default implementation and add
    // custom behavior to our event stores.
}

// Implementing other event stores
public class ProductDbEventStore : DbEventStore<ProductEvent>, IProductEventStore
{
    // Create the event store with factory to get database connections from 
    public ProductDbEventStore(IDbConnectionFactory dbConnectionFactory)
        : base(dbConnectionFactory)
    {
    }
    
    // Create the event store with a database connection
    public ProductDbEventStore(IDbConnection dbConnection)
        : base(dbConnection)
    {
    }

    // Create the event store with a database transaction
    public ProductDbEventStore(IDbTransaction dbTransaction)
        : base(dbTransaction)
    {
    }
    
    // Most of the members of DbEventStore<TEvent> are virtual,
    // so we can override the default implementation and add
    // custom behavior to our event stores.
}
```
Once more, that's all we need to have our own store for shopping cart events.


## Dependency Injection in Web APIs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register a DbConnectionFactory service with the required configurations taken from the application settings
builder.Services.AddDbConnectionFactory(serviceProvider => {
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var sqlConfiguration = configuration.GetRequiredSection("Databases");
    var dbConnectionConfigurations = 
        sqlConfiguration.GetChildren().Select(databaseConfiguration => new DbConnectionConfiguration
        {
            Key = databaseConfiguration.Key,
            ProviderInvariantName = databaseConfiguration["ProviderInvariantName"],
            ConnectionString = databaseConfiguration["ConnectionString"]
        });

    return new DbConnectionFactory(dbConnectionConfigurations.ToArray());
    });

// Register the default configuration for all event stores
// and then customize individual configurations as needed 
builder
    .Services
    .AddDbEventSourcing(config => {
        config.ConnectionKey = "Default";
        config.IdColumn = "id";
        config.KeyColumn = "key";
        config.TypeColumn = "type";
        config.PayloadColumn = "payload";
        config.MetadataColumn = "metadata";
        config.TimestampColumn = "timestamp";
        config.VersionColumn = "version";
        config.CorrelationIdColumn = "correlation_id";
    })
    .UseEventStore<IShoppingCartEventStore, ShoppingCartDbEventStore>(config => {
        config.TargetSchema = "sales";
        config.TargetTable = "shopping_cart_event";
        config.IdColumn = "shopping_cart_id";
    })
    .UseEventStore<IProductEventStore, ProductDbEventStore>(config => {
        config.TargetSchema = "inventory";
        config.TargetTable = "product_event";
        config.IdColumn = "product_id";
    });

// Add and configure services
// ...

var app = builder.Build();

// Activate services

app.Run();

```

## Using The Event Stores
Once our event stores are registered and configured, we can use them as needed.
For example, we can use an event store in a command handler for adding items
to our shopping cart.

```csharp
public class AddShoppingCartItemCommand
{
    public AddShoppingCartItemCommand(
        Guid shoppingCartId,
        Guid shoppingCartItemId,
        Guid productId
        double quantity
        )
    {
        ShoppingCartId = shoppingCartId;
        ShoppingCartItemId = shoppingCartItemId;
        ProductId = productId;
        Quantity = quantity;
    }
    
    public Guid ShoppingCartId { get; }
    public Guid ShoppingCartItemId { get; }
    public Guid ProductId { get; }
    public double Quantity { get; }
}

public class AddShoppingCartItemCommandHandler
{
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    private readonly IProductRepository _productRepository;
    
    public AddShoppingCartItemCommandHandler(
        IShoppingCartEventStore shoppingCartEventStore,
        IProductRepository productRepository
        )
    {
        _shoppingCartEventStore = shoppingCartEventStore;
        _productRepository = productRepository;
    }
    
    public async Task HandleAsync(AddShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = new ShoppingCart();
        
        await shoppingCart.LoadAsync(
            command.ShoppingCartId.ToString(),
            _shoppingCartEventStore,
            cancellationToken
            );
        
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            throw new ProductNotFoundException(command.ProductId);
        
        shoppingCart.AddItem(
            command.ShoppingCartItemId,
            product,
            command.Quantity
            );
        
        await shoppingCart.SaveAsync(_shoppingCartEventStore, cancellationToken);
    }
}
```