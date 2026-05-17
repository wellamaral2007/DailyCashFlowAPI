using BaseFramework.Event.DailyCashFlow;

namespace BaseFramework.MicroService.DailyCashFlow;

public interface IGenerateDailyCashFlowMS  
{    
    public Task GenerateDailyCashEvent(decimal BalanceValue);        
}