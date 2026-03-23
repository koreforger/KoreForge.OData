using KF.OData.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace KF.OData.Controllers;

/// <summary>
/// Base OData controller providing full CRUD with authorization hooks, row-level filtering,
/// and property restriction enforcement. Source-generated controllers inherit from this.
/// </summary>
/// <typeparam name="TContext">The <see cref="DbContext"/> type.</typeparam>
/// <typeparam name="TEntity">The entity type (must have a single primary key).</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class KoreForgeODataController<TContext, TEntity, TKey> : ODataController
    where TContext : DbContext
    where TEntity : class
{
    private readonly TContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IRowLevelFilterProvider<TEntity>? _rowFilter;
    private readonly ODataEntityAuthorizationInfo? _authInfo;
    private readonly IReadOnlySet<string> _patchDenied;
    private readonly IReadOnlySet<string> _putDenied;

    protected KoreForgeODataController(
        TContext context,
        IAuthorizationService authorizationService,
        IRowLevelFilterProvider<TEntity>? rowFilter = null)
    {
        _context = context;
        _authorizationService = authorizationService;
        _rowFilter = rowFilter;
        _authInfo = ODataEntityAuthorizationInfo.FromEntityType(typeof(TEntity));
        _patchDenied = PropertyRestrictionResolver.GetPatchDeniedProperties(typeof(TEntity));
        _putDenied = PropertyRestrictionResolver.GetPutDeniedProperties(typeof(TEntity));
    }

    protected TContext DbContext => _context;

    /// <summary>Returns the <see cref="DbSet{TEntity}"/> for the entity.</summary>
    protected abstract DbSet<TEntity> EntitySet { get; }

    /// <summary>Extracts the primary key value from the entity.</summary>
    protected abstract TKey GetKey(TEntity entity);

    /// <summary>Finds an entity by primary key.</summary>
    protected abstract Task<TEntity?> FindByKeyAsync(TKey key, CancellationToken ct);

    // ── Query hooks (override in partial controller extensions) ──

    protected virtual void OnBeforeQuery(ref IQueryable<TEntity> query) { }
    protected virtual void OnBeforeSingleResult(ref IQueryable<TEntity> query) { }

    // ── CUD hooks ──

    protected virtual void OnBeforeCreate(TEntity entity) { }
    protected virtual void OnAfterCreate(TEntity entity) { }
    protected virtual void OnBeforeReplace(TEntity existing, TEntity incoming) { }
    protected virtual void OnAfterReplace(TEntity entity) { }
    protected virtual void OnBeforePatch(TEntity entity, Delta<TEntity> delta) { }
    protected virtual void OnAfterPatch(TEntity entity) { }
    protected virtual void OnBeforeDelete(TEntity entity) { }
    protected virtual void OnAfterDelete(TEntity entity) { }

    // ── CRUD actions (OData convention routing — method names are the convention) ──

    [EnableQuery]
    public virtual IActionResult Get()
    {
        IQueryable<TEntity> query = EntitySet.AsNoTracking();
        if (_rowFilter is not null)
            query = _rowFilter.ApplyFilter(query);
        OnBeforeQuery(ref query);
        return Ok(query);
    }

    [EnableQuery]
    public virtual async Task<IActionResult> Get(TKey key)
    {
        if (!await AuthorizeAsync(ODataOperation.Read))
            return Forbid();

        IQueryable<TEntity> query = EntitySet.AsNoTracking()
            .Where(BuildKeyPredicate(key));
        if (_rowFilter is not null)
            query = _rowFilter.ApplyFilter(query);
        OnBeforeSingleResult(ref query);
        return Ok(SingleResult.Create(query));
    }

    public virtual async Task<IActionResult> Post([FromBody] TEntity entity, CancellationToken ct)
    {
        if (!await AuthorizeAsync(ODataOperation.Create))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        OnBeforeCreate(entity);
        EntitySet.Add(entity);
        await _context.SaveChangesAsync(ct);
        OnAfterCreate(entity);

        return Created(entity);
    }

    public virtual async Task<IActionResult> Put(TKey key, [FromBody] TEntity update, CancellationToken ct)
    {
        if (!await AuthorizeAsync(ODataOperation.Update))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!EqualityComparer<TKey>.Default.Equals(key, GetKey(update)))
            return BadRequest("Key mismatch.");

        var existing = await FindByKeyAsync(key, ct);
        if (existing is null)
            return NotFound();

        OnBeforeReplace(existing, update);
        _context.Entry(existing).CurrentValues.SetValues(update);
        await _context.SaveChangesAsync(ct);
        OnAfterReplace(existing);

        return Updated(existing);
    }

    public virtual async Task<IActionResult> Patch(TKey key, [FromBody] Delta<TEntity> delta, CancellationToken ct)
    {
        if (!await AuthorizeAsync(ODataOperation.Update))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = await FindByKeyAsync(key, ct);
        if (entity is null)
            return NotFound();

        // Enforce property restrictions
        if (_patchDenied.Count > 0)
        {
            var changedProps = delta.GetChangedPropertyNames();
            foreach (var prop in changedProps)
            {
                if (_patchDenied.Contains(prop))
                    return BadRequest($"Property '{prop}' cannot be modified via PATCH.");
            }
        }

        OnBeforePatch(entity, delta);
        delta.Patch(entity);
        await _context.SaveChangesAsync(ct);
        OnAfterPatch(entity);

        return Updated(entity);
    }

    public virtual async Task<IActionResult> Delete(TKey key, CancellationToken ct)
    {
        if (!await AuthorizeAsync(ODataOperation.Delete))
            return Forbid();

        var entity = await FindByKeyAsync(key, ct);
        if (entity is null)
            return NotFound();

        OnBeforeDelete(entity);
        EntitySet.Remove(entity);
        await _context.SaveChangesAsync(ct);
        OnAfterDelete(entity);

        return NoContent();
    }

    // ── Private helpers ──

    /// <summary>Builds a predicate expression to match the entity by key. Must be overridden by generated controllers.</summary>
    protected abstract System.Linq.Expressions.Expression<Func<TEntity, bool>> BuildKeyPredicate(TKey key);

    private async Task<bool> AuthorizeAsync(ODataOperation operation)
    {
        if (_authInfo is null)
            return true;

        return await _authInfo.IsAuthorizedAsync(_authorizationService, User, operation);
    }
}
