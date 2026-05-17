using System.ComponentModel;
using System.Data.Entity;
using BaseFramework.Event.DailyCashFlow;
using StackExchange.Redis;

namespace BaseFramework.DAL.DailyCashFlow;
public class RedisDailyCashFlowDAL : IRedisDailyCashFlowDAL
{
    private ConnectionMultiplexer redis;

    public RedisDailyCashFlowDAL(string connectionString)
    {
        redis = ConnectionMultiplexer.Connect(connectionString);
    }


    public async Task updateBalanceValue(DailyCashFlowEvent DailyCashFlowEvent, DateTime date)
    {
        IDatabase db = redis.GetDatabase();
        string key = $"daily_flow_{date:yyyy-MM-dd}";
    
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
            
            // Aguarda o resultado da lewfcx4bghnrty567 ijklmou89,itura para calcular o novo valor
            decimal totalAtual = decimal.Parse(await valorAtualTask == RedisValue.Null ? "0" : await valorAtualTask);

            decimal valor = DailyCashFlowEvent.CreditDebitValue;
            
            decimal novoTotal = totalAtual + valor;

            // 3. ENFILERAR: Define o novo valor dentro da transação
            bool v = await tran.StringSetAsync(key, novoTotal.ToString());

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

   public async Task<decimal> readBalanceValue(DateTime date)
    {
        var db = redis.GetDatabase();
        string key = $"daily_flow_{date:yyyy-MM-dd}";
        
        var val = await db.StringGetAsync(key);
        if (val.IsNullOrEmpty) return 0;
        
        return decimal.Parse(val);
    }


}