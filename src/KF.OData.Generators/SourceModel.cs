using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace KF.OData.Generators;

/// <summary>
/// Metadata about a DbSet property on a DbContext.
/// </summary>
internal sealed class EntitySetInfo
{
    public string EntityTypeName { get; }
    public string EntityTypeFullName { get; }
    public string PropertyName { get; }
    public string? KeyPropertyName { get; }
    public string? KeyTypeName { get; }
    public bool IsIgnored { get; }
    public bool HasAuthorize { get; }
    public string? ReadPolicy { get; }
    public string? CreatePolicy { get; }
    public string? UpdatePolicy { get; }
    public string? DeletePolicy { get; }
    public string? Roles { get; }

    public EntitySetInfo(
        string entityTypeName,
        string entityTypeFullName,
        string propertyName,
        string? keyPropertyName,
        string? keyTypeName,
        bool isIgnored,
        bool hasAuthorize,
        string? readPolicy,
        string? createPolicy,
        string? updatePolicy,
        string? deletePolicy,
        string? roles)
    {
        EntityTypeName = entityTypeName;
        EntityTypeFullName = entityTypeFullName;
        PropertyName = propertyName;
        KeyPropertyName = keyPropertyName;
        KeyTypeName = keyTypeName;
        IsIgnored = isIgnored;
        HasAuthorize = hasAuthorize;
        ReadPolicy = readPolicy;
        CreatePolicy = createPolicy;
        UpdatePolicy = updatePolicy;
        DeletePolicy = deletePolicy;
        Roles = roles;
    }
}

/// <summary>
/// Metadata about a DbContext with its entity sets.
/// </summary>
internal sealed class DbContextInfo
{
    public string ContextTypeName { get; }
    public string ContextTypeFullName { get; }
    public string ContextNamespace { get; }
    public string ContextPrefix { get; }
    public List<EntitySetInfo> EntitySets { get; }

    public DbContextInfo(
        string contextTypeName,
        string contextTypeFullName,
        string contextNamespace,
        List<EntitySetInfo> entitySets)
    {
        ContextTypeName = contextTypeName;
        ContextTypeFullName = contextTypeFullName;
        ContextNamespace = contextNamespace;
        EntitySets = entitySets;

        // Derive prefix: "SalesContext" -> "Sales", "AlertsDbContext" -> "AlertsDb"
        ContextPrefix = contextTypeName.EndsWith("Context", StringComparison.Ordinal)
            ? contextTypeName.Substring(0, contextTypeName.Length - "Context".Length)
            : contextTypeName;
    }
}
