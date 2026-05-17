namespace BaseFramework.Event.DailyCashFlow;

public class DailyCashFlowEvent 
{    
    public decimal CreditDebitValue { get ; set ; }
    public DateTime ThowDate { get ; set ; }
    public String? EventType { get ; set ; }
}