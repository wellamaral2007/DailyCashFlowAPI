using System.ComponentModel;
using System.Data.Entity;

namespace Base.DAL;

public abstract class AbstractCRUDDAL: Component
{
    protected DbContext session
    {
        get
        {
            if (session == null)
                throw new InvalidOperationException("A session IUnitOfWork do repositório não está instanciada.");
            return (session);
        }
    }

    public virtual DbContext Context
    {
        get
        {
            return session;
        }
    }

    public AbstractCRUDDAL()
        : base()
    {
    }



    public bool Add<TEntity>(TEntity entity) where TEntity : class 
    {
        if (!IsValid(entity))
            return false;
        try
        {
            session.Set(typeof(TEntity)).Add(entity);
            return session.Entry(entity).GetValidationResult().IsValid;
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                throw new Exception(ex.InnerException.Message, ex);
            throw new Exception(ex.Message, ex);
        }
    }

    public bool Delete<TEntity>(TEntity entity) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public IList<TEntity> GetAll<TEntity>() where TEntity : class
    {
        return session.Set<TEntity>().ToList();
    }

    public IList<TEntity> GetAll<TEntity>(string[] include) where TEntity : class
    {
        throw new NotImplementedException();
    }

  
    public IList<TEntity> GetAll<TEntity>(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public bool IsValid<TEntity>(TEntity entity) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public bool Update<TEntity>(TEntity entity) where TEntity : class
    {
        if (!IsValid(entity))
            return false;
        try
        {
            session.Set(typeof(TEntity)).Add(entity);
            return session.Entry(entity).GetValidationResult().IsValid;
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                throw new Exception(ex.InnerException.Message, ex);
            throw new Exception(ex.Message, ex);
        }
    }


    
}

