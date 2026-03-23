using KF.OData.Configuration;
using KF.OData.Integration.Tests.TestModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.ModelBuilder;

namespace KF.OData.Integration.Tests;

/// <summary>
/// Minimal ASP.NET Core host used by the integration tests.
/// Wires up the TestCatalogContext, OData, and a permissive auth policy.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // DbContext — connection string injected by test fixture
        var connectionString = builder.Configuration["ConnectionStrings:TestDb"]
                               ?? "Server=(localdb)\\mssqllocaldb;Database=ODataIntTest;Trusted_Connection=True;";

        builder.Services.AddDbContext<TestCatalogContext>(opt =>
            opt.UseSqlServer(connectionString));

        // Authorization — allow everything in tests
        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(_ => true)
                .Build();
            options.AddPolicy("CanReadOrders", p => p.RequireAssertion(_ => true));
            options.AddPolicy("CanCreateOrders", p => p.RequireAssertion(_ => true));
        });

        // Build EDM model
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.EntitySet<Product>("Products");
        modelBuilder.EntitySet<Order>("Orders");
        var edmModel = modelBuilder.GetEdmModel();

        // OData with route components
        builder.Services.AddControllers()
            .AddOData(opt => opt
                .AddRouteComponents("odata/TestCatalog", edmModel)
                .Count()
                .Filter()
                .OrderBy()
                .Select()
                .Expand()
                .SetMaxTop(50));

        var app = builder.Build();

        // Ensure DB is created
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestCatalogContext>();
            db.Database.EnsureCreated();
        }

        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
