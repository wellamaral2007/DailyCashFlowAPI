using BaseFramework.Event.DailyCashFlow;   

namespace BaseFramework.DAL.DailyCashFlow;
public interface ISQLDailyCashFlowDAL
{
    public Task insertCreditDebit(DailyCashFlowEvent DailyCashFlowEvent);

    public Task<decimal> readBalanceValue(DateTime date);

}