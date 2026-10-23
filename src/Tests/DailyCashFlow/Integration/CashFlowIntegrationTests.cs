using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DailyCashFlow.Integration.Clients;
using DailyCashFlow.Integration.Models;
using Xunit;

namespace Tests.DailyCashFlow.IntegrationCashFlow.Tests;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Quality Verification)
 * HEXAGONAL ROLE:  Outbound Test Adapter - Automated Integration Test Suite
 * CONCEPTS:        State Machine Verification / Content Isolation / Automated Teardown
 * ============================================================================
 * This test suite coordinates end-to-end relational and caching network state validations,
 * asserting decimal parsing computations and sweeping data variations upon completion.
 */
public class DailyCashFlowIntegrationTests : IAsyncLifetime
{
    private readonly DailyCashFlowApiClient _apiClient;
    private readonly string _username;
    private readonly string _password;
    private readonly string _uniqueSession;
    private readonly List<Guid> _createdEntriesInTest;

    /**
     * Initializes configuration bounds, handlers bypassing untrusted certificates, and API proxy routes.
     */
    public DailyCashFlowIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8080";
        _username = configuration["ApiSettings:TestUser"] ?? "test_user";
        _password = configuration["ApiSettings:TestPassword"] ?? "password123";

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        _apiClient = new DailyCashFlowApiClient(httpClient);

        _uniqueSession = Guid.NewGuid().ToString();
        _createdEntriesInTest = new List<Guid>();
    }

    /**
     * Pre-execution trigger initializing credentials handshake prior to invoking facts.
     */
    public async Task InitializeAsync()
    {
        await _apiClient.AuthenticateAsync(_username, _password);
    }

    /**
     * Validates simultaneous transaction postings, verifying multi-storage dual-write cache consistency.
     */
    [Fact]
    public async Task IsolatedFlow_ShouldCalculateBalanceCorrectlyAndAutoCleanUp()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var credit = new EntryRequest(3756.87m);
        var debit = new EntryRequest(-564.49m);

        var resCredit = await _apiClient.CreateEntryAsync(credit);
        var resDebit = await _apiClient.CreateEntryAsync(debit);

        if (resCredit.IsSuccessStatusCode && resCredit.StatusCode != HttpStatusCode.NoContent) 
        {
            try 
            {
                var body = await resCredit.Content.ReadFromJsonAsync<EntryResponse>();
                if (body != null && body.Id != Guid.Empty) _createdEntriesInTest.Add(body.Id);
            } 
            catch 
            {
                // Suppresses exceptions if endpoint skips returning structured JSON metadata
            }
        }
        
        if (resDebit.IsSuccessStatusCode && resDebit.StatusCode != HttpStatusCode.NoContent) 
        {
            try 
            {
                var body = await resDebit.Content.ReadFromJsonAsync<EntryResponse>();
                if (body != null && body.Id != Guid.Empty) _createdEntriesInTest.Add(body.Id);
            } 
            catch 
            {
                // Suppresses exceptions if endpoint skips returning structured JSON metadata
            }
        }

        /**
         * Processing delay gate ensuring background event-driven saga consumers complete operations.
         */
        await Task.Delay(20000);

        var resBalance = await _apiClient.GetBalanceAsync(today, _uniqueSession);

        if (!resBalance.IsSuccessStatusCode)
        {
            string attemptedUrl = resBalance.RequestMessage?.RequestUri?.ToString() ?? "Unknown";
            string errorContent = await resBalance.Content.ReadAsStringAsync();
            
            throw new HttpRequestException($"[HTTP ERROR DIAGNOSTICS] Requested URL target: '{attemptedUrl}' | Remote Response Body payload: {errorContent}");
        }

        var rawJson = await resBalance.Content.ReadAsStringAsync();

        var numericPart = rawJson
            .Replace("{\"balanceValue\":", "")
            .Replace("}", "")
            .Trim();

        var culturePtBr = new System.Globalization.CultureInfo("pt-BR");
        decimal balanceValue = decimal.Parse(numericPart, culturePtBr);

        Assert.Equal(3192.38m, balanceValue);
    }

    /**
     * Post-execution infrastructure teardown purging generated data rows to keep state isolation.
     */
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
                // Prevents test lifecycle blockages if database records are cleared downstream
            }
        }
    }
}
