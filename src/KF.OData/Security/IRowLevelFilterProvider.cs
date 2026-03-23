namespace KF.OData.Security;

/// <summary>
/// Provides row-level query filtering for OData entities.
/// Implement this interface and register it in DI to apply per-user or per-tenant filtering.
/// </summary>
/// <typeparam name="TEntity">The entity type to filter.</typeparam>
public interface IRowLevelFilterProvider<TEntity> where TEntity : class
{
    /// <summary>
    /// Applies row-level filtering to the query. Called before every read operation.
    /// </summary>
    IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query);
}
