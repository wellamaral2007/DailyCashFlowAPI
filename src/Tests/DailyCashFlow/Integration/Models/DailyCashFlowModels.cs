namespace DailyCashFlow.Integration.Models;


public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token);

public record EntryRequest(decimal Amount);
public record EntryResponse(Guid Id, decimal Amount);

public record BalanceResponse(DateTime Date, decimal Balance);
