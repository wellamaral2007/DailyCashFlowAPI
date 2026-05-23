using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using DailyCashFlow.Integration.Clients;
using DailyCashFlow.Integration.Models;
using Xunit;

namespace Tests.DailyCashFlow.IntegrationCashFlow.Tests;

public class DailyCashFlowIntegrationTests : IAsyncLifetime
{
    private readonly DailyCashFlowApiClient _apiClient;
    private readonly string _username;
    private readonly string _password;
    private readonly string _uniqueSession;
    private readonly List<Guid> _createdEntriesInTest;

    public DailyCashFlowIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var baseUrl = configuration["ApiSettings:BaseUrl"] ?? throw new ArgumentNullException("BaseUrl is null.");
        _username = configuration["ApiSettings:TestUser"] ?? throw new ArgumentNullException("TestUser is null.");
        _password = configuration["ApiSettings:TestPassword"] ?? throw new ArgumentNullException("TestPassword is null.");

        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _apiClient = new DailyCashFlowApiClient(httpClient);

        _uniqueSession = Guid.NewGuid().ToString();
        _createdEntriesInTest = new List<Guid>();
    }

    // Runs BEFORE each [Fact] test method
    public async Task InitializeAsync()
    {
        await _apiClient.AuthenticateAsync(_username, _password);
    }

    [Fact]
    public async Task IsolatedFlow_ShouldCalculateBalanceCorrectlyAndAutoCleanUp()
    {
        // 1. Arrange
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var credit = new EntryRequest(300.00m);
        var debit = new EntryRequest(100.00m);

        // 2. Act
        var resCredit = await _apiClient.CreateEntryAsync(credit);
        var resDebit = await _apiClient.CreateEntryAsync(debit);

        // Track created IDs for cleanup
        if (resCredit.IsSuccessStatusCode) {
            var body = await resCredit.Content.ReadFromJsonAsync<EntryResponse>();
            if (body != null) _createdEntriesInTest.Add(body.Id);
        }
        if (resDebit.IsSuccessStatusCode) {
            var body = await resDebit.Content.ReadFromJsonAsync<EntryResponse>();
            if (body != null) _createdEntriesInTest.Add(body.Id);
        }

        var resBalance = await _apiClient.GetBalanceAsync(today, _uniqueSession);

        // 3. Assert
        Assert.Equal(HttpStatusCode.Created, resCredit.StatusCode);
        Assert.Equal(HttpStatusCode.Created, resDebit.StatusCode);
        
        resBalance.EnsureSuccessStatusCode();
        var balanceData = await resBalance.Content.ReadFromJsonAsync<BalanceResponse>();
        
        Assert.NotNull(balanceData);
        Assert.Equal(200.00m, balanceData.Balance); 
    }

    // Runs AFTER each [Fact] test method (Guarantees cleanup)
    public async Task DisposeAsync()
    {
        foreach (var id in _createdEntriesInTest)
        {
            try
            {
                await _apiClient.DeleteEntryAsync(id);
            }
            catch
            {
                // Log cleanup failure if necessary, without breaking the test report
            }
        }
    }
}
