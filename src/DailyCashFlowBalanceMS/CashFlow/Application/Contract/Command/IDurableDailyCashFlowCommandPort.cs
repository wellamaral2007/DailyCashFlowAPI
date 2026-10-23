using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using System.Threading.Tasks;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;

/// <summary>
/// Defines the outbound driven port for persisting transaction entries into durable relational storage.
/// </summary>
public interface IDurableDailyCashFlowCommandPort
{
    /// <summary>
    /// Inserts a validated credit or debit cash flow transaction ledger entry into the database.
    /// </summary>
    public Task insertCreditDebit(DailyCashFlowEntity DailyCashFlowEntity);
}
