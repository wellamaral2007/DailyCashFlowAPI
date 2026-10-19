// File: src/DailyCashFlowBalanceMS.Tests/UnitTests/DailyCashFlowApiClientUnitTests.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth; // 📌 Required to access the Success static factory
using Moq;
using Moq.Protected;
using DailyCashFlow.Integration.Clients;
using DailyCashFlow.Integration.Models;
using Xunit;

namespace Tests.DailyCashFlow.Units.DailyCashFlow.Tests.UnitTests;

public class DailyCashFlowApiClientUnitTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly DailyCashFlowApiClient _apiClient;

    public DailyCashFlowApiClientUnitTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:8080") // Local test
            //BaseAddress = new Uri("https://localhost:8080") // Na nuvem passando pelo Apigee
        };
        
        _apiClient = new DailyCashFlowApiClient(_httpClient);
    }

    
    [Fact(DisplayName = "AuthenticateAsync with valid OAuth2 client credentials should set authorization header")]
    public async Task AuthenticateAsync_ValidCredentials_ShouldSetAuthorizationHeader()
    {
        // Arrange
        var username = "test_user";
        var password = "password123";
        var fakeToken = "gcp-apigee-at-v1-987456123...";

        // 1. O JSON PRECISA conter a chave "Token" exatamente como o seu modelo LoginResponse espera ler
        var jsonPayload = $"{{\"Token\":\"{fakeToken}\"}}";
        
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri!.AbsolutePath == "/v1/finance-auth/token"
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        await _apiClient.AuthenticateAsync(username, password);

        // Assert (Linha 62 agora vai passar com sucesso!)
        Assert.NotNull(_httpClient.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", _httpClient.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal(fakeToken, _httpClient.DefaultRequestHeaders.Authorization.Parameter);
    }



    [Fact(DisplayName = "CreateEntry with valid financial request should return accepted status code")]
    public async Task CreateEntry_ValidRequest_ShouldReturnCreatedStatus()
    {
        // Arrange
        var request = new EntryRequest(-2673.69m);
        var fakeResponse = new EntryResponse(Guid.NewGuid(), -2673.69m);

        SetupMockHttpResponse(HttpStatusCode.Accepted, fakeResponse);

        // Act
        var response = await _apiClient.CreateEntryAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync", 
            Times.Once(), 
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Method == HttpMethod.Post && 
                req.RequestUri!.PathAndQuery.Contains("CreditDebitValue=")
            ), 
            ItExpr.IsAny<CancellationToken>()
        );

    }

    [Fact(DisplayName = "GetBalance with valid time window parameters should return accurate calculated scalar")]
    public async Task GetBalance_ValidParameters_ShouldReturnCorrectBalanceData()
    {
        // Arrange
        var isoDate = "2026-10-18";
        var sessionCode = "session-123";
        var fakeResponse = new BalanceResponse(DateTime.Parse(isoDate), -2673.69m);

        SetupMockHttpResponse(HttpStatusCode.OK, fakeResponse);

        // Act
        var response = await _apiClient.GetBalanceAsync(isoDate, sessionCode);
        var content = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.Equal(-2673.69m, content.Balance);
    }

    private void SetupMockHttpResponse<T>(HttpStatusCode statusCode, T content)
    {
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(content)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);
    }
}
