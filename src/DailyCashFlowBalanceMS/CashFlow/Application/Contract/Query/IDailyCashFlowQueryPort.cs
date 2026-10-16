namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
public interface IDailyCashFlowQueryPort
{
    public Task<decimal> readBalanceValue(DateTime date);
}