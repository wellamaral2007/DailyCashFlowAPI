using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseFramework.Technology.Middleware.Outbound.DAL;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Relational Database Schema / Metadata Mapping
 * CONCEPTS:        Data Modeling / Optimistic Concurrency Control
 * ============================================================================
 * This component maps the physical table properties for relational storage engines,
 * decoupling infrastructure database constraints from pure core domain entities.
 */
[Table("tb_daily_cash_flow")]
public class DailyCashFlowORM : IORMEntity
{
    /**
     * Gets or sets the primary key tracking record constraint.
     */
    [Key]
    [Column("id")]
    [StringLength(50)]
    public string Id { get; set; } = null!;

    /**
     * Gets or sets the transactional monetary decimal amount.
     */
    [Column("amount")]
    public decimal Amount { get; set; }

    /**
     * Gets or sets the exact timestamp when the financial entry was written.
     */
    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; }

    /**
     * Gets or sets the database byte stream used to enforce native optimistic concurrency control.
     */
    [Timestamp]
    [Column("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
