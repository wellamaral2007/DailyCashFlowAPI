using CashFlow.Event;

namespace CashFlow.DAL;
public interface IDailyCashFlowDAL
{
    public void insertCreditDebit(DailyCashFlowEvent DailyCashFlowEvent);

    public void updateBalanceValue(DailyCashFlowEvent DailyCashFlowEvent);

    public Double readBalanceValue();

}