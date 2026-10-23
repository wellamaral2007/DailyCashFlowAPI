using System;

namespace DailyCashFlow.Integration.Models;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase C - Information Systems Architecture (Data Records)
 * HEXAGONAL ROLE:  Integration Data Transfer Objects / Contract Records
 * CONCEPTS:        Strongly-Typed Payloads / Immutable Messaging Schemas
 * ============================================================================
 * This file gathers the immutable data record structures used to map network
 * payloads across outbound API client boundaries and testing integration pipelines.
 */

/**
 * Represents the credentials contract payload submitted during the authentication handshake.
 */
public record LoginRequest(string Username, string Password);

/**
 * Captures the access token string returned upon successful security identity authorization.
 */
public record LoginResponse(string Token);

/**
 * Represents the inbound numerical financial movement entry request mapping.
 */
public record EntryRequest(decimal Amount);

/**
 * Captures the structured data validation mapping returned after writing a transaction entry.
 */
public record EntryResponse(Guid Id, decimal Amount);

/**
 * Represents the final state projection model holding the aggregated balance value.
 */
public record BalanceResponse(DateTime Date, decimal Balance);
