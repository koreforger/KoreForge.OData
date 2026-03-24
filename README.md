# KoreForge.OData

OData infrastructure and source generation for the KoreForge ecosystem. Provides everything needed to expose EF Core `DbContext`s as fully-featured OData APIs with authorization, property-level restrictions, and row-level filtering.

## Packages

| Package | Description |
|---------|-------------|
| `KoreForge.OData` | Attributes, security model, base controller, EDM registration, ASP.NET Core integration |
| `KoreForge.OData.Generators` | Roslyn source generator — auto-generates OData controllers and EDM configurators from DbContext |

## Quick Start

1. Reference both packages:

```xml
<PackageReference Include="KoreForge.OData" />
<PackageReference Include="KoreForge.OData.Generators" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

2. Annotate entities:

```csharp
[ODataAuthorize(ReadPolicy = "CanReadOrders", CreatePolicy = "CanCreateOrders")]
public class Order
{
    [Key]
    public int OrderId { get; set; }
    public string Description { get; set; } = "";

    [ODataPropertyRestriction(DenyPatch = true, DenyPut = true)]
    public string CreatedBy { get; set; } = "system";
}

[ODataIgnore]
public class AuditLog { ... }
```

3. Register in `Program.cs`:

```csharp
builder.Services.AddEdmModelConfigurator<SalesEdmConfigurator>(); // generated
builder.Services.AddControllers().AddKoreForgeOData();
```

4. Routes are generated as `/odata/{ContextPrefix}/{EntitySet}`.

See [doc/UsageGuide.md](doc/UsageGuide.md) for detailed usage.

## Attributes

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[ODataAuthorize]` | Entity class | Per-operation policies (Read/Create/Update/Delete) and role-based auth |
| `[ODataIgnore]` | Entity class | Excludes entity from OData generation |
| `[ODataPropertyRestriction]` | Property | Denies read/patch/put/serialization per property |

## Security

- **Entity-level**: `[ODataAuthorize]` on entity classes — controls who can perform CRUD operations
- **Property-level**: `[ODataPropertyRestriction]` on properties — prevents modification of sensitive fields via PATCH or PUT
- **Row-level**: Implement `IRowLevelFilterProvider<TEntity>` and register in DI — filters queries per-user

See [doc/SecurityGuide.md](doc/SecurityGuide.md) for details.

## Prerequisites

- .NET 10 SDK
- SQL Server (integration tests require a SQL Server instance)

## Development

```powershell
# Build and test (unit tests only)
.\bin\build-test.ps1
```

## Solution Layout

```
src/
  KF.OData/                           Runtime library (attributes, security, base controller, DI)
    Attributes/                        ODataAuthorize, ODataIgnore, ODataPropertyRestriction
    Configuration/                     EDM model building and ASP.NET Core integration
    Controllers/                       KoreForgeODataController<TContext, TEntity, TKey> base class
    Security/                          Authorization, row-level filtering, property restrictions
  KF.OData.Generators/                 Roslyn source generator (netstandard2.0)
tst/
  KF.OData.Tests/                      Unit tests (21 tests)
  KF.OData.Integration.Tests/          Integration tests against SQL Server (13 tests)
bin/
  build-test.ps1                       Automation script
```

## License

MIT — see [LICENSE.md](LICENSE.md).
