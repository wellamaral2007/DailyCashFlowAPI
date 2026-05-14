using CashFlow.Event;

namespace CashFlow.DAL;
public interface ISQLDailyCashFlowDAL
{
    public void insertCreditDebit(DailyCashFlowEvent DailyCashFlowEvent);

}