using BaseFramework.Event.DailyCashFlow;

namespace BaseFramework.DAL.DailyCashFlow;
public interface IRedisDailyCashFlowDAL
{
    public Task updateBalanceValue(DailyCashFlowEvent DailyCashFlowEvent, DateTime date);
    public Task<decimal> readBalanceValue(DateTime date);
}