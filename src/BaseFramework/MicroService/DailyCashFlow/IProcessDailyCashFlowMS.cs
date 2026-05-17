using BaseFramework.Event.DailyCashFlow;

namespace BaseFramework.MicroService.DailyCashFlow;

public interface IProcessDailyCashFlowMS  
{    

    public void ProcessingDailyCashEvent(DailyCashFlowEvent DailyCashFlowEvent);
    
        
}