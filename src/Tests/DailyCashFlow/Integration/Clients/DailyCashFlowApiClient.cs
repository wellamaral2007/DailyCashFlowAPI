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
        var response = await _httpClient.PostAsJsonAsync("/finance-auth/token", new LoginRequest(username, password));
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
        return await _httpClient.PostAsJsonAsync("/daily-cash-flow/entry", request);
    }

    public async Task<HttpResponseMessage> GetBalanceAsync(string isoDate, string sessionCode)
    {
        return await _httpClient.GetAsync($"/daily-cash-flow/balance?date={isoDate}&session={sessionCode}");
    }

    public async Task<HttpResponseMessage> DeleteEntryAsync(Guid id)
    {
        return await _httpClient.DeleteAsync($"/daily-cash-flow/entries/{id}");
    }
}
