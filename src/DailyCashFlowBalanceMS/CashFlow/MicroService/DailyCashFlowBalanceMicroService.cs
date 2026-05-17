
using BaseFramework.DAL.DailyCashFlow;
using BaseFramework.MicroService;
using BaseFramework.MicroService.DailyCashFlow;
using Polly;
using Polly.CircuitBreaker;


namespace DailyCashFlowBalanceMS.CashFlow.MicroService
{

/*******************************
Microservice logic with CQRS 
read operations just - 
*******************************/
public class DailyCashFlowBalanceMicroService : AbstractMicroService, IDailyCashFlowBalanceMS
{    

    private readonly IRedisDailyCashFlowDAL RedisDailyCashFlowDAL;
    private readonly ISQLDailyCashFlowDAL SQLDailyCashFlowDAL;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public DailyCashFlowBalanceMicroService()
    {
        RedisDailyCashFlowDAL = new RedisDailyCashFlowDAL();
        SQLDailyCashFlowDAL = new SQLDailyCashFlowDAL();
        
        // Configure Circuit Breaker: Open circuit after 3 consecutive failures, wait 30 seconds
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, timespan) => { Console.WriteLine($"[DailyCashFlowBalanceMS CIRCUIT BREAKER] Opened! Reason: {ex.Message}"); },
                onReset: () => { Console.WriteLine("[DailyCashFlowBalanceMS CIRCUIT BREAKER] Closed! Redis is back online."); }
            ); 
    }


   /*******************************
    Take current Key-Value of
    BalanceValue State in Redis, 
    if not available or Redis is 
    down, fallback to SQL Server
    for report return
    *******************************/
    public async Task<decimal> ObtainDailyBalance(DateTime date)
    {
        // Execute the call via the circuit breaker
        return (decimal)await _circuitBreaker.ExecuteAsync(async () =>
        {
            try
            {
                // Try fetching from RedisDAL
                var cachedValue = await RedisDailyCashFlowDAL.readBalanceValue(date);
                if (cachedValue != 0) return cachedValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REDIS ERROR] {ex.Message}. Routing to SQL...");
                // Rethrow to trigger the circuit breaker on Redis failure
                throw; 
            }

            // Fallback if Redis is empty / returns null (not a server failure)
            return await SQLDailyCashFlowDAL.readBalanceValue(date);
        }); 
    }
 
}

}