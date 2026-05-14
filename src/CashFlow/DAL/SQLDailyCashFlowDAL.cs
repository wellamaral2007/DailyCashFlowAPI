using System.ComponentModel;
using System.Data.Entity;
using Base.DAL;
using CashFlow.Entity;
using CashFlow.Event;
using StackExchange.Redis;

namespace CashFlow.DAL;
public class SQLDailyCashFlowDAL : AbstractCRUDDAL, ISQLDailyCashFlowDAL
{
    public void insertCreditDebit(DailyCashFlowEvent DailyCashFlowEvent)
    {
        DailyCashFlowEntity entity = new DailyCashFlowEntity();
        bool v = Add(entity);
    }

    


}