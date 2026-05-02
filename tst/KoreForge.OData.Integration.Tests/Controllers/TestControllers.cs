using System.Linq.Expressions;
using KoreForge.OData.Controllers;
using KoreForge.OData.Integration.Tests.TestModel;
using KoreForge.OData.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoreForge.OData.Integration.Tests.Controllers;

public class ProductsController : KoreForgeODataController<TestCatalogContext, Product, int>
{
    public ProductsController(
        TestCatalogContext context,
        IAuthorizationService authorizationService,
        IRowLevelFilterProvider<Product>? rowFilter = null)
        : base(context, authorizationService, rowFilter) { }

    protected override DbSet<Product> EntitySet => DbContext.Products;
    protected override int GetKey(Product entity) => entity.ProductId;

    protected override Task<Product?> FindByKeyAsync(int key, CancellationToken ct)
        => DbContext.Products.FindAsync(new object[] { key }, ct).AsTask();

    protected override Expression<Func<Product, bool>> BuildKeyPredicate(int key)
        => e => e.ProductId == key;
}

public class OrdersController : KoreForgeODataController<TestCatalogContext, Order, int>
{
    public OrdersController(
        TestCatalogContext context,
        IAuthorizationService authorizationService,
        IRowLevelFilterProvider<Order>? rowFilter = null)
        : base(context, authorizationService, rowFilter) { }

    protected override DbSet<Order> EntitySet => DbContext.Orders;
    protected override int GetKey(Order entity) => entity.OrderId;

    protected override Task<Order?> FindByKeyAsync(int key, CancellationToken ct)
        => DbContext.Orders.FindAsync(new object[] { key }, ct).AsTask();

    protected override Expression<Func<Order, bool>> BuildKeyPredicate(int key)
        => e => e.OrderId == key;
}
