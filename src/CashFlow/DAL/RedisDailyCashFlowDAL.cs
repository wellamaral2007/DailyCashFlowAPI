using System.ComponentModel;
using System.Data.Entity;
using Base.DAL;
using CashFlow.Entity;
using CashFlow.Event;
using StackExchange.Redis;

namespace CashFlow.DAL;
public class RedisDailyCashFlowDAL : IRedisDailyCashFlowDAL
{
    private ConnectionMultiplexer redis;

    public void updateBalanceValue(DailyCashFlowEvent DailyCashFlowEvent)
    {
        redis = ConnectionMultiplexer.Connect("gcp_host");
        IDatabase db = redis.GetDatabase();
        key = "BalanceValue";
    
        bool atualizado = false;
        int tentativas = 0;

        // Padrão de repetição (Retry) para transação (Optimistic Locking)
        while (!atualizado && tentativas < 5)
        {
            // 1. WATCH: Monitora a chave para ver se ela muda antes do EXEC
            // O StackExchange.Redis faz isso automaticamente ao criar a transação
            var tran = db.CreateTransaction();

            // 2. LER: Busca o valor atual
            var valorAtualTask = db.StringGetAsync(key);
            
            // Aguarda o resultado da leitura para calcular o novo valor
            double totalAtual = double.Parse(await valorAtualTask == RedisValue.Null ? "0" : await valorAtualTask);
            double novoTotal = totalAtual + valor;

            // 3. ENFILERAR: Define o novo valor dentro da transação
            _ = tran.StringSetAsync(key, novoTotal);

            // 4. EXECUTE: Tenta commitar a transação
            // Se a chave mudou entre a leitura e o commit, ExecuteAsync retorna false
            atualizado = await tran.ExecuteAsync();

            if (atualizado)
            {
                Console.WriteLine($"Sucesso! Data: {key}. Valor Transação: {valor}. Novo Total: {novoTotal}");
            }
            else
            {
                tentativas++;
                Console.WriteLine($"Concorrência detectada. Tentativa {tentativas}...");
                await Task.Delay(10); // Pequena pausa antes de tentar novamente
            }
        }
    }


    public Double readBalanceValue()
    {
        redis = ConnectionMultiplexer.Connect("gcp_host");
        // Get a reference to the Redis database
        IDatabase db = redis.GetDatabase();
        string value = db.StringGet("BalanceValue");
        // ... Your Redis operations go here ...
        // Disconnect from Redis
        redis.Close();
        return Convert.ToDouble(value);
    }

}