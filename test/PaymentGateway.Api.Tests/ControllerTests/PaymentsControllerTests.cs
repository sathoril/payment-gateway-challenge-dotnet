using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.ControllerTests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();
    
    [Fact]
    public async Task GetPayment_ReturnsPaymentSuccessfully_IfExists()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);
        var mockBankHttpClient = new Mock<IBankHttpClient>();
        var paymentService = new PaymentsService(mockBankHttpClient.Object, paymentsRepository);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(paymentsRepository);
                services.AddSingleton<IPaymentService>(paymentService);
                services.AddSingleton(mockBankHttpClient.Object);
            }))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadAsStringAsync();
    
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task GetPayment_Returns404Error_IfPaymentDoesntExists()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task PostPaymentAsync_WithValidRequest_ReturnsOkWithPaymentResponse()
    {
        // Arrange
        var validRequest = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var expectedResponse = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "5678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000
        };

        var mockPaymentService = new Mock<IPaymentService>();
        mockPaymentService
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync(expectedResponse);

        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(paymentsRepository);
                    services.AddSingleton(mockPaymentService.Object);
                }))
            .CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", validRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(expectedResponse.Id, paymentResponse.Id);
        Assert.Equal(expectedResponse.Status, paymentResponse.Status);
        mockPaymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task PostPaymentAsync_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "123", // Muito curto
            ExpiryMonth = 13, // Mês inválido
            ExpiryYear = DateTime.UtcNow.Year - 1, // Ano passado
            Currency = "INVALID",
            Amount = -100, // Valor negativo
            Cvv = "12" // CVV muito curto
        };

        var mockPaymentService = new Mock<IPaymentService>();
        var paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(paymentsRepository);
                    services.AddSingleton(mockPaymentService.Object);
                }))
            .CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        mockPaymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()), Times.Never);
    }
    
    [Fact]
    public async Task PostPaymentAsync_WithExpiredCard_ReturnsBadRequest()
    {
        // Arrange
        var expiredCardRequest = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = DateTime.UtcNow.Month,
            ExpiryYear = DateTime.UtcNow.Year, // Cartão expirado
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var mockPaymentService = new Mock<IPaymentService>();
        var paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(paymentsRepository);
                    services.AddSingleton(mockPaymentService.Object);
                }))
            .CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", expiredCardRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        mockPaymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()), Times.Never);
    }
    
    [Fact]
    public async Task PostPaymentAsync_WithNonNumericCardNumber_ReturnsBadRequest()
    {
        // Arrange
        var invalidCardRequest = new PostPaymentRequest
        {
            CardNumber = "1234abcd12345678", // Contém letras
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var mockPaymentService = new Mock<IPaymentService>();
        var paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(paymentsRepository);
                services.AddSingleton(mockPaymentService.Object);
            }))
            .CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", invalidCardRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        mockPaymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()), Times.Never);
    }

    [Fact]
    public async Task PostPaymentAsync_WithZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        var zeroAmountRequest = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 0, // Valor zero
            Cvv = "123"
        };

        var mockPaymentService = new Mock<IPaymentService>();
        var paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(paymentsRepository);
                services.AddSingleton(mockPaymentService.Object);
            }))
            .CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", zeroAmountRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        mockPaymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()), Times.Never);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var validRequest = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var mockPaymentService = new Mock<IPaymentService>();
        mockPaymentService
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        var paymentsRepository = new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(paymentsRepository);
                services.AddSingleton(mockPaymentService.Object);
            }))
            .CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", validRequest);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        mockPaymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task PostPaymentAsync_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentService>();
        var paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(paymentsRepository);
                services.AddSingleton(mockPaymentService.Object);
            }))
            .CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", (PostPaymentRequest)null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        mockPaymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()), Times.Never);
    }

    [Fact]
    public async Task PostPaymentAsync_WithInvalidCurrency_ReturnsBadRequest()
    {
        // Arrange
        var invalidCurrencyRequest = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "INVALID_CURRENCY", // Moeda inválida
            Amount = 1000,
            Cvv = "123"
        };

        var mockPaymentService = new Mock<IPaymentService>();
        var paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(paymentsRepository);
                services.AddSingleton(mockPaymentService.Object);
            }))
            .CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", invalidCurrencyRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        mockPaymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<PostPaymentRequest>()), Times.Never);
    }
}