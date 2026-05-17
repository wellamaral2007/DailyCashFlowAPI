using BaseFramework.Event.DailyCashFlow;   

namespace BaseFramework.DAL.DailyCashFlow;
public interface ISQLDailyCashFlowDAL
{
    public void insertCreditDebit(DailyCashFlowEvent DailyCashFlowEvent);

    public Task<decimal> readBalanceValue(DateTime date);

}