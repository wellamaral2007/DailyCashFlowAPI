using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DailyCashFlow.Integration.Models;

namespace DailyCashFlow.Integration.Clients;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Integration)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - API Client / Gateway Proxy Link
 * CONCEPTS:        OAuth2 Handshake Validation / Absolute URI Interception
 * ============================================================================
 * This component encapsulates the outbound REST integration client logic,
 * routing requests through Apigee proxies with dynamic host resolution.
 */
public class DailyCashFlowApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apigeeAuthHost;
    private readonly string _apigeeClientId;
    private readonly string _apigeeClientSecret;

    /**
     * Initializes a new instance resolving infrastructure parameters dynamically from environment schemas.
     */
    public DailyCashFlowApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        _apigeeAuthHost = Environment.GetEnvironmentVariable("APIGEE_AUTH_HOST") ?? "localhost";
        _apigeeClientId = Environment.GetEnvironmentVariable("APIGEE_CLIENT_ID") ?? string.Empty;
        _apigeeClientSecret = Environment.GetEnvironmentVariable("APIGEE_CLIENT_SECRET") ?? string.Empty;
    }

    /**
     * Executes the OAuth2 Client Credentials handshake against the Apigee gateway proxy cluster.
     */
    public async Task<string> AuthenticateAsync(string username, string password)
    {
        var authUrl = $"https://{_apigeeAuthHost}/v1/finance-auth/token";
        var request = new HttpRequestMessage(HttpMethod.Post, authUrl);

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apigeeClientId}:{_apigeeClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var formData = new Dictionary<string, string> { { "grant_type", "client_credentials" } };
        request.Content = new FormUrlEncodedContent(formData);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            string tokenReal = tokenElement.GetString() ?? throw new Exception("The resolved bearer access token instance is null.");

            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenReal);
                
            Console.WriteLine($"[AUTHENTICATION SUCCESS] Bearer access token retrieved successfully.");
            
            return tokenReal;
        }
        
        throw new HttpRequestException("The gateway responded with HTTP 200, but the 'access_token' metadata key was missing from the response body.");
    }

    /**
     * Dispatches a financial transaction movement entry string utilizing specific pt-BR floating point schemas.
     */
    public async Task<HttpResponseMessage> CreateEntryAsync(EntryRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        string amountInBrazilianFormat = request.Amount.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        
        var absoluteUri = new Uri(_httpClient.BaseAddress!, $"v1/cashflow/entry?CreditDebitValue={amountInBrazilianFormat}");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, absoluteUri);
        httpRequest.Content = JsonContent.Create(request);

        if (_httpClient.DefaultRequestHeaders.Authorization != null)
        {
            httpRequest.Headers.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
        }

        return await _httpClient.SendAsync(httpRequest);
    }

    /**
     * Resolves the consolidated net balance for a specific date through the secure gateway proxy cluster.
     */
    public async Task<HttpResponseMessage> GetBalanceAsync(string isoDate, string sessionCode)
    {
        var absoluteUri = new Uri(_httpClient.BaseAddress!, $"v1/cashflow/balance?date={isoDate}&session={sessionCode}");
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, absoluteUri);

        if (_httpClient.DefaultRequestHeaders.Authorization != null)
        {
            httpRequest.Headers.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
        }

        return await _httpClient.SendAsync(httpRequest);
    }

    /**
     * Purges an isolated tracking transaction ledger record using its unique identification descriptor.
     */
    public async Task<HttpResponseMessage> DeleteEntryAsync(Guid id)
    {
        return await _httpClient.DeleteAsync($"/daily-cash-flow/entries/{id}");
    }
}
