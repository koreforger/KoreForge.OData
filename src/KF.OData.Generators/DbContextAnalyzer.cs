using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace KF.OData.Generators;

/// <summary>
/// Analyses a DbContext class symbol to extract entity set metadata for generation.
/// </summary>
internal static class DbContextAnalyzer
{
    private const string DbContextFullName = "Microsoft.EntityFrameworkCore.DbContext";
    private const string DbSetFullName = "Microsoft.EntityFrameworkCore.DbSet";
    private const string ODataIgnoreFullName = "KF.OData.Attributes.ODataIgnoreAttribute";
    private const string ODataAuthorizeFullName = "KF.OData.Attributes.ODataAuthorizeAttribute";
    private const string KeyAttributeFullName = "System.ComponentModel.DataAnnotations.KeyAttribute";

    public static DbContextInfo? TryExtract(INamedTypeSymbol contextSymbol, Compilation compilation)
    {
        // Verify it inherits from DbContext
        if (!InheritsFrom(contextSymbol, DbContextFullName))
            return null;

        var contextNamespace = contextSymbol.ContainingNamespace.ToDisplayString();
        var contextPrefix = DbContextInfo.ComputePrefix(contextSymbol.Name);
        var entitySets = new List<EntitySetInfo>();

        // Find all DbSet<T> properties
        foreach (var member in contextSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            if (property.Type is not INamedTypeSymbol propertyType)
                continue;

            if (!IsDbSetType(propertyType))
                continue;

            var entityType = propertyType.TypeArguments[0] as INamedTypeSymbol;
            if (entityType is null)
                continue;

            var isIgnored = HasAttribute(entityType, ODataIgnoreFullName);
            var authorizeAttr = GetAttribute(entityType, ODataAuthorizeFullName);
            var keyInfo = FindPrimaryKey(entityType);
            var schema = DeriveSchema(entityType, contextNamespace, contextPrefix);

            entitySets.Add(new EntitySetInfo(
                entityTypeName: entityType.Name,
                entityTypeFullName: entityType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", ""),
                propertyName: property.Name,
                keyPropertyName: keyInfo.keyName,
                keyTypeName: keyInfo.keyType,
                isIgnored: isIgnored,
                hasAuthorize: authorizeAttr is not null,
                readPolicy: GetNamedArgument(authorizeAttr, "ReadPolicy"),
                createPolicy: GetNamedArgument(authorizeAttr, "CreatePolicy"),
                updatePolicy: GetNamedArgument(authorizeAttr, "UpdatePolicy"),
                deletePolicy: GetNamedArgument(authorizeAttr, "DeletePolicy"),
                roles: GetNamedArgument(authorizeAttr, "Roles"),
                schema: schema
            ));
        }

        if (entitySets.Count == 0)
            return null;

        return new DbContextInfo(
            contextTypeName: contextSymbol.Name,
            contextTypeFullName: contextSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", ""),
            contextNamespace: contextNamespace,
            entitySets: entitySets
        );
    }

    private static bool InheritsFrom(INamedTypeSymbol symbol, string baseTypeFullName)
    {
        var current = symbol.BaseType;
        while (current is not null)
        {
            if (current.ToDisplayString() == baseTypeFullName)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static bool IsDbSetType(INamedTypeSymbol type)
    {
        return type.IsGenericType
               && type.OriginalDefinition.ToDisplayString() == "Microsoft.EntityFrameworkCore.DbSet<TEntity>";
    }

    private static bool HasAttribute(INamedTypeSymbol type, string attributeFullName)
    {
        return type.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    private static AttributeData? GetAttribute(INamedTypeSymbol type, string attributeFullName)
    {
        return type.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    private static string? GetNamedArgument(AttributeData? attr, string name)
    {
        if (attr is null) return null;
        var arg = attr.NamedArguments.FirstOrDefault(a => a.Key == name);
        return arg.Value.Value as string;
    }

    private static (string? keyName, string? keyType) FindPrimaryKey(INamedTypeSymbol entityType)
    {
        // Strategy 1: Look for [Key] attribute
        foreach (var member in entityType.GetMembers())
        {
            if (member is IPropertySymbol prop)
            {
                if (prop.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == KeyAttributeFullName))
                {
                    return (prop.Name, prop.Type.ToDisplayString());
                }
            }
        }

        // Strategy 2: Convention — {TypeName}Id or Id
        var conventions = new[] { entityType.Name + "Id", "Id" };
        foreach (var convention in conventions)
        {
            var prop = entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => string.Equals(p.Name, convention, StringComparison.OrdinalIgnoreCase));

            if (prop is not null)
                return (prop.Name, prop.Type.ToDisplayString());
        }

        return (null, null);
    }

    private static string? DeriveSchema(INamedTypeSymbol entityType, string contextNamespace, string contextPrefix)
    {
        var entityNs = entityType.ContainingNamespace.ToDisplayString();
        var expectedPrefix = contextNamespace + "." + contextPrefix;

        if (entityNs.Length > expectedPrefix.Length
            && entityNs.StartsWith(expectedPrefix + ".", StringComparison.Ordinal))
        {
            return entityNs.Substring(expectedPrefix.Length + 1);
        }

        return null;
    }
}
