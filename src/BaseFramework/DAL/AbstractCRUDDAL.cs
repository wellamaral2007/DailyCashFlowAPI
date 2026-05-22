using System.ComponentModel;
using System.Data.Entity;
using System.Linq.Expressions;

namespace BaseFramework.DAL;

public abstract class AbstractCRUDDAL : Component, IDAL<IEntity>
{
    private DbContextTransaction t;


    protected DbContext session;

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
        string _projectId = "DailyCashFlowBalance";

        string _database = VaultCredentialUtility.GetSecretAsync(_projectId, "DATABASE").GetAwaiter().GetResult(); 
        string _userDB = VaultCredentialUtility.GetSecretAsync(_projectId, "USER_DATABASE").GetAwaiter().GetResult(); 
        string _passwordDB = VaultCredentialUtility.GetSecretAsync(_projectId, "PASSWORD_DATABASE").GetAwaiter().GetResult();

        string sqlConnectionString = $"Server=GCP_CLOUD_SQL\\SQLEXPRESS;Database={_database};User Id={_userDB};Password={_passwordDB};Trusted_Connection=True;TrustServerCertificate=True;";
        session = new(sqlConnectionString);
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


