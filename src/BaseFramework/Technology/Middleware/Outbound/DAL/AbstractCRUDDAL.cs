using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BaseFramework.Technology.Middleware.Outbound.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BaseFramework.DAL;

/// <summary>
/// Abstract data access layer component implementing generic CRUD mechanisms using Entity Framework Core.
/// </summary>
public abstract class AbstractCRUDDAL : DbContext, IDAL<IORMEntity>
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of AbstractCRUDDAL with the target connection string.
    /// </summary>
    protected AbstractCRUDDAL(string connectionString) : base()
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Configures the database context options, establishing MySql connection and auto-detecting server version.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));
        }
    }

    /// <summary>
    /// Configures the database schema metadata mapping, setting up specific column types for optimistic concurrency properties.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (string.Equals(property.Name, "rowversion", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetColumnType("timestamp");
                }
            }
        }
    }

    /// <summary>
    /// Persists a new tracking record into the database utilizing standard atomic transacting capabilities.
    /// </summary>
    public async Task<bool> AddAsync(IORMEntity entity)  
    {
        if (!await IsValidAsync(entity)) return false;
        try
        {
            await base.AddAsync(entity); 
            await this.SaveChangesAsync(); 
            return true;
        }
        catch (Exception ex) { throw NormalizeException(ex); }
    }

    /// <summary>
    /// Removes a tracking record from the current database context and applies mutations.
    /// </summary>
    public async Task<bool> DeleteAsync(IORMEntity entity)
    {
        try
        {
            base.Remove(entity);
            await this.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { throw NormalizeException(ex); }
    }

    /// <summary>
    /// Updates an existing tracking record within the database context and persists changes.
    /// </summary>
    public async Task<bool> UpdateAsync(IORMEntity entity)
    {
        if (!await IsValidAsync(entity)) return false;
        try
        {
            base.Update(entity);
            await this.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { throw NormalizeException(ex); }
    }

    /// <summary>
    /// Evaluates if the target data transfer object or model satisfies initial reference boundary constraints.
    /// </summary>
    public Task<bool> IsValidAsync(IORMEntity entity)
    {
        return Task.FromResult(entity != null);
    }

    /// <summary>
    /// Abstract collection extraction point. Throws an exception forcing strongly-typed implementations on inheritance.
    /// </summary>
    public async Task<IList<IORMEntity>> GetAllAsync()
    {
        throw new NotImplementedException("Use the protected 'GetByQueryAsync<TEntity>' method in child DAL classes for strongly-typed data reads.");
    }

    /// <summary>
    /// Abstract relational collection extraction point supporting object relationship queries.
    /// </summary>
    public async Task<IList<IORMEntity>> GetAllAsync(string[] include)
    {
        throw new NotImplementedException("Use the protected 'GetByQueryAsync<TEntity>' method in child DAL classes for strongly-typed data reads.");
    }

    /// <summary>
    /// Synchronous block handler routing all data extraction operations through the asynchronous method pipeline.
    /// </summary>
    public IList<IORMEntity> GetAll() => GetAllAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Synchronous block handler routing all relation data extraction operations through the asynchronous method pipeline.
    /// </summary>
    public IList<IORMEntity> GetAll(string[] include) => GetAllAsync(include).GetAwaiter().GetResult();

    /// <summary>
    /// Extracts a collection filter slice using custom expressions directly mapping against specific model definitions.
    /// </summary>
    protected async Task<IList<TEntity>> GetByQueryAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        return await this.Set<TEntity>().Where(predicate).AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Wraps structural driver or engine exceptions to decouple raw internal details.
    /// </summary>
    private Exception NormalizeException(Exception ex)
    {
        if (ex.InnerException != null) return new Exception(ex.InnerException.Message, ex);
        return new Exception(ex.Message, ex);
    }
}
