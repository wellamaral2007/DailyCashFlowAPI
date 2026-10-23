using System;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.DTO.Event.DailyCashFlow;

/**
 * TOGAF DIMENSION: Phase C - Information Systems Architecture (Data Transfer Object)
 * HEXAGONAL ROLE:  Inbound Data Transfer Object / Infrastructure Messaging Contract
 * CONCEPTS:        Decoupled Serialization Model / Ephemeral Event Schema
 */
public class DailyCashFlowEvent 
{    
    /**
     * Gets or sets the unique identifier generated for the specific transaction boundary.
     */
    public string Id { get; set; } = string.Empty;

    /**
     * Gets or sets the absolute monetary numeric transaction value for credits or debits.
     */
    public decimal CreditDebitValue { get; set; }

    /**
     * Gets or sets the exact timestamp when the financial entry was posted.
     */
    public DateTime ThowDate { get; set; }

    /**
     * Gets or sets the event type tag used to orchestrate internal workflow transitions.
     */
    public string? EventType { get; set; }
}
