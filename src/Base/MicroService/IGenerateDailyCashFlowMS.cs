using CashFlow.Event;

namespace Base.MicroService;

public interface IGenerateDailyCashFlowMS  
{    
    public void GenerateDailyCashEvent(DailyCashFlowEvent DailyCashFlowEvent);        
}