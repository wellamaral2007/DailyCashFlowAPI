using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BaseFramework.Technology.Middleware.Outbound.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BaseFramework.DAL;

public abstract class AbstractCRUDDAL : DbContext, IDAL<IORMEntity>
{
    private readonly string _connectionString;

    protected AbstractCRUDDAL(string connectionString) : base()
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));
        }
    }

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

        // ============================================================================
    // IMPLEMENTAÇÃO ASSÍNCRONA CORRIGIDA (RUNTIME TYPE RESOLUTION)
    // ============================================================================

    public async Task<bool> AddAsync(IORMEntity entity)  
    {
        if (!await IsValidAsync(entity)) return false;
        try
        {
            using var transaction = await this.Database.BeginTransactionAsync();
            
            // 🚀 CORREÇÃO: No EF Core, chamar o método AddAsync direto na raiz do DbContext
            // faz com que ele descubra e mapeie o tipo real (DailyCashFlowORM) automaticamente [1.1]!
            await base.AddAsync(entity); 
            
            await this.SaveChangesAsync(); 
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex) { throw NormalizeException(ex); }
    }

    public async Task<bool> DeleteAsync(IORMEntity entity)
    {
        try
        {
            using var transaction = await this.Database.BeginTransactionAsync();
            
            // Remove o registro usando o método de rastreamento do DbContext base [1.1]
            base.Remove(entity);
            
            await this.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex) { throw NormalizeException(ex); }
    }

    public async Task<bool> UpdateAsync(IORMEntity entity)
    {
        if (!await IsValidAsync(entity)) return false;
        try
        {
            using var transaction = await this.Database.BeginTransactionAsync();
            
            // Atualiza o registro usando o método de rastreamento do DbContext base [1.1]
            base.Update(entity);
            
            await this.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex) { throw NormalizeException(ex); }
    }


    public Task<bool> IsValidAsync(IORMEntity entity)
    {
        return Task.FromResult(entity != null);
    }

    // Como as leituras da interface IDAL exigem um retorno genérico de IORMEntity,
    // nós delegamos a busca mapeando dinamicamente através do tipo genérico passado pelo repositório filha.
    public async Task<IList<IORMEntity>> GetAllAsync()
    {
        throw new NotImplementedException("Utilize o método protegido 'GetByQueryAsync<TEntity>' nas classes DAL filhas para leitura strongly-typed.");
    }

    public async Task<IList<IORMEntity>> GetAllAsync(string[] include)
    {
        throw new NotImplementedException("Utilize o método protegido 'GetByQueryAsync<TEntity>' nas classes DAL filhas para leitura strongly-typed.");
    }

    public IList<IORMEntity> GetAll() => GetAllAsync().GetAwaiter().GetResult();
    public IList<IORMEntity> GetAll(string[] include) => GetAllAsync(include).GetAwaiter().GetResult();

    // ============================================================================
    // FACILITADORES PROTEGIDOS FORTEMENTE TIPADOS PARA AS DALs FILHAS
    // ============================================================================
    
    protected async Task<IList<TEntity>> GetByQueryAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        return await this.Set<TEntity>().Where(predicate).AsNoTracking().ToListAsync();
    }

    private Exception NormalizeException(Exception ex)
    {
        if (ex.InnerException != null) return new Exception(ex.InnerException.Message, ex);
        return new Exception(ex.Message, ex);
    }
}
