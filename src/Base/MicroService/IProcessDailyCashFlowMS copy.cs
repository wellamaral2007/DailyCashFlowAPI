using CashFlow.Event;

namespace Base.MicroService;

public interface IProcessDailyCashFlowMS  
{    

    public void ProcessingDailyCashEvent(DailyCashFlowEvent DailyCashFlowEvent);
    
        
}