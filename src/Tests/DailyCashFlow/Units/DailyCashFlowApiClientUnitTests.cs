using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Moq;
using Moq.Protected;
using DailyCashFlow.Integration.Clients;
using DailyCashFlow.Integration.Models;
using Xunit;

namespace Tests.DailyCashFlow.Units.DailyCashFlow.Tests.UnitTests;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Quality Assurance)
 * HEXAGONAL ROLE:  Outbound Test Adapter - Unit Testing Suite
 * CONCEPTS:        Mock Isolation / HTTP Interception Pipeline / Contract Validation
 * ============================================================================
 * This test suite utilizes the Moq framework to intercept standard network communication
 * handlers, isolating client execution logic from dependencies on live cloud environments.
 */
public class DailyCashFlowApiClientUnitTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly DailyCashFlowApiClient _apiClient;

    /**
     * Initializes mock infrastructure and configures loopback network bounds for testing.
     */
    public DailyCashFlowApiClientUnitTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        
        _apiClient = new DailyCashFlowApiClient(_httpClient);
    }

    /**
     * Asserts that submitting authentic OAuth2 credentials applies a Bearer header token to the context.
     */
    [Fact(DisplayName = "AuthenticateAsync with valid OAuth2 client credentials should set authorization header")]
    public async Task AuthenticateAsync_ValidCredentials_ShouldSetAuthorizationHeader()
    {
        var username = "test_user";
        var password = "password123";
        var fakeToken = "gcp-apigee-at-v1-987456123...";

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

        await _apiClient.AuthenticateAsync(username, password);

        Assert.NotNull(_httpClient.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", _httpClient.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal(fakeToken, _httpClient.DefaultRequestHeaders.Authorization.Parameter);
    }

    /**
     * Asserts that creating financial movement request records returns an Accepted status code.
     */
    [Fact(DisplayName = "CreateEntry with valid financial request should return accepted status code")]
    public async Task CreateEntry_ValidRequest_ShouldReturnCreatedStatus()
    {
        var request = new EntryRequest(-2673.69m);
        var fakeResponse = new EntryResponse(Guid.NewGuid(), -2673.69m);

        SetupMockHttpResponse(HttpStatusCode.Accepted, fakeResponse);

        var response = await _apiClient.CreateEntryAsync(request);

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

    /**
     * Asserts that querying balances within specific parameters resolves accurate calculations.
     */
    [Fact(DisplayName = "GetBalance with valid time window parameters should return accurate calculated scalar")]
    public async Task GetBalance_ValidParameters_ShouldReturnCorrectBalanceData()
    {
        var isoDate = "2026-10-18";
        var sessionCode = "session-123";
        var fakeResponse = new BalanceResponse(DateTime.Parse(isoDate), -2673.69m);

        SetupMockHttpResponse(HttpStatusCode.OK, fakeResponse);

        var response = await _apiClient.GetBalanceAsync(isoDate, sessionCode);
        var content = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.Equal(-2673.69m, content.Balance);
    }

    /**
     * Helper routine injecting mocked payload responses into the simulated HTTP handler pipeline.
     */
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
