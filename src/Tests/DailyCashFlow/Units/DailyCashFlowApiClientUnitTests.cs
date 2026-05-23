using System.Net;
using System.Net.Http.Json;
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
            BaseAddress = new Uri("https://gcp-finance-apigee.apigee.net/v1/cashflow")
        };
        
        _apiClient = new DailyCashFlowApiClient(_httpClient);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ShouldSetAuthorizationHeader()
    {
        // Arrange
        var username = "test_user";
        var password = "password123";
        var fakeResponse = new LoginResponse("fake-jwt-token");

        SetupMockHttpResponse(HttpStatusCode.OK, fakeResponse);

        // Act
        await _apiClient.AuthenticateAsync(username, password);

        // Assert
        Assert.NotNull(_httpClient.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", _httpClient.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal("fake-jwt-token", _httpClient.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public async Task CreateEntry_ValidRequest_ShouldReturnCreatedStatus()
    {
        // Arrange
        var request = new EntryRequest(150.00m);
        var fakeResponse = new EntryResponse(Guid.NewGuid(), 150.00m);

        SetupMockHttpResponse(HttpStatusCode.Created, fakeResponse);

        // Act
        var response = await _apiClient.CreateEntryAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        // Verifica se a API foi chamada no endpoint correto e com o método POST
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Method == HttpMethod.Post && 
                req.RequestUri.AbsolutePath == "/api/cash-flow/entries"),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetBalance_ValidParameters_ShouldReturnCorrectBalanceData()
    {
        // Arrange
        var isoDate = "2026-05-22";
        var sessionCode = "session-123";
        var fakeResponse = new BalanceResponse(DateTime.Parse(isoDate), 500.00m);

        SetupMockHttpResponse(HttpStatusCode.OK, fakeResponse);

        // Act
        var response = await _apiClient.GetBalanceAsync(isoDate, sessionCode);
        var content = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.Equal(500.00m, content.Balance);
    }

    // Helper método para simular as respostas HTTP que a API daria
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
