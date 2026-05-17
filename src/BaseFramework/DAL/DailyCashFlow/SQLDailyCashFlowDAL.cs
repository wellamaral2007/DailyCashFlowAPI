using System.ComponentModel;
using System.Data.Entity;
using System.Data.SqlClient;
using BaseFramework.Entity;
using BaseFramework.Event.DailyCashFlow;
using StackExchange.Redis;

namespace BaseFramework.DAL.DailyCashFlow;
public class SQLDailyCashFlowDAL : AbstractCRUDDAL, ISQLDailyCashFlowDAL
{
    private readonly string connectionString;


    public SQLDailyCashFlowDAL(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public void insertCreditDebit(DailyCashFlowEvent DailyCashFlowEvent)
    {
        DailyCashFlowEntity entity = new DailyCashFlowEntity();
        bool v = Add(entity);
    }

    public async Task<decimal> readBalanceValue(DateTime date)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(
            "SELECT SUM(flow_value) FROM DailyCashFlow WHERE CAST(flowDate AS DATE) = @Date", 
            connection);
            
        command.Parameters.AddWithValue("@Date", date.Date);
        await connection.OpenAsync();
        
        var result = await command.ExecuteScalarAsync();
        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
    }


}