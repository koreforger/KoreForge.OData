using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using KF.OData.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace KF.OData.Tests;

public class ODataSourceGeneratorTests
{
    private const string SampleDbContext = @"
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SampleApp
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public string Description { get; set; } = """";
        public decimal Total { get; set; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = """";
    }

    public class SalesContext : DbContext
    {
        public SalesContext(DbContextOptions<SalesContext> o) : base(o) { }
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
    }
}
";

    private const string IgnoredEntityContext = @"
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using KF.OData.Attributes;

namespace SampleApp
{
    public class VisibleEntity
    {
        [Key]
        public int Id { get; set; }
    }

    [ODataIgnore]
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
    }

    public class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> o) : base(o) { }
        public DbSet<VisibleEntity> Visible { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    }
}
";

    [Fact]
    public void Generator_ProducesControllersForEachDbSet()
    {
        var (output, diagnostics) = RunGenerator(SampleDbContext);

        output.Should().ContainKey("OrderController.g.cs");
        output.Should().ContainKey("CustomerController.g.cs");

        var orderSource = output["OrderController.g.cs"];
        orderSource.Should().Contain("class OrderController");
        orderSource.Should().Contain("KoreForgeODataController<SampleApp.SalesContext, SampleApp.Order, int>");
        orderSource.Should().Contain("entity.OrderId");
    }

    [Fact]
    public void Generator_ProducesEdmConfigurator()
    {
        var (output, diagnostics) = RunGenerator(SampleDbContext);

        output.Should().ContainKey("SalesEdmConfigurator.g.cs");

        var edmSource = output["SalesEdmConfigurator.g.cs"];
        edmSource.Should().Contain("class SalesEdmConfigurator : IEdmModelConfigurator");
        edmSource.Should().Contain("ContextPrefix => \"Sales\"");
        edmSource.Should().Contain("builder.EntitySet<SampleApp.Order>(\"Orders\")");
        edmSource.Should().Contain("builder.EntitySet<SampleApp.Customer>(\"Customers\")");
    }

    [Fact]
    public void Generator_RespectsODataIgnore()
    {
        var (output, diagnostics) = RunGenerator(IgnoredEntityContext);

        output.Should().ContainKey("VisibleEntityController.g.cs");
        output.Should().NotContainKey("AuditLogController.g.cs");

        var edmSource = output["TestEdmConfigurator.g.cs"];
        edmSource.Should().Contain("EntitySet<SampleApp.VisibleEntity>");
        edmSource.Should().NotContain("AuditLog");
    }

    [Fact]
    public void Generator_UsesConventionKey_WhenNoKeyAttribute()
    {
        var (output, _) = RunGenerator(SampleDbContext);

        var custSource = output["CustomerController.g.cs"];
        custSource.Should().Contain("entity.CustomerId");
    }

    [Fact]
    public void Generator_RoutePrefixFollowsConvention()
    {
        var (output, _) = RunGenerator(SampleDbContext);

        var orderSource = output["OrderController.g.cs"];
        // Controller uses OData convention routing — no [Route] attribute — but the comment documents the expected path
        orderSource.Should().Contain("odata/Sales/Orders");
    }

    [Fact]
    public void Generator_EmitsNoSourceForNonDbContext()
    {
        var source = @"
namespace Foo
{
    public class NotADbContext
    {
        public string Name { get; set; } = """";
    }
}
";
        var (output, _) = RunGenerator(source);
        output.Should().BeEmpty();
    }

    private static (Dictionary<string, string> output, ImmutableArray<Diagnostic> diagnostics) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.KeyAttribute).Assembly.Location),
        };

        // Add runtime assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));

        // Add EF Core reference
        var efCoreAssembly = typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly;
        references.Add(MetadataReference.CreateFromFile(efCoreAssembly.Location));

        // Add KF.OData.Attributes
        var kfODataAssembly = typeof(KF.OData.Attributes.ODataIgnoreAttribute).Assembly;
        references.Add(MetadataReference.CreateFromFile(kfODataAssembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ODataSourceGenerator();
        CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var generatedTrees = outputCompilation.SyntaxTrees
            .Except(compilation.SyntaxTrees)
            .ToDictionary(
                t => Path.GetFileName(t.FilePath),
                t => t.ToString());

        return (generatedTrees, diagnostics);
    }
}
