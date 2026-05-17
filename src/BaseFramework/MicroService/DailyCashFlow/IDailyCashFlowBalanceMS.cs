
namespace BaseFramework.MicroService.DailyCashFlow;

public interface IDailyCashFlowBalanceMS  
{    
    public Task<decimal> ObtainDailyBalance(DateTime date);
}