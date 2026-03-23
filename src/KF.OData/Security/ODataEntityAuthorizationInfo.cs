using KF.OData.Attributes;
using Microsoft.AspNetCore.Authorization;

namespace KF.OData.Security;

/// <summary>
/// Resolves authorization policies for an OData entity based on <see cref="ODataAuthorizeAttribute"/>.
/// </summary>
public sealed class ODataEntityAuthorizationInfo
{
    public string? ReadPolicy { get; init; }
    public string? CreatePolicy { get; init; }
    public string? UpdatePolicy { get; init; }
    public string? DeletePolicy { get; init; }
    public string[]? Roles { get; init; }

    /// <summary>
    /// Extracts authorization info from the <see cref="ODataAuthorizeAttribute"/> on an entity type.
    /// Returns null if no attribute is found.
    /// </summary>
    public static ODataEntityAuthorizationInfo? FromEntityType(Type entityType)
    {
        var attr = entityType.GetCustomAttributes(typeof(ODataAuthorizeAttribute), false)
            .OfType<ODataAuthorizeAttribute>()
            .FirstOrDefault();

        if (attr is null)
            return null;

        return new ODataEntityAuthorizationInfo
        {
            ReadPolicy = attr.ReadPolicy,
            CreatePolicy = attr.CreatePolicy,
            UpdatePolicy = attr.UpdatePolicy,
            DeletePolicy = attr.DeletePolicy,
            Roles = attr.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        };
    }

    /// <summary>
    /// Checks whether the current user is authorized for the specified operation.
    /// </summary>
    public async Task<bool> IsAuthorizedAsync(
        IAuthorizationService authorizationService,
        System.Security.Claims.ClaimsPrincipal user,
        ODataOperation operation)
    {
        // Check role-based first
        if (Roles is { Length: > 0 })
        {
            if (!Roles.Any(role => user.IsInRole(role)))
                return false;
        }

        // Check operation-specific policy
        var policyName = operation switch
        {
            ODataOperation.Read => ReadPolicy,
            ODataOperation.Create => CreatePolicy,
            ODataOperation.Update => UpdatePolicy,
            ODataOperation.Delete => DeletePolicy,
            _ => null
        };

        if (policyName is not null)
        {
            var result = await authorizationService.AuthorizeAsync(user, policyName);
            return result.Succeeded;
        }

        return true;
    }
}

/// <summary>
/// Represents the OData CRUD operation types for authorization mapping.
/// </summary>
public enum ODataOperation
{
    Read,
    Create,
    Update,
    Delete
}
