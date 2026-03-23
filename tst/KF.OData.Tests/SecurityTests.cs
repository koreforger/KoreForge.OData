using FluentAssertions;
using KF.OData.Attributes;
using KF.OData.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace KF.OData.Tests;

public class ODataEntityAuthorizationInfoTests
{
    [Fact]
    public void FromEntityType_WithNoAttribute_ReturnsNull()
    {
        var info = ODataEntityAuthorizationInfo.FromEntityType(typeof(PlainEntity));
        info.Should().BeNull();
    }

    [Fact]
    public void FromEntityType_WithAttribute_ExtractsPolicies()
    {
        var info = ODataEntityAuthorizationInfo.FromEntityType(typeof(SecuredEntity));
        info.Should().NotBeNull();
        info!.ReadPolicy.Should().Be("CanRead");
        info.CreatePolicy.Should().Be("CanCreate");
        info.UpdatePolicy.Should().Be("CanEdit");
        info.DeletePolicy.Should().Be("CanDelete");
    }

    [Fact]
    public void FromEntityType_WithRoles_ParsesRoleList()
    {
        var info = ODataEntityAuthorizationInfo.FromEntityType(typeof(RoleEntity));
        info.Should().NotBeNull();
        info!.Roles.Should().BeEquivalentTo("Admin", "Finance");
    }

    private class PlainEntity { }

    [ODataAuthorize(
        ReadPolicy = "CanRead",
        CreatePolicy = "CanCreate",
        UpdatePolicy = "CanEdit",
        DeletePolicy = "CanDelete")]
    private class SecuredEntity { }

    [ODataAuthorize(Roles = "Admin,Finance")]
    private class RoleEntity { }
}

public class PropertyRestrictionResolverTests
{
    [Fact]
    public void GetPatchDeniedProperties_ReturnsRestrictedProps()
    {
        var denied = PropertyRestrictionResolver.GetPatchDeniedProperties(typeof(RestrictedEntity));
        denied.Should().Contain("Secret");
        denied.Should().NotContain("Name");
    }

    [Fact]
    public void GetPutDeniedProperties_ReturnsRestrictedProps()
    {
        var denied = PropertyRestrictionResolver.GetPutDeniedProperties(typeof(RestrictedEntity));
        denied.Should().Contain("Secret");
    }

    [Fact]
    public void GetReadDeniedProperties_ReturnsRestrictedProps()
    {
        var denied = PropertyRestrictionResolver.GetReadDeniedProperties(typeof(RestrictedEntity));
        denied.Should().Contain("Hidden");
        denied.Should().NotContain("Name");
    }

    [Fact]
    public void NoRestrictions_ReturnsEmptySet()
    {
        var denied = PropertyRestrictionResolver.GetPatchDeniedProperties(typeof(PlainEntity));
        denied.Should().BeEmpty();
    }

    private class PlainEntity
    {
        public string Name { get; set; } = "";
    }

    private class RestrictedEntity
    {
        public string Name { get; set; } = "";

        [ODataPropertyRestriction(DenyPatch = true, DenyPut = true)]
        public string Secret { get; set; } = "";

        [ODataPropertyRestriction(DenyRead = true)]
        public string Hidden { get; set; } = "";
    }
}
