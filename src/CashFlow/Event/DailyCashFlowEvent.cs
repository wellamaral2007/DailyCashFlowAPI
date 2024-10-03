namespace CashFlow.Event;

public class DailyCashFlowEvent 
{    
    public Double CreditDebitValue { get ; set ; }
    public DateTime ThowDate { get ; set ; }
    public String? EventType { get ; set ; }
}