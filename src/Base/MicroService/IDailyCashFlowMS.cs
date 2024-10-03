using CashFlow.Event;

namespace Base.MicroService;

public interface IDailyCashFlowMS  
{    

    public void ProcessingDailyCashEvent(DailyCashFlowEvent DailyCashFlowEvent);
    
    public double ObtainDailyBalance();


        
}