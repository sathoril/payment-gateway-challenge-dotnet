using System.Net;
using System.Net.Http.Json;

using AutoMapper;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;
using Moq.AutoMock;

using PaymentGateway.Application.Controllers;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Domain;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ExternalServices.Responses;
using PaymentGateway.Domain.Interfaces.Repositories;
using PaymentGateway.Domain.Interfaces.Services;

namespace PaymentGateway.Api.Tests.Application.ControllerTests;

public class PaymentControllerTests
{
    private readonly AutoMocker _autoMocker = new();
    
    [Fact]
    public async Task GetPayment_ReturnsPaymentSuccessfully_IfExists()
    {
        // Arrange
        var payment = new Payment("1234123412341234", DateTime.Now.AddMonths(1).Month, DateTime.Now.Year, nameof(Currency.BRL), 1000, "1234");;
        var mappedResponse = new GetPaymentByIdResponse()
        {
            ExpiryYear = payment.ExpiryYear,
            Id = payment.Id,
            Status = payment.Status,
            Amount = payment.Amount,
            CardNumberLastFour = payment.CardNumberLastFour,
            Currency = payment.Currency,
            ExpiryMonth = payment.ExpiryMonth
        };
        
        _autoMocker
            .GetMock<IPaymentRepository>()
            .Setup(x => x.GetByIdAsync(payment.Id))
            .ReturnsAsync(payment);
        
        _autoMocker
            .GetMock<IMapper>()
            .Setup(x => x.Map<Payment, GetPaymentByIdResponse>(It.IsAny<Payment>()))
            .Returns(mappedResponse);
        
        HttpClient client = CreateWebApplicationMocked();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadAsStringAsync();
    
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }
    
    [Fact]
    public async Task GetPayment_WithValidRequest_UnhandledExceptionHappens_ReturnsInternalServerError()
    {
        // Arrange
        
        HttpClient client = CreateWebApplicationMocked();
        
        _autoMocker.GetMock<IPaymentRepository>()
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Unhandled exception"));
    
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid().ToString()}");
    
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Verify(x => 
                x.ProcessPaymentAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), 
                    It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPayment_Returns404Error_IfPaymentDoesntExists()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentController>();
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
        var validRequest = new ProcessPaymentRequest()
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };
    
        var payment = new Payment(validRequest.CardNumber, validRequest.ExpiryMonth, validRequest.ExpiryYear, validRequest.Currency, validRequest.Amount, validRequest.Cvv);;
        
        var expectedResponse = new ProcessPaymentResponse()
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "5678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = Currency.USD,
            Amount = 1000
        };

        var acquiringBankResponse = new AcquiringBankResponse()
        {
            Authorized = true, AuthorizationCode = "123123", SuccessfulRequest = true
        };
    
        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Setup(x => x.ProcessPaymentAsync(validRequest.CardNumber, validRequest.ExpiryMonth, validRequest.ExpiryYear, validRequest.Currency, validRequest.Amount, validRequest.Cvv))
            .ReturnsAsync(payment);
        
        _autoMocker
            .GetMock<IMapper>()
            .Setup(x => x.Map<Payment, ProcessPaymentResponse>(payment))
            .Returns(expectedResponse);
        
        _autoMocker
            .GetMock<IAcquiringBankService>()
            .Setup(x => x.SendPaymentToBankAsync(validRequest.CardNumber, validRequest.ExpiryMonth, validRequest.ExpiryYear, validRequest.Currency, validRequest.Amount, validRequest.Cvv))
            .ReturnsAsync(acquiringBankResponse);
    
        HttpClient client = CreateWebApplicationMocked();
    
        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", validRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<ProcessPaymentResponse>();
    
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(expectedResponse.Id, paymentResponse.Id);
        Assert.Equal(expectedResponse.Status, paymentResponse.Status);
        
        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Verify(x => 
                x.ProcessPaymentAsync(
                    validRequest.CardNumber, validRequest.ExpiryMonth, validRequest.ExpiryYear, validRequest.Currency, validRequest.Amount, validRequest.Cvv), Times.Once);
    }
    
    [Fact]
    public async Task PostPaymentAsync_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new ProcessPaymentRequest()
        {
            CardNumber = "123", // Invalid CardNumber
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "BRL",
            Amount = 100,
            Cvv = "123"
        };

        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Setup(x => x.ProcessPaymentAsync(invalidRequest.CardNumber, invalidRequest.ExpiryMonth,
                invalidRequest.ExpiryYear, invalidRequest.Currency, invalidRequest.Amount, invalidRequest.Cvv))
            .ThrowsAsync(new ArgumentException("Card number must be between 14-19 characters"));
            
        HttpClient client = CreateWebApplicationMocked();
    
        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", invalidRequest);
    
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Verify(x => x.ProcessPaymentAsync(invalidRequest.CardNumber, invalidRequest.ExpiryMonth,
                invalidRequest.ExpiryYear, invalidRequest.Currency, invalidRequest.Amount, invalidRequest.Cvv), Times.Once);
    }
    
    [Fact]
    public async Task PostPaymentAsync_WithInvalidRequest_ReturnsBadRequestWithExpectedDetailMessageSameAsExceptionMessage()
    {
        // Arrange
        var invalidRequest = new ProcessPaymentRequest()
        {
            CardNumber = "123", // Invalid CardNumber
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "BRL",
            Amount = 100,
            Cvv = "123"
        };

        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Setup(x => x.ProcessPaymentAsync(invalidRequest.CardNumber, invalidRequest.ExpiryMonth,
                invalidRequest.ExpiryYear, invalidRequest.Currency, invalidRequest.Amount, invalidRequest.Cvv))
            .ThrowsAsync(new ArgumentException("Card number must be between 14-19 characters"));
            
        HttpClient client = CreateWebApplicationMocked();
    
        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", invalidRequest);
    
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Card number must be between 14-19 characters", response.Content.ReadAsStringAsync().Result);
        
        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Verify(x => x.ProcessPaymentAsync(invalidRequest.CardNumber, invalidRequest.ExpiryMonth,
                invalidRequest.ExpiryYear, invalidRequest.Currency, invalidRequest.Amount, invalidRequest.Cvv), Times.Once);
    }
    
    [Fact]
    public async Task PostPaymentAsync_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var validRequest = new ProcessPaymentRequest()
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Setup(x => x.ProcessPaymentAsync(validRequest.CardNumber, validRequest.ExpiryMonth,
                validRequest.ExpiryYear, validRequest.Currency, validRequest.Amount, validRequest.Cvv))
            .ThrowsAsync(new Exception("Service unavailable"));
    
        HttpClient client = CreateWebApplicationMocked();
    
        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", validRequest);
    
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Verify(x => x.ProcessPaymentAsync(validRequest.CardNumber, validRequest.ExpiryMonth,
                validRequest.ExpiryYear, validRequest.Currency, validRequest.Amount, validRequest.Cvv), Times.Once);
    }
    
    [Fact]
    public async Task PostPaymentAsync_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        
        HttpClient client = CreateWebApplicationMocked();
    
        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", (ProcessPaymentRequest)null);
    
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _autoMocker
            .GetMock<IPaymentUseCase>()
            .Verify(x => 
                x.ProcessPaymentAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), 
                It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }
    
    private HttpClient CreateWebApplicationMocked()
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(_autoMocker.Get<IPaymentRepository>());
                    services.AddSingleton(_autoMocker.Get<IPaymentUseCase>());
                    services.AddSingleton(_autoMocker.Get<IAcquiringBankService>());
                    services.AddSingleton(_autoMocker.Get<IMapper>());
                }))
            .CreateClient();
        return client;
    }
}