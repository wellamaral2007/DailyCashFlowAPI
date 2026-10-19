using System.Net.Http.Headers;
using System.Net.Http.Json;
using DailyCashFlow.Integration.Models;

namespace DailyCashFlow.Integration.Clients;

public class DailyCashFlowApiClient
{
    private readonly HttpClient _httpClient;

    public DailyCashFlowApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task AuthenticateAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("/v1/finance-auth/token", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (loginResult != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", loginResult.Token);
        }
    }

    public async Task<HttpResponseMessage> CreateEntryAsync(EntryRequest request)
    {
        // return await _httpClient.PostAsJsonAsync($"/daily-cash-flow/entry/?CreditDebitValue={request.Amount}"", request);
        return await _httpClient.PostAsJsonAsync($"/?CreditDebitValue={request.Amount}", request); // 🚀 TESTING: Local test
    }

    public async Task<HttpResponseMessage> GetBalanceAsync(string isoDate, string sessionCode)
    {
        // return await _httpClient.GetAsync($"/daily-cash-flow/balance?date={isoDate}&session={sessionCode}");
        return await _httpClient.GetAsync($"/?date={isoDate}&session={sessionCode}"); // 🚀 TESTING: Local test
    }

    public async Task<HttpResponseMessage> DeleteEntryAsync(Guid id)
    {
        return await _httpClient.DeleteAsync($"/daily-cash-flow/entries/{id}");
    }
}
