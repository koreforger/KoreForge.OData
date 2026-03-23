namespace KF.OData.Attributes;

/// <summary>
/// Restricts a property from specific OData operations.
/// Applied to entity properties to control read/write access at property level.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ODataPropertyRestrictionAttribute : Attribute
{
    /// <summary>When true, the property is excluded from read projections ($select).</summary>
    public bool DenyRead { get; set; }

    /// <summary>When true, the property cannot be modified via PATCH.</summary>
    public bool DenyPatch { get; set; }

    /// <summary>When true, the property cannot be modified via PUT.</summary>
    public bool DenyPut { get; set; }

    /// <summary>When true, the property is excluded from serialization entirely.</summary>
    public bool DenySerialization { get; set; }
}
