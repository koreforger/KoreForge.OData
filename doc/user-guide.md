# KoreForge.OData — Usage Guide

## Installation

Reference both packages (the generator is an analyzer, not a runtime dependency):

```xml
<PackageReference Include="KoreForge.OData" />
<PackageReference Include="KoreForge.OData.Generators" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

## Source Generation

The `KoreForge.OData.Generators` package uses a Roslyn incremental source generator to produce:

1. **OData controllers** — one per non-ignored `DbSet<T>` on your `DbContext`
2. **EDM configurators** — registers entity sets with the OData model builder

### What gets generated

For a context like:

```csharp
public class SalesContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    [ODataIgnore]
    public DbSet<AuditLog> AuditLogs { get; set; }
}
```

The generator produces:
- `ProductsController : KoreForgeODataController<SalesContext, Product, int>`
- `OrdersController : KoreForgeODataController<SalesContext, Order, int>`
- `SalesEdmConfigurator : IEdmModelConfigurator`

`AuditLog` is excluded because of `[ODataIgnore]`.

## Registration

In `Program.cs`:

```csharp
// Register the generated EDM configurator
builder.Services.AddEdmModelConfigurator<SalesEdmConfigurator>();

// Add controllers with OData support
builder.Services.AddControllers().AddKoreForgeOData();
```

This wires up OData routing at `/odata/{ContextPrefix}/{EntitySet}`, e.g.:
- `GET /odata/Sales/Products`
- `GET /odata/Sales/Products(1)`
- `POST /odata/Sales/Orders`
- `PATCH /odata/Sales/Orders(1)`
- `PUT /odata/Sales/Orders(1)`
- `DELETE /odata/Sales/Orders(1)`

## OData Query Support

All standard OData query options are supported on `GET` endpoints:

| Option | Example |
|--------|---------|
| `$filter` | `/Products?$filter=Price gt 10` |
| `$select` | `/Products?$select=Name,Price` |
| `$orderby` | `/Products?$orderby=Price desc` |
| `$top` / `$skip` | `/Products?$top=10&$skip=20` |
| `$count` | `/Products?$count=true` |
| `$expand` | `/Orders?$expand=Product` |

## Entity Annotations

### ODataAuthorize

Controls access per CRUD operation:

```csharp
[ODataAuthorize(
    ReadPolicy = "CanReadOrders",
    CreatePolicy = "CanCreateOrders",
    UpdatePolicy = "CanUpdateOrders",
    DeletePolicy = "CanDeleteOrders")]
public class Order { ... }
```

If a policy is not set, that operation defaults to allowed.

### ODataPropertyRestriction

Prevents modification of specific properties:

```csharp
public class Order
{
    [Key]
    public int OrderId { get; set; }

    [ODataPropertyRestriction(DenyPatch = true, DenyPut = true)]
    public string CreatedBy { get; set; } = "system";

    [ODataPropertyRestriction(DenyRead = true, DenySerialization = true)]
    public string InternalSecret { get; set; } = "";
}
```

- **DenyPatch**: Returns 400 if property appears in a PATCH delta
- **DenyPut**: Returns 400 if property value differs from the existing value in a PUT
- **DenyRead**: Property excluded from query results
- **DenySerialization**: Property excluded from JSON serialization

### ODataIgnore

Excludes an entity from OData exposure entirely:

```csharp
[ODataIgnore]
public class AuditLog { ... }
```

## Row-Level Filtering

Implement `IRowLevelFilterProvider<TEntity>` to restrict which rows a user can see:

```csharp
public class OrderRowFilter : IRowLevelFilterProvider<Order>
{
    private readonly IHttpContextAccessor _http;

    public OrderRowFilter(IHttpContextAccessor http) => _http = http;

    public IQueryable<Order> ApplyFilter(IQueryable<Order> query)
    {
        var userId = _http.HttpContext?.User.FindFirst("sub")?.Value;
        return query.Where(o => o.CreatedBy == userId);
    }
}
```

Register in DI:

```csharp
builder.Services.AddScoped<IRowLevelFilterProvider<Order>, OrderRowFilter>();
```

## Controller Hooks

The base controller provides virtual hooks for custom logic:

| Hook | When |
|------|------|
| `OnBeforeQuery` | Before returning a collection query |
| `OnBeforeSingleResult` | Before returning a single entity query |
| `OnBeforeCreate` / `OnAfterCreate` | Around `POST` |
| `OnBeforeReplace` / `OnAfterReplace` | Around `PUT` |
| `OnBeforePatch` / `OnAfterPatch` | Around `PATCH` |
| `OnBeforeDelete` / `OnAfterDelete` | Around `DELETE` |

Override these in partial controller extensions to add validation, auditing, or side effects.
