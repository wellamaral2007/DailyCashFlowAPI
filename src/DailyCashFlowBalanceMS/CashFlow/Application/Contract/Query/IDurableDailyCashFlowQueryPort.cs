namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
public interface IDurableDailyCashFlowQueryPort
{
    public Task<decimal> readBalanceValue(DateTime date);

}