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

public class DailyCashFlowApiClient
{
    private readonly HttpClient _httpClient;

    public DailyCashFlowApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Realiza o Handshake OAuth2 (Client Credentials) contra o Apigee idêntico ao comando cURL.
    /// </summary>
    // 🚀 MUDANÇA DE ASSINATURA: Agora retorna Task<string> para extrair o token para o Runner [1.3]
    public async Task<string> AuthenticateAsync(string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:8443/v1/finance-auth/token");

        request.Headers.Add("user", username);
        request.Headers.Add("password", password);
        request.Headers.Host = "daily-cashflow-sol-17852.dns-replaceme.example.com";

        var clientId = "SdYiWDjmzNdhINMDCoOnG2RKwMGSJr6FrwM7P0QfYpeHG8Lg";
        var clientSecret = "2P1NKp45EcvgktlVtlKbkrps1bAXZfhfLLhU302UYjb5c2WvYRGuPYRRmsZEOg2a";
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var formData = new Dictionary<string, string> { { "grant_type", "client_credentials" } };
        request.Content = new FormUrlEncodedContent(formData);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            string tokenReal = tokenElement.GetString() ?? throw new Exception("Token obtido veio nulo.");

            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenReal);
                
            Console.WriteLine($"\n[AUTENTICADO COM SUCESSO] Token Bearer obtido: {tokenReal}\n");
            
            return tokenReal; // 🚀 RETORNA O TOKEN EXTRAÍDO PARA O RUNNER [1.3]
        }
        
        throw new HttpRequestException("O Apigee respondeu com sucesso, mas a chave 'access_token' não foi encontrada no JSON.");
    }



    /// <summary>
    /// Envia uma movimentação financeira forçando a URL absoluta para o .NET manter o Host do Apigee.
    /// </summary>
    public async Task<HttpResponseMessage> CreateEntryAsync(EntryRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        string amountInBrazilianFormat = request.Amount.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        
        // 🚀 CORREÇÃO: Cria a URL Absoluta unindo a base com o proxy do Apigee
        var absoluteUri = new Uri(_httpClient.BaseAddress!, $"v1/cashflow/entry?CreditDebitValue={amountInBrazilianFormat}");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, absoluteUri);
        httpRequest.Content = JsonContent.Create(request);

        if (_httpClient.DefaultRequestHeaders.Authorization != null)
        {
            httpRequest.Headers.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
        }

        // Agora que a URI é resolvida completamente, o .NET aceita a substituição estrita do Host igual ao cURL
        httpRequest.Headers.Host = "daily-cashflow-sol-17852.dns-replaceme.example.com";

        return await _httpClient.SendAsync(httpRequest);
    }

    /// <summary>
    /// Consulta o saldo forçando a URL absoluta, garantindo o envio do cabeçalho Host corporativo (Corrige o 404).
    /// </summary>
    public async Task<HttpResponseMessage> GetBalanceAsync(string isoDate, string sessionCode)
    {
        // 🚀 CORREÇÃO: Cria a URL Absoluta unindo a base com o proxy do Apigee (Linha 97)
        var absoluteUri = new Uri(_httpClient.BaseAddress!, $"v1/cashflow/balance?date={isoDate}&session={sessionCode}");
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, absoluteUri);

        if (_httpClient.DefaultRequestHeaders.Authorization != null)
        {
            httpRequest.Headers.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
        }

        // Força o cabeçalho Host idêntico ao comando cURL funcional
        httpRequest.Headers.Host = "daily-cashflow-sol-17852.dns-replaceme.example.com";

        return await _httpClient.SendAsync(httpRequest);
    }


    /// <summary>
    /// Remove um lançamento específico a partir do identificador único (Guid).
    /// </summary>
    public async Task<HttpResponseMessage> DeleteEntryAsync(Guid id)
    {
        return await _httpClient.DeleteAsync($"/daily-cash-flow/entries/{id}");
    }
}
