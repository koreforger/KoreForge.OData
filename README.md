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

## Attributes

- `[ODataAuthorize]` — per-operation policies (Read/Create/Update/Delete) and role-based auth
- `[ODataIgnore]` — excludes entity from generation
- `[ODataPropertyRestriction]` — denies read/patch/put/serialization per property

## Security

- **Entity-level**: `[ODataAuthorize]` on entity classes
- **Property-level**: `[ODataPropertyRestriction]` on properties
- **Row-level**: Implement `IRowLevelFilterProvider<TEntity>` and register in DI

## Build

```powershell
.\bin\build-test.ps1
```

## License

[MIT](LICENSE.md)
