// File: src/BaseFramework/Technology/Middleware/Inbound/DTO/Event/DailyCashFlow/DailyCashFlowEvent.cs
using System;

namespace BaseFramework.Event.DailyCashFlow;

public class DailyCashFlowEvent 
{    
    // 🌐 Adicionado "= string.Empty" e "?" para zerar os avisos de Nullability
    public string Id { get; set; } = string.Empty;
    public decimal CreditDebitValue { get; set; }
    public DateTime ThowDate { get; set; }
    public string? EventType { get; set; }
}
