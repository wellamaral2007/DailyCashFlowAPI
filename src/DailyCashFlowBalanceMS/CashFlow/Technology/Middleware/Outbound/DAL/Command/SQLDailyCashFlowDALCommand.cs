using BaseFramework.DAL;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;
//using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command
{

    /**
     * ============================================================================
     * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
     * HEXAGONAL ROLE:  Outbound Driven Adapter - Persistent/Durable Command
     * CONCEPTS:        Data Access Layer (DAL) / Entity Framework Core 14 / ACID Write
     * ============================================================================
     * This infrastructure component handles persistent state modifications, maping
     * rich domain entities into decoupled relational database models seamlessly.
     */
    public class SQLDailyCashFlowDALCommand : AbstractCRUDDAL, IDurableDailyCashFlowCommandPort
    {
        private readonly DailyCashFlowCommandContext _context;

        /**
         * Dependency Injection via Centralized Entity Framework DbContext Pool (IoC)
         */
        public SQLDailyCashFlowDALCommand(DailyCashFlowCommandContext context)
        {
            _context = context;
        }

        /**
         * Persists the credit or debit transactional financial entry within an ACID relational scope.
         */
        public async Task insertCreditDebit(DailyCashFlowEntity dailyCashFlowEvent)
        {
            // Maps the incoming Rich Domain Entity properties into the anemic ORM schema representation
            var ormRecord = new DailyCashFlowORM
            {
                Id = dailyCashFlowEvent.Id, // Unique identifier for the transaction
                Amount = dailyCashFlowEvent.CreditDebitValue, // Financial amount
                TransactionDate = dailyCashFlowEvent.ThowDate
            };

            // Appends the record into the Entity Framework Context Change Tracker mechanism
            _context.CashFlowEntries.Add(ormRecord);

            // Pushes the atomic physical insert down onto the SQL engine database topology
            await _context.SaveChangesAsync();
        }
    }
}