// File: apps/daily-cash-flow-service/src/DailyCashFlowService.API/3_Technology/Middleware/Outbound/Cache/RedisDailyCashFlowDALQuery.cs
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using StackExchange.Redis;
using Polly;
using Polly.CircuitBreaker;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - Cache Query
 * CONCEPTS:        High Performance Caching / Sub-millisecond Key-Value Reads
 * ============================================================================
 * This infrastructure component implements clean read-only caching mechanisms,
 * fetching high-speed data state projections completely isolated from the Core.
 */
public class RedisDailyCashFlowDALQuery : IDailyCashFlowQueryPort
{
    private readonly IConnectionMultiplexer _redis;
    private const string CacheKeyPrefix = "daily_flow_";
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    private SQLDailyCashFlowDALQuery _sqlDailyCashFlowDALQuery;

    /**
     * Dependency Injection via Centralized Connection Multiplexer Pool (IoC)
     */
    public RedisDailyCashFlowDALQuery(IConnectionMultiplexer redis, SQLDailyCashFlowDALQuery _sqlDALQuery)
    {
        _redis = redis;
        _sqlDailyCashFlowDALQuery = _sqlDALQuery ?? throw new ArgumentNullException(nameof(_sqlDALQuery));
        // Resilience Governance Configuration: Breaks the circuit after 3 consecutive failures, waits 30 seconds
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, timespan) => 
                { 
                    Console.WriteLine($"[DAILY CASH FLOW CIRCUIT BREAKER] Opened! Reason: {ex.Message}. Routing traffic directly to Durable storage."); 
                },
                onReset: () => 
                { 
                    Console.WriteLine("[DAILY CASH FLOW CIRCUIT BREAKER] Closed! QuickAccess middleware is back online."); 
                }
            ); 
    }

    /**
     * Executes a direct and optimized fetch of the consolidated net balance from memory.
     */
    public async Task<decimal> readBalanceValue(DateTime date)
    {
        try
        {
            // Executa a busca protegida pelo Circuit Breaker voltada ao Redis (QuickAccess)
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                try
                {
                    var db = _redis.GetDatabase();
                    string key = $"{CacheKeyPrefix}{date:yyyy-MM-dd}";
                    
                    var cachedValue = await db.StringGetAsync(key);
                    if (cachedValue.IsNullOrEmpty) 
                    {
                        // ATENÇÃO: Se for um Cache Miss, você deve lançar uma exceção customizada ou
                        // retornar um valor que indique que precisa buscar no SQL. 
                        // Se retornar 0.0m direto aqui, ele assume que o saldo é zero e pula o SQL.
                        throw new KeyNotFoundException("Cache Miss"); 
                    }
                    
                    return decimal.Parse(cachedValue.ToString());
                }
                catch (Exception ex) when (ex is not KeyNotFoundException)
                {
                    Console.WriteLine($"[QUICK ACCESS ERROR] {ex.Message}. Tripping Circuit Breaker.");
                    throw; // Repassa para o Polly contabilizar a falha de infraestrutura
                }
            });
        }
        catch (Exception ex) // CORREÇÃO: Captura tanto BrokenCircuitException quanto a falha inicial do Redis
        {
            if (ex is BrokenCircuitException)
            {
                Console.WriteLine("[FALLBACK CRITICAL] Circuit is open. Bypassing QuickAccess and querying Durable Query Port directly.");
            }
            else
            {
                Console.WriteLine($"[FALLBACK IMMEDIATE] Redis failed down: {ex.Message}. Routing immediately to Durable Query Port.");
            }

            // Executa a consulta de alta durabilidade no SQL usando a data correta do fuso local
            return await _sqlDailyCashFlowDALQuery.readBalanceValue(date);
        }

    }

    /**
     * Hydrates the fast-access read cache memory mapping (Cache Miss Hydration lifecycle)
     */
    public async Task SetCachedBalanceAsync(decimal balance, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        string key = $"{CacheKeyPrefix}{DateTime.UtcNow:yyyy-MM-dd}";
        await db.StringSetAsync(key, balance.ToString(), ttl);
    }
}
