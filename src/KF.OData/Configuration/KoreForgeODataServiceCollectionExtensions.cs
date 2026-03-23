using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.ModelBuilder;

namespace KF.OData.Configuration;

/// <summary>
/// Extension methods for registering KoreForge OData services in the DI container.
/// </summary>
public static class KoreForgeODataServiceCollectionExtensions
{
    /// <summary>
    /// Registers KoreForge OData services with default options.
    /// Call this after registering all <see cref="IEdmModelConfigurator"/> implementations.
    /// </summary>
    public static IMvcBuilder AddKoreForgeOData(
        this IMvcBuilder mvcBuilder,
        Action<KoreForgeODataOptions>? configureOptions = null)
    {
        var options = new KoreForgeODataOptions();
        configureOptions?.Invoke(options);

        mvcBuilder.Services.AddSingleton(options);

        // Build one EDM model per registered configurator, each mapped to its own route prefix.
        mvcBuilder.AddOData(odata =>
        {
            var configurators = mvcBuilder.Services
                .BuildServiceProvider()
                .GetServices<IEdmModelConfigurator>();

            foreach (var configurator in configurators)
            {
                var modelBuilder = new ODataConventionModelBuilder();
                configurator.Configure(modelBuilder);
                var model = modelBuilder.GetEdmModel();

                var routePrefix = $"{options.RoutePrefix}/{configurator.ContextPrefix}";

                odata.AddRouteComponents(routePrefix, model)
                     .Count()
                     .Filter()
                     .OrderBy()
                     .Select()
                     .Expand()
                     .SetMaxTop(options.MaxPageSize);
            }
        });

        return mvcBuilder;
    }

    /// <summary>
    /// Registers an <see cref="IEdmModelConfigurator"/> implementation in the DI container.
    /// </summary>
    public static IServiceCollection AddEdmModelConfigurator<TConfigurator>(this IServiceCollection services)
        where TConfigurator : class, IEdmModelConfigurator
    {
        services.AddSingleton<IEdmModelConfigurator, TConfigurator>();
        return services;
    }
}
