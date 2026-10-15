using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
public interface IDurableDailyCashFlowCommandPort
{
    public Task insertCreditDebit(DailyCashFlowEntity DailyCashFlowEntity);

}