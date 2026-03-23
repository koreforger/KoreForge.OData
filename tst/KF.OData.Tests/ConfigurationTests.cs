using FluentAssertions;
using KF.OData.Configuration;
using Xunit;

namespace KF.OData.Tests;

public class KoreForgeODataOptionsTests
{
    [Fact]
    public void Defaults_AreReasonable()
    {
        var options = new KoreForgeODataOptions();
        options.MaxPageSize.Should().Be(100);
        options.MaxExpandDepth.Should().Be(3);
        options.MaxNodeCount.Should().Be(100);
        options.EnableCount.Should().BeTrue();
        options.EnableFilter.Should().BeTrue();
        options.EnableOrderBy.Should().BeTrue();
        options.EnableSelect.Should().BeTrue();
        options.EnableExpand.Should().BeTrue();
        options.RoutePrefix.Should().Be("odata");
    }

    [Fact]
    public void AllProperties_CanBeOverridden()
    {
        var options = new KoreForgeODataOptions
        {
            MaxPageSize = 50,
            MaxExpandDepth = 5,
            MaxNodeCount = 200,
            EnableCount = false,
            EnableFilter = false,
            RoutePrefix = "api/odata"
        };

        options.MaxPageSize.Should().Be(50);
        options.MaxExpandDepth.Should().Be(5);
        options.MaxNodeCount.Should().Be(200);
        options.EnableCount.Should().BeFalse();
        options.EnableFilter.Should().BeFalse();
        options.RoutePrefix.Should().Be("api/odata");
    }
}
