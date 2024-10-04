using System.ComponentModel;
using System.Data.Entity;
using Base.DAL;
using CashFlow.Entity;
using CashFlow.Event;
using StackExchange.Redis;

namespace CashFlow.DAL;
public class DailyCashFlowDAL : AbstractCRUDDAL
{
    private ConnectionMultiplexer redis;

    public void insertCreditDebit(DailyCashFlowEvent DailyCashFlowEvent)
    {
        DailyCashFlowEntity entity = new DailyCashFlowEntity();
        bool v = Add(entity);
    }

    public void updateBalanceValue(DailyCashFlowEvent DailyCashFlowEvent)
    {
      redis = ConnectionMultiplexer.Connect("gcp_host");
      IDatabase db = redis.GetDatabase();
      ITransaction transaction = db.CreateTransaction();
      string value = db.StringGet("BalanceValue");
      decimal newBalanceValue = ((decimal)Convert.ToDouble(value)) + ((decimal)DailyCashFlowEvent.CreditDebitValue());
      db.StringSet("BalanceValue", newBalanceValue.ToString());
      
    }

    public Double readBalanceValue()
    {
        redis = ConnectionMultiplexer.Connect("gcp_host");
        // Get a reference to the Redis database
        IDatabase db = redis.GetDatabase();
        string value = db.StringGet("BalanceValue");
        // ... Your Redis operations go here ...
        // Disconnect from Redis
        redis.Close();
        return Convert.ToDouble(value);
    }

}