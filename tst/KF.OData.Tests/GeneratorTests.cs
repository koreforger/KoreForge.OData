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
using KF.OData.Attributes;

[assembly: GenerateODataFor(typeof(SampleApp.SalesContext))]

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

[assembly: GenerateODataFor(typeof(SampleApp.TestContext))]

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

    private const string SchemaAwareContext = @"
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using KF.OData.Attributes;

[assembly: GenerateODataFor(typeof(KF.Data.AlertsDbContext))]

namespace KF.Data.Alerts.Notification
{
    public class Channel
    {
        [Key]
        public int ChannelId { get; set; }
        public string Name { get; set; } = """";
    }

    public class Priority
    {
        [Key]
        public int PriorityId { get; set; }
        public string Label { get; set; } = """";
    }
}

namespace KF.Data.Alerts
{
    public class AuditTrail
    {
        [Key]
        public int AuditTrailId { get; set; }
    }
}

namespace KF.Data
{
    public class AlertsDbContext : DbContext
    {
        public AlertsDbContext(DbContextOptions<AlertsDbContext> o) : base(o) { }
        public DbSet<KF.Data.Alerts.Notification.Channel> Channels { get; set; } = null!;
        public DbSet<KF.Data.Alerts.Notification.Priority> Priorities { get; set; } = null!;
        public DbSet<KF.Data.Alerts.AuditTrail> AuditTrails { get; set; } = null!;
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
        orderSource.Should().Contain("odata/Sales/Orders");
    }

