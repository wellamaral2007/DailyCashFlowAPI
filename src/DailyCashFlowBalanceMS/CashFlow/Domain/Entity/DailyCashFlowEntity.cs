using Domain;

namespace DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
public class DailyCashFlowEntity: IEntity
{
    public String? Id { get ; set ; }
    public Decimal CreditDebitValue { get ; set ; }
    public DateTime ThowDate { get ; set ; }

    public String? EventType { get ; set ; }
}    
