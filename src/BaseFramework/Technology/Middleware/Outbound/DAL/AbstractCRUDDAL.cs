using System.ComponentModel;
using System.Data.Entity;
using System.Linq.Expressions;
using BaseFramework.Technology.Middleware.Outbound.DAL;
using BaseFramework.Technology.Util;

namespace BaseFramework.DAL;

public abstract class AbstractCRUDDAL : Component, IDAL<IORMEntity>
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
        //string _projectId = "DailyCashFlowBalance";

        //string _database = VaultCredentialUtility.GetSecretAsync(_projectId, "DATABASE").GetAwaiter().GetResult(); 
        //string _userDB = VaultCredentialUtility.GetSecretAsync(_projectId, "USER_DATABASE").GetAwaiter().GetResult(); 
        //string _passwordDB = VaultCredentialUtility.GetSecretAsync(_projectId, "PASSWORD_DATABASE").GetAwaiter().GetResult();

        //string sqlConnectionString = $"Server=GCP_CLOUD_SQL\\SQLEXPRESS;Database={_database};User Id={_userDB};Password={_passwordDB};Trusted_Connection=True;TrustServerCertificate=True;";
        //session = new(sqlConnectionString);
    }
        
    public bool Add(IORMEntity entity)  
    {
        if (!IsValid(entity))
            return false;
        try
        {
            t = session.Database.BeginTransaction();
            session.Set(typeof(IORMEntity)).Add(entity);
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

    public bool Delete(IORMEntity entity)
    {
        try
        {
            t = session.Database.BeginTransaction();
            session.Set(typeof(IORMEntity)).Remove(entity);
            //.Set(typeof(IORMEntity)).Add(entity);
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

    public bool Update(IORMEntity entity)
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



    public IList<IORMEntity> GetAll()
    {
        throw new NotImplementedException();
    }

    public IList<IORMEntity> GetAll(string[] include)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        try
        {
            t = session.Database.BeginTransaction();
            //session.Set(typeof(IORMEntity)).Add(entity);
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

    public IList<IORMEntity> GetAll<IORMEntity>() 
    {
        //return session.Set<IORMEntity>().ToList();
        //return session.Set<(typeof(IORMEntity))>.ToList();
        return (IList<IORMEntity>)(session.Set(typeof(IORMEntity))).Find();
        //throw new NotImplementedException();
    }

    public IList<IORMEntity> GetAll<IORMEntity>(string[] include)
    {
        throw new NotImplementedException();
    }

  
    public IList<IORMEntity> GetAll<IORMEntity>(System.Linq.Expressions.Expression<Func<IORMEntity, bool>> predicate) 
    {
        throw new NotImplementedException();
    }

    public bool IsValid(IORMEntity entity)
    {
        return true;
    }

}


