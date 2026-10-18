using System;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.DTO.Event.DailyCashFlow;

public class DailyCashFlowEvent 
{    
    public string Id { get; set; } = string.Empty;
    public decimal CreditDebitValue { get; set; }
    public DateTime ThowDate { get; set; }
    public string? EventType { get; set; }
}
