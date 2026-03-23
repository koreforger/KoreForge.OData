using System.ComponentModel.DataAnnotations;
using KF.OData.Attributes;

namespace KF.OData.Integration.Tests.TestModel;

public class Product
{
    [Key]
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Category { get; set; }
}

[ODataAuthorize(ReadPolicy = "CanReadOrders", CreatePolicy = "CanCreateOrders")]
public class Order
{
    [Key]
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [ODataPropertyRestriction(DenyPatch = true)]
    public string CreatedBy { get; set; } = "system";
}

[ODataIgnore]
public class AuditLog
{
    [Key]
    public int LogId { get; set; }
    public string Message { get; set; } = string.Empty;
}
