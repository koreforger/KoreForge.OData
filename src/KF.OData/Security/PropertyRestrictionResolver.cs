using KF.OData.Attributes;

namespace KF.OData.Security;

/// <summary>
/// Resolves restricted property names for an entity type based on <see cref="ODataPropertyRestrictionAttribute"/>.
/// </summary>
public static class PropertyRestrictionResolver
{
    /// <summary>
    /// Returns the names of properties that are restricted from PATCH operations for a given entity type.
    /// </summary>
    public static IReadOnlySet<string> GetPatchDeniedProperties(Type entityType)
    {
        return GetDeniedProperties(entityType, r => r.DenyPatch);
    }

    /// <summary>
    /// Returns the names of properties that are restricted from PUT operations for a given entity type.
    /// </summary>
    public static IReadOnlySet<string> GetPutDeniedProperties(Type entityType)
    {
        return GetDeniedProperties(entityType, r => r.DenyPut);
    }

    /// <summary>
    /// Returns the names of properties that are restricted from read projections for a given entity type.
    /// </summary>
    public static IReadOnlySet<string> GetReadDeniedProperties(Type entityType)
    {
        return GetDeniedProperties(entityType, r => r.DenyRead);
    }

    private static IReadOnlySet<string> GetDeniedProperties(Type entityType, Func<ODataPropertyRestrictionAttribute, bool> predicate)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in entityType.GetProperties())
        {
            var attr = property.GetCustomAttributes(typeof(ODataPropertyRestrictionAttribute), false)
                .OfType<ODataPropertyRestrictionAttribute>()
                .FirstOrDefault();

            if (attr is not null && predicate(attr))
            {
                result.Add(property.Name);
            }
        }
        return result;
    }
}
