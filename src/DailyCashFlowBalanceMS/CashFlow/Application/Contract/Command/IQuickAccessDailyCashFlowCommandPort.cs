using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
public interface IQuickAccessDailyCashFlowCommandPort
{
    public Task updateBalanceValue(DailyCashFlowEntity DailyCashFlowEntity, DateTime date);

}