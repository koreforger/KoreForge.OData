using FluentAssertions;
using KF.OData.Attributes;
using Xunit;

namespace KF.OData.Tests;

public class ODataAuthorizeAttributeTests
{
    [Fact]
    public void DefaultProperties_AreNull()
    {
        var attr = new ODataAuthorizeAttribute();
        attr.ReadPolicy.Should().BeNull();
        attr.CreatePolicy.Should().BeNull();
        attr.UpdatePolicy.Should().BeNull();
        attr.DeletePolicy.Should().BeNull();
        attr.Roles.Should().BeNull();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var attr = new ODataAuthorizeAttribute
        {
            ReadPolicy = "CanRead",
            CreatePolicy = "CanCreate",
            UpdatePolicy = "CanUpdate",
            DeletePolicy = "CanDelete",
            Roles = "Admin,Finance"
        };

        attr.ReadPolicy.Should().Be("CanRead");
        attr.CreatePolicy.Should().Be("CanCreate");
        attr.UpdatePolicy.Should().Be("CanUpdate");
        attr.DeletePolicy.Should().Be("CanDelete");
        attr.Roles.Should().Be("Admin,Finance");
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        var attr = typeof(TestEntity).GetCustomAttributes(typeof(ODataAuthorizeAttribute), false);
        attr.Should().ContainSingle();
        var typed = (ODataAuthorizeAttribute)attr[0];
        typed.ReadPolicy.Should().Be("ReadTest");
    }

    [ODataAuthorize(ReadPolicy = "ReadTest")]
    private class TestEntity { }
}

public class ODataIgnoreAttributeTests
{
    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        typeof(IgnoredEntity).GetCustomAttributes(typeof(ODataIgnoreAttribute), false)
            .Should().ContainSingle();
    }

    [ODataIgnore]
    private class IgnoredEntity { }
}

public class ODataPropertyRestrictionAttributeTests
{
    [Fact]
    public void DefaultsAreFalse()
    {
        var attr = new ODataPropertyRestrictionAttribute();
        attr.DenyRead.Should().BeFalse();
        attr.DenyPatch.Should().BeFalse();
        attr.DenyPut.Should().BeFalse();
        attr.DenySerialization.Should().BeFalse();
    }

    [Fact]
    public void CanSetAll()
    {
        var attr = new ODataPropertyRestrictionAttribute
        {
            DenyRead = true,
            DenyPatch = true,
            DenyPut = true,
            DenySerialization = true
        };

        attr.DenyRead.Should().BeTrue();
        attr.DenyPatch.Should().BeTrue();
        attr.DenyPut.Should().BeTrue();
        attr.DenySerialization.Should().BeTrue();
    }
}
