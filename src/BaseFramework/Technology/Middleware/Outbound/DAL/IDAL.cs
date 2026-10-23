using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaseFramework.DAL;

/// <summary>
/// Generic asynchronous data access layer contract enforcing decoupled CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The target class tracking record database type mapping.</typeparam>
public interface IDAL<TEntity> : IDisposable where TEntity : class
{
    /// <summary>
    /// Fetches all active entity records from the current storage context.
    /// </summary>
    Task<IList<TEntity>> GetAllAsync();

    /// <summary>
    /// Fetches all active entity records including specific related object graphs.
    /// </summary>
    Task<IList<TEntity>> GetAllAsync(string[] include);

    //Task<IList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Persists a new transient entity data instance into the underlying repository.
    /// </summary>
    Task<bool> AddAsync(TEntity entity);

    /// <summary>
    /// Permanently purges an existing tracked record from the storage context.
    /// </summary>
    Task<bool> DeleteAsync(TEntity entity);

    /// <summary>
    /// Updates the state data layout of an existing tracked entity record.
    /// </summary>
    Task<bool> UpdateAsync(TEntity entity);

    /// <summary>
    /// Evaluates structural business parameters to validate database tracking readiness.
    /// </summary>
    Task<bool> IsValidAsync(TEntity entity);
}
