using System.ComponentModel;
using System.Data.Entity;
using System.Linq.Expressions;

namespace Base.DAL 
{

public abstract class AbstractCRUDDAL : Component, IDAL<IEntity>
{
    private DbContextTransaction t;


    protected DbContext session
    {
        get
        {
            if (session == null)
            {

            }   
            //    throw new InvalidOperationException("A session IUnitOfWork do repositório não está instanciada.");
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
        
    public bool Add(IEntity entity)  
    {
        if (!IsValid(entity))
            return false;
        try
        {
            t = session.Database.BeginTransaction();
            session.Set(typeof(IEntity)).Add(entity);
            t.Commit();
            return session.Entry(entity).GetValidationResult().IsValid;

        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                throw new Exception(ex.InnerException.Message, ex);
            throw new Exception(ex.Message, ex);
        }
    }

    public bool Delete(IEntity entity)
    {
        try
        {
            t = session.Database.BeginTransaction();
            session.Set(typeof(IEntity)).Remove(entity);
            //.Set(typeof(IEntity)).Add(entity);
            t.Commit();
            return session.Entry(entity).GetValidationResult().IsValid;
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                throw new Exception(ex.InnerException.Message, ex);
            throw new Exception(ex.Message, ex);
        }
    }

    public bool Update(IEntity entity)
    {
        if (!IsValid(entity))
            return false;
        try
        {
            t = session.Database.BeginTransaction();
            //session.Set(typeof(TEntity)).Add(entity);
            session.SaveChanges();
            t.Commit();
            return session.Entry(entity).GetValidationResult().IsValid;
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                throw new Exception(ex.InnerException.Message, ex);
            throw new Exception(ex.Message, ex);
        }
    }



    public IList<IEntity> GetAll()
    {
        throw new NotImplementedException();
    }

    public IList<IEntity> GetAll(string[] include)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        try
        {
            t = session.Database.BeginTransaction();
            //session.Set(typeof(IEntity)).Add(entity);
            session.Dispose();
            t.Commit();
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
                throw new Exception(ex.InnerException.Message, ex);
            throw new Exception(ex.Message, ex);
        }
    }

    public IList<IEntity> GetAll<IEntity>() 
    {
        //return session.Set<IEntity>().ToList();
        //return session.Set<(typeof(IEntity))>.ToList();
        return (IList<IEntity>)(session.Set(typeof(IEntity))).Find();
        //throw new NotImplementedException();
    }

    public IList<IEntity> GetAll<IEntity>(string[] include)
    {
        throw new NotImplementedException();
    }

  
    public IList<IEntity> GetAll<IEntity>(System.Linq.Expressions.Expression<Func<IEntity, bool>> predicate) 
    {
        throw new NotImplementedException();
    }

    public bool IsValid(IEntity entity)
    {
        return true;
    }

}


}