using Base.DAL;

namespace CashFlow.Entity;
public class DailyCashFlowEntity: IEntity
{
    public String? Id { get ; set ; }
    public Double CreditDebitValue { get ; set ; }
    public DateTime ThowDate { get ; set ; }
}    
