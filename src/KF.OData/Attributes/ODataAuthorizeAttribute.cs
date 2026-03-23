namespace KF.OData.Attributes;

/// <summary>
/// Marks an entity with per-operation authorization policies for OData endpoints.
/// Applied to entity partial classes to control access at entity and operation level.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ODataAuthorizeAttribute : Attribute
{
    /// <summary>Policy name required for read operations (Get, Get by key).</summary>
    public string? ReadPolicy { get; set; }

    /// <summary>Policy name required for create operations (Post).</summary>
    public string? CreatePolicy { get; set; }

    /// <summary>Policy name required for update operations (Put, Patch).</summary>
    public string? UpdatePolicy { get; set; }

    /// <summary>Policy name required for delete operations.</summary>
    public string? DeletePolicy { get; set; }

    /// <summary>Comma-separated role names. If set, all operations require one of these roles.</summary>
    public string? Roles { get; set; }
}
