using CashFlow.Event;

namespace CashFlow.DAL;
public interface IRedisDailyCashFlowDAL
{
    public void updateBalanceValue(DailyCashFlowEvent DailyCashFlowEvent);
    public Double readBalanceValue();

}