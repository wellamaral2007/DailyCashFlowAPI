namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
public interface IQuickAccessDailyCashFlowQueryPort
{
    public Task<decimal> readBalanceValue(DateTime date);
}