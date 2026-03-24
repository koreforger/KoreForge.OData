using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KF.OData.Integration.Tests.TestModel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KF.OData.Integration.Tests;

[Collection("SqlServer")]
public class ODataCrudTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;
    private ODataWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public ODataCrudTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    public async Task InitializeAsync()
    {
        _factory = new ODataWebApplicationFactory(_sql.ConnectionString);
        _client = _factory.CreateClient();

        // Seed test data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestCatalogContext>();
        await db.Database.EnsureCreatedAsync();

        db.Products.RemoveRange(db.Products);
        db.Orders.RemoveRange(db.Orders);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product { ProductId = 1, Name = "Widget", Price = 9.99m, Category = "Gadgets" },
            new Product { ProductId = 2, Name = "Gizmo", Price = 19.99m, Category = "Gadgets" },
            new Product { ProductId = 3, Name = "Doohickey", Price = 4.50m, Category = "Tools" }
        );
        db.Orders.Add(new Order { OrderId = 1, ProductId = 1, Quantity = 5, CreatedBy = "test" });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── GET all ──

    [Fact]
    public async Task GetAll_Products_ReturnsSeededData()
    {
        var response = await _client.GetAsync("/odata/TestCatalog/Products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ODataResult<Product>>(JsonOpts);
        json!.Value.Should().HaveCount(3);
    }

    // ── GET by key ──

    [Fact]
    public async Task GetByKey_ReturnsCorrectProduct()
    {
        var response = await _client.GetAsync("/odata/TestCatalog/Products(1)");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<Product>(JsonOpts);
        json!.Name.Should().Be("Widget");
    }

    // ── $filter ──

    [Fact]
    public async Task Filter_ByCategory_Works()
    {
        var response = await _client.GetAsync("/odata/TestCatalog/Products?$filter=Category eq 'Gadgets'");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ODataResult<Product>>(JsonOpts);
        json!.Value.Should().HaveCount(2);
    }

    // ── $select ──

    [Fact]
    public async Task Select_ReturnsOnlyRequestedFields()
    {
        var response = await _client.GetAsync("/odata/TestCatalog/Products?$select=Name,Price");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Name").And.Contain("Price");
    }

    // ── $orderby ──

    [Fact]
    public async Task OrderBy_ReturnsOrderedResults()
    {
        var response = await _client.GetAsync("/odata/TestCatalog/Products?$orderby=Price desc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ODataResult<Product>>(JsonOpts);
        json!.Value[0].Name.Should().Be("Gizmo");
    }

    // ── $top / $skip ──

    [Fact]
    public async Task TopAndSkip_Paginates()
    {
        var response = await _client.GetAsync("/odata/TestCatalog/Products?$orderby=ProductId&$top=2&$skip=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ODataResult<Product>>(JsonOpts);
        json!.Value.Should().HaveCount(2);
        json.Value[0].ProductId.Should().Be(2);
    }

    // ── $count ──

    [Fact]
    public async Task Count_ReturnsCount()
    {
        var response = await _client.GetAsync("/odata/TestCatalog/Products/$count");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Trim().Should().Be("3");
    }

    // ── POST ──

    [Fact]
    public async Task Post_CreatesNewProduct()
    {
        var newProduct = new { ProductId = 100, Name = "NewProduct", Price = 15.0m, Category = "New" };
        var response = await _client.PostAsJsonAsync("/odata/TestCatalog/Products", newProduct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify it exists
        var get = await _client.GetAsync("/odata/TestCatalog/Products(100)");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── PUT ──

    [Fact]
    public async Task Put_ReplacesProduct()
    {
        var updated = new { ProductId = 2, Name = "UpdatedGizmo", Price = 29.99m, Category = "Updated" };
        var response = await _client.PutAsJsonAsync("/odata/TestCatalog/Products(2)", updated);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetFromJsonAsync<Product>("/odata/TestCatalog/Products(2)", JsonOpts);
        get!.Name.Should().Be("UpdatedGizmo");
    }

    [Fact]
    public async Task Put_DeniedProperty_ReturnsBadRequest()
    {
        // CreatedBy is marked DenyPut = true — changing it should fail
        var updated = new { OrderId = 1, ProductId = 1, Quantity = 10, CreatedBy = "hacker" };
        var response = await _client.PutAsJsonAsync("/odata/TestCatalog/Orders(1)", updated);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_DeniedProperty_SameValue_Succeeds()
    {
        // CreatedBy unchanged — Put should succeed
        var updated = new { OrderId = 1, ProductId = 1, Quantity = 10, CreatedBy = "test" };
        var response = await _client.PutAsJsonAsync("/odata/TestCatalog/Orders(1)", updated);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── DELETE ──

    [Fact]
    public async Task Delete_RemovesProduct()
    {
        var response = await _client.DeleteAsync("/odata/TestCatalog/Products(3)");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync("/odata/TestCatalog/Products(3)");
        // Should be Not Found or empty result
        get.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.NoContent);
    }

    // ── Ignored entity ──

    [Fact]
    public async Task IgnoredEntity_NotAccessible()
    {
        var response = await _client.GetAsync("/odata/TestCatalog/AuditLogs");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

/// <summary>Helper for deserializing OData collection responses.</summary>
internal class ODataResult<T>
{
    public List<T> Value { get; set; } = new();
}