    [Fact]
    public void Generator_EmitsOnlyAttributeForNonDbContext()
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
        output.Should().ContainKey("GenerateODataForAttribute.g.cs");
        output.Should().HaveCount(1);
    }

    [Fact]
    public void Generator_ControllersUseCleanNamespace()
    {
        var (output, _) = RunGenerator(SampleDbContext);

        var orderSource = output["OrderController.g.cs"];
        orderSource.Should().Contain("namespace SampleApp.Controllers;");
        orderSource.Should().NotContain(".Generated.");
    }

    [Fact]
    public void Generator_EdmConfiguratorsUseCleanNamespace()
    {
        var (output, _) = RunGenerator(SampleDbContext);

        var edmSource = output["SalesEdmConfigurator.g.cs"];
        edmSource.Should().Contain("namespace SampleApp.Configuration;");
        edmSource.Should().NotContain(".Generated.");
    }

    [Fact]
    public void Generator_StripsDbContextSuffix()
    {
        var source = @"
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using KF.OData.Attributes;

[assembly: GenerateODataFor(typeof(MyApp.AlertsDbContext))]

namespace MyApp
{
    public class Channel
    {
        [Key]
        public int ChannelId { get; set; }
    }

    public class AlertsDbContext : DbContext
    {
        public AlertsDbContext(DbContextOptions<AlertsDbContext> o) : base(o) { }
        public DbSet<Channel> Channels { get; set; } = null!;
    }
}
";
        var (output, _) = RunGenerator(source);

        output.Should().ContainKey("AlertsEdmConfigurator.g.cs");
        var edmSource = output["AlertsEdmConfigurator.g.cs"];
        edmSource.Should().Contain("ContextPrefix => \"Alerts\"");
    }

    [Fact]
    public void Generator_GroupsEntitiesBySchema()
    {
        var (output, _) = RunGenerator(SchemaAwareContext);

        // Two EDM configurators — one per schema
        output.Should().ContainKey("AlertsNotificationEdmConfigurator.g.cs");
        output.Should().ContainKey("AlertsEdmConfigurator.g.cs");

        var notificationEdm = output["AlertsNotificationEdmConfigurator.g.cs"];
        notificationEdm.Should().Contain("ContextPrefix => \"Alerts/Notification\"");
        notificationEdm.Should().Contain("Channel");
        notificationEdm.Should().Contain("Priority");
        notificationEdm.Should().NotContain("AuditTrail");

        var defaultEdm = output["AlertsEdmConfigurator.g.cs"];
        defaultEdm.Should().Contain("ContextPrefix => \"Alerts\"");
        defaultEdm.Should().NotContain("ContextPrefix => \"Alerts/");
        defaultEdm.Should().Contain("AuditTrail");
        defaultEdm.Should().NotContain("Channel");
    }

    [Fact]
    public void Generator_SchemaAwareRouteInControllerComment()
    {
        var (output, _) = RunGenerator(SchemaAwareContext);

        var channelSource = output["NotificationChannelController.g.cs"];
        channelSource.Should().Contain("odata/Alerts/Notification/Channels");
        channelSource.Should().Contain("class NotificationChannelController");

        var auditSource = output["AuditTrailController.g.cs"];
        auditSource.Should().Contain("odata/Alerts/AuditTrails");
        auditSource.Should().NotContain("odata/Alerts/Notification");
    }

    [Fact]
    public void Generator_SchemaPrefix_PreventsControllerNameCollision()
    {
        // Two schemas each with a "Channel" entity — must produce separate controllers
        var source = @"
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using KF.OData.Attributes;

[assembly: GenerateODataFor(typeof(MyApp.MultiDbContext))]

namespace MyApp.Multi.Notification
{
    public class Channel
    {
        [Key]
        public int ChannelId { get; set; }
        public string Name { get; set; } = """";
    }
}

namespace MyApp.Multi.Archive
{
    public class Channel
    {
        [Key]
        public int ChannelId { get; set; }
        public string Name { get; set; } = """";
    }
}

namespace MyApp
{
    public class MultiDbContext : DbContext
    {
        public MultiDbContext(DbContextOptions<MultiDbContext> o) : base(o) { }
        public DbSet<MyApp.Multi.Notification.Channel> NotificationChannels { get; set; } = null!;
        public DbSet<MyApp.Multi.Archive.Channel> ArchiveChannels { get; set; } = null!;
    }
}
";
        var (output, _) = RunGenerator(source);

        // Both controllers must appear — no collision
        output.Should().ContainKey("NotificationChannelController.g.cs");
        output.Should().ContainKey("ArchiveChannelController.g.cs");

        // Class names include schema prefix
        var notifSource = output["NotificationChannelController.g.cs"];
        notifSource.Should().Contain("class NotificationChannelController");
        notifSource.Should().Contain("odata/Multi/Notification/NotificationChannels");

        var archiveSource = output["ArchiveChannelController.g.cs"];
        archiveSource.Should().Contain("class ArchiveChannelController");
        archiveSource.Should().Contain("odata/Multi/Archive/ArchiveChannels");
    }

    [Fact]
    public void Generator_NoSchemaPrefix_WhenNoSchema()
    {
        // Entities without a schema should NOT get a prefix
        var (output, _) = RunGenerator(SampleDbContext);

        output.Should().ContainKey("OrderController.g.cs");
        var orderSource = output["OrderController.g.cs"];
        orderSource.Should().Contain("class OrderController");
        orderSource.Should().NotContain("class SalesOrderController");
    }

    [Fact]
    public void Generator_RequiresAssemblyAttribute()
    {
        // DbContext in source but no [assembly: GenerateODataFor] → no controllers
        var source = @"
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SampleApp
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
    }

    public class SalesContext : DbContext
    {
        public SalesContext(DbContextOptions<SalesContext> o) : base(o) { }
        public DbSet<Order> Orders { get; set; } = null!;
    }
}
";
        var (output, _) = RunGenerator(source);
        output.Should().ContainKey("GenerateODataForAttribute.g.cs");
        output.Should().HaveCount(1, "no controllers should be generated without assembly attribute");
    }

    [Fact]
    public void Generator_AlwaysInjectsAttributeSource()
    {
        var source = @"namespace Empty { }";
        var (output, _) = RunGenerator(source);
        output.Should().ContainKey("GenerateODataForAttribute.g.cs");
        var attrSource = output["GenerateODataForAttribute.g.cs"];
        attrSource.Should().Contain("class GenerateODataForAttribute");
        attrSource.Should().Contain("AttributeTargets.Assembly");
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
