namespace KF.OData.Attributes;

/// <summary>
/// Excludes an entity from OData generation entirely.
/// No controller, route, or EDM registration will be generated.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ODataIgnoreAttribute : Attribute
{
}
