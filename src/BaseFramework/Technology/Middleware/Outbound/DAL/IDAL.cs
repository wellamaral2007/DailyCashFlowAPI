namespace BaseFramework.DAL;
public interface IDAL<TEntity> : IDisposable where TEntity : class
{
    Task<IList<TEntity>> GetAllAsync();
    Task<IList<TEntity>> GetAllAsync(string[] include);
    //Task<IList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate);

    Task<bool> AddAsync(TEntity entity);
    Task<bool> DeleteAsync(TEntity entity);
    Task<bool> UpdateAsync(TEntity entity);
    Task<bool> IsValidAsync(TEntity entity);
}