using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseFramework.Technology.Middleware.Outbound.DAL;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;

[Table("tb_daily_cash_flow")]
public class DailyCashFlowORM : IORMEntity
{
    [Key]
    [Column("id")]
    [StringLength(50)]
    public string Id { get; set; } = null!;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; }

    [Timestamp] // Controle de Concorrência Otimista nativo do EF6 (byte[] rowversion)
    [Column("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
