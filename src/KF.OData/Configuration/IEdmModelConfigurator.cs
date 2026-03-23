using Microsoft.OData.ModelBuilder;

namespace KF.OData.Configuration;

/// <summary>
/// Defines a contract for types that register entity sets into the OData EDM model.
/// Implement this interface per DbContext (typically source-generated) to register entity sets.
/// </summary>
public interface IEdmModelConfigurator
{
    /// <summary>
    /// The name prefix for the OData route (typically the DbContext name without "Context" suffix).
    /// Used to build routes like /odata/{ContextPrefix}/{EntitySet}.
    /// </summary>
    string ContextPrefix { get; }

    /// <summary>
    /// Configures the EDM model builder with entity sets for this context.
    /// </summary>
    void Configure(ODataConventionModelBuilder builder);
}
