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


    public SQLDailyCashFlowDAL()
    {
        string _projectId = "DailyCashFlowBalance";
        string _database = VaultCredentialUtility.GetSecretAsync(_projectId, "DATABASE").GetAwaiter().GetResult(); 
        string _userDB = VaultCredentialUtility.GetSecretAsync(_projectId, "USER_DATABASE").GetAwaiter().GetResult(); 
        string _passwordDB = VaultCredentialUtility.GetSecretAsync(_projectId, "PASSWORD_DATABASE").GetAwaiter().GetResult();

        string sqlConnectionString = $"Server=GCP_CLOUD_SQL\\SQLEXPRESS;Database={_database};User Id={_userDB};Password={_passwordDB};Trusted_Connection=True;TrustServerCertificate=True;";
    
        this.connectionString = sqlConnectionString;
    }

    public async Task insertCreditDebit(DailyCashFlowEvent DailyCashFlowEvent)
    {
        var connection = new SqlConnection(connectionString);
        // Garante de forma segura e idempotente que a tabela e os campos existam no SQL Server
        string checkTableQuery = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DailyCashFlow' AND xtype='U')
            BEGIN
                CREATE TABLE DailyCashFlow (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Value DECIMAL(18,2) NOT NULL,
                    Date DATE NOT NULL
                );
            END";
        await connection.OpenAsync();
        using (var cmdCheck = new SqlCommand(checkTableQuery, connection))
        {
            await cmdCheck.ExecuteNonQueryAsync();
        }
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