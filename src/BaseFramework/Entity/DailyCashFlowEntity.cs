using BaseFramework.DAL;

namespace BaseFramework.Entity;
public class DailyCashFlowEntity: IEntity
{
    public String? Id { get ; set ; }
    public Double CreditDebitValue { get ; set ; }
    public DateTime ThowDate { get ; set ; }
}    
