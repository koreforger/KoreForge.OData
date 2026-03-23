namespace KF.OData.Configuration;

/// <summary>
/// Configuration options for KoreForge OData registration.
/// </summary>
public sealed class KoreForgeODataOptions
{
    /// <summary>
    /// Maximum page size for OData queries. Default: 100.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Maximum expand depth for $expand queries. Default: 3.
    /// </summary>
    public int MaxExpandDepth { get; set; } = 3;

    /// <summary>
    /// Maximum number of allowed query nodes. Default: 100.
    /// </summary>
    public int MaxNodeCount { get; set; } = 100;

    /// <summary>
    /// Whether to enable $count query option. Default: true.
    /// </summary>
    public bool EnableCount { get; set; } = true;

    /// <summary>
    /// Whether to enable $filter query option. Default: true.
    /// </summary>
    public bool EnableFilter { get; set; } = true;

    /// <summary>
    /// Whether to enable $orderby query option. Default: true.
    /// </summary>
    public bool EnableOrderBy { get; set; } = true;

    /// <summary>
    /// Whether to enable $select query option. Default: true.
    /// </summary>
    public bool EnableSelect { get; set; } = true;

    /// <summary>
    /// Whether to enable $expand query option. Default: true.
    /// </summary>
    public bool EnableExpand { get; set; } = true;

    /// <summary>
    /// Route prefix for OData endpoints. Default: "odata".
    /// </summary>
    public string RoutePrefix { get; set; } = "odata";
}
