# KoreForge.OData — Security Guide

## Overview

KoreForge.OData provides three layers of security that work together:

1. **Entity-level authorization** — controls who can perform CRUD on each entity
2. **Property-level restrictions** — prevents modification or exposure of sensitive properties
3. **Row-level filtering** — restricts which rows a user can see or modify

All layers are enforced in the base controller (`KoreForgeODataController`) automatically.

## Entity-Level Authorization

Use `[ODataAuthorize]` on entity classes to require ASP.NET Core authorization policies:

```csharp
[ODataAuthorize(
    ReadPolicy = "CanReadOrders",
    CreatePolicy = "CanCreateOrders",
    UpdatePolicy = "CanUpdateOrders",
    DeletePolicy = "CanDeleteOrders")]
public class Order { ... }
```

- Each operation maps to a policy name registered in `AuthorizationOptions`
- If a policy is not specified, that operation is **allowed by default**
- Authorization is checked before any database access occurs
- Failed authorization returns `403 Forbidden`

### Role-Based Shorthand

You can also specify roles directly:

```csharp
[ODataAuthorize(ReadRoles = "Reader,Admin", CreateRoles = "Admin")]
public class Product { ... }
```

## Property-Level Restrictions

Use `[ODataPropertyRestriction]` to control individual properties:

```csharp
public class Order
{
    [ODataPropertyRestriction(DenyPatch = true, DenyPut = true)]
    public string CreatedBy { get; set; } = "system";

    [ODataPropertyRestriction(DenyRead = true)]
    public string InternalNotes { get; set; } = "";
}
```

### Enforcement Behaviour

| Flag | PATCH | PUT | GET |
|------|-------|-----|-----|
| `DenyPatch` | 400 if property in delta | — | — |
| `DenyPut` | — | 400 if value differs from existing | — |
| `DenyRead` | — | — | Property excluded from queries |
| `DenySerialization` | — | — | Property excluded from JSON output |

**PATCH enforcement**: If any changed property in the `Delta<T>` is in the denied set, the request is rejected with `400 Bad Request` before any modification occurs.

**PUT enforcement**: The incoming entity's denied property values are compared against the existing entity. If any denied property has a different value, the request is rejected. This allows PUTs that don't change the restricted property to succeed.

## Row-Level Filtering

Implement `IRowLevelFilterProvider<TEntity>`:

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

The filter is applied to both collection (`GET /Products`) and single-entity (`GET /Products(1)`) queries. If an entity exists but fails the row filter, the user receives `404 Not Found` rather than `403 Forbidden` to avoid information leakage.

## Security Checklist

- [ ] All entities with sensitive data have `[ODataAuthorize]` policies
- [ ] Immutable fields (CreatedBy, CreatedUtc) have `DenyPatch = true, DenyPut = true`
- [ ] Internal/secret fields have `DenyRead = true` and/or `DenySerialization = true`
- [ ] Multi-tenant or user-scoped entities have `IRowLevelFilterProvider` implementations
- [ ] Auth policies are registered in `AuthorizationOptions` during startup
- [ ] Entities that should not be exposed have `[ODataIgnore]`
