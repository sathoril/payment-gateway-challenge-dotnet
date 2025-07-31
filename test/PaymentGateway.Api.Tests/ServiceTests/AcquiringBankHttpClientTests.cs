using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.ServiceTests;

public class AcquiringBankHttpClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly AcquiringBankHttpClient _acquiringBankHttpClient;

    public AcquiringBankHttpClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:8080/")
        };
        _acquiringBankHttpClient = new AcquiringBankHttpClient(_httpClient);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithValidRequest_ReturnsSuccessfulAuthorizedResponse()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        var responseJson = JsonSerializer.Serialize(bankResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _acquiringBankHttpClient.SendPaymentToBankAsync(request);

        // Assert
        Assert.True(result.IsSuccessStatusCode);
        Assert.NotNull(result.Item2);
        Assert.True(result.Item2.Authorized);
        Assert.Equal("AUTH123", result.Item2.AuthorizationCode);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithValidRequest_ReturnsSuccessfulDeclinedResponse()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = false,
            AuthorizationCode = null
        };

        var responseJson = JsonSerializer.Serialize(bankResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _acquiringBankHttpClient.SendPaymentToBankAsync(request);

        // Assert
        Assert.True(result.IsSuccessStatusCode);
        Assert.NotNull(result.Item2);
        Assert.False(result.Item2.Authorized);
        Assert.Null(result.Item2.AuthorizationCode);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithBadRequest_ReturnsUnsuccessfulResponse()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _acquiringBankHttpClient.SendPaymentToBankAsync(request);

        // Assert
        Assert.False(result.IsSuccessStatusCode);
        Assert.NotNull(result.Item2);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithInternalServerError_ReturnsUnsuccessfulResponse()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _acquiringBankHttpClient.SendPaymentToBankAsync(request);

        // Assert
        Assert.False(result.IsSuccessStatusCode);
        Assert.NotNull(result.Item2);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_SendsCorrectRequestToBank()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new PostAcquiringBankResponse
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

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(httpResponseMessage);

        // Act
        await _acquiringBankHttpClient.SendPaymentToBankAsync(request);

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
        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        var responseJson = JsonSerializer.Serialize(bankResponse);

            // Arrange
            var request = new PostPaymentRequest
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

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
                .ReturnsAsync(httpResponseMessage);

            // Act
            await _acquiringBankHttpClient.SendPaymentToBankAsync(request);

            // Assert
            var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
            var bankRequest = JsonSerializer.Deserialize<PostAcquiringBankRequest>(requestContent);
            
            Assert.Equal(expectedRequestExpiryDate, bankRequest!.ExpiryDate);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithNetworkTimeout_ThrowsException()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => 
            _acquiringBankHttpClient.SendPaymentToBankAsync(request));
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithHttpRequestException_ThrowsException()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _acquiringBankHttpClient.SendPaymentToBankAsync(request));
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithBankResponseAs503_ReturnsSuccessAsFalse()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent(JsonSerializer.Serialize(new PostAcquiringBankResponse()), Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _acquiringBankHttpClient.SendPaymentToBankAsync(request);

        // Assert
        Assert.False(result.IsSuccessStatusCode);
        Assert.NotNull(result.Item2);
    }

    [Fact]
    public async Task SendPaymentToBankAsync_WithDifferentCurrencies_SendsCorrectCurrency()
    {
        // Arrange
        var currencies = new[] { "USD", "EUR", "GBP", "JPY", "BRL" };
        
        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        var responseJson = JsonSerializer.Serialize(bankResponse);

        foreach (var currency in currencies)
        {
            var request = new PostPaymentRequest
            {
                CardNumber = "1234567812345678",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = currency,
                Amount = 1000,
                Cvv = "123"
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            HttpRequestMessage capturedRequest = null;

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
                .ReturnsAsync(httpResponseMessage);

            // Act
            await _acquiringBankHttpClient.SendPaymentToBankAsync(request);

            // Assert
            var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
            var bankRequest = JsonSerializer.Deserialize<PostAcquiringBankRequest>(requestContent);
            
            Assert.Equal(currency, bankRequest!.Currency);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}