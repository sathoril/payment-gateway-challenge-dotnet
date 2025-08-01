using System.Net;
using System.Text;
using System.Text.Json;

using Moq;
using Moq.AutoMock;
using Moq.Protected;

using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Domain;
using PaymentGateway.Infrastructure.HttpClients;

namespace PaymentGateway.Api.Tests.InfrastructureTests.HttpClientsTests;

public class AcquiringBankHttpClientTests
{
    private readonly AutoMocker _autoMocker = new AutoMocker();
    private readonly Mock<HttpMessageHandler> _mockedHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly AcquiringBankService _acquiringBankService;

    public AcquiringBankHttpClientTests()
    {
        _mockedHttpMessageHandler = _autoMocker.GetMock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockedHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:8080/")
        };
        _acquiringBankService = new AcquiringBankService(_httpClient);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithValidRequest_ReturnsSuccessfulAuthorizedResponse()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new AcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        var responseJson = JsonSerializer.Serialize(bankResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockedHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _acquiringBankService.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

        // Assert
        Assert.True(result.SuccessfulRequest);
        Assert.True(result.Authorized);
        Assert.NotEmpty(result.AuthorizationCode);
        Assert.Equal("AUTH123", result.AuthorizationCode);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithValidRequest_ReturnsSuccessfulDeclinedResponse()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new AcquiringBankResponse
        {
            Authorized = false,
            AuthorizationCode = null
        };

        var responseJson = JsonSerializer.Serialize(bankResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockedHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _acquiringBankService.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);;

        // Assert
        Assert.True(result.SuccessfulRequest);
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.InsufficientStorage)]
    public async Task SendPaymentToBankAsync_WithAnyResponseDifferentThen200OK_ReturnsUnsuccessfulResponse(HttpStatusCode failedStatusCode)
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var httpResponseMessage = new HttpResponseMessage(failedStatusCode)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        _mockedHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _acquiringBankService.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.SuccessfulRequest);
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_SendsCorrectRequestToBank()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new AcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        var responseJson = JsonSerializer.Serialize(bankResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        HttpRequestMessage capturedRequest = null;

        _mockedHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(httpResponseMessage);

        // Act
        await _acquiringBankService.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);;;

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Contains("payments", capturedRequest.RequestUri?.ToString());
        Assert.Equal("application/json", capturedRequest.Content?.Headers.ContentType?.MediaType);

        var requestContent = await capturedRequest.Content!.ReadAsStringAsync();
        var bankRequest = JsonSerializer.Deserialize<PostAcquiringBankRequest>(requestContent);
        
        Assert.NotNull(bankRequest);
        Assert.Equal("1234567812345678", bankRequest.CardNumber);
        Assert.Equal("12/2025", bankRequest.ExpiryDate);
        Assert.Equal("USD", bankRequest.Currency);
        Assert.Equal(1000, bankRequest.Amount);
        Assert.Equal("123", bankRequest.Cvv);
    }

    [Theory]
    [InlineData(1, 2025, "01/2025")]
    [InlineData(12, 2025, "12/2025")]
    [InlineData(5, 2030, "05/2030")]
    [InlineData(10, 2024, "10/2024")]
    public async Task SendPaymentToBankAsync_WithDifferentExpiryDates_FormatsCorrectly(
        int requestMonth, int requestYear, string expectedRequestExpiryDate)
    {
        // Arrange
        var bankResponse = new AcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        var responseJson = JsonSerializer.Serialize(bankResponse);

            // Arrange
            var request = new ProcessPaymentRequest
            {
                CardNumber = "1234567812345678",
                ExpiryMonth = requestMonth,
                ExpiryYear = requestYear,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            HttpRequestMessage capturedRequest = null;

            _mockedHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
                .ReturnsAsync(httpResponseMessage);

            // Act
            await _acquiringBankService.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

            // Assert
            var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
            var bankRequest = JsonSerializer.Deserialize<PostAcquiringBankRequest>(requestContent);
            
            Assert.Equal(expectedRequestExpiryDate, bankRequest!.ExpiryDate);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithAnyException_ReturnsUnsuccessfulResponse()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        _mockedHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _acquiringBankService.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.SuccessfulRequest);
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
    }

}