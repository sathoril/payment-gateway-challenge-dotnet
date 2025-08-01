using Moq;
using Moq.AutoMock;

using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.UseCases;
using PaymentGateway.Domain;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ExternalServices.Responses;
using PaymentGateway.Domain.Interfaces.Repositories;
using PaymentGateway.Domain.Interfaces.Services;

namespace PaymentGateway.Api.Tests.Application.ServiceTests;

public class PaymentUseCaseTests
{
    private readonly AutoMocker _autoMocker = new();
    private readonly Mock<IAcquiringBankService> _mockAcquiringBankService;
    private readonly Mock<IPaymentRepository> _mockPaymentsRepository;
    private readonly PaymentUseCase _paymentUseCase;

    public PaymentUseCaseTests()
    {
        _mockAcquiringBankService = _autoMocker.GetMock<IAcquiringBankService>();
        _mockPaymentsRepository = _autoMocker.GetMock<IPaymentRepository>();
        _paymentUseCase = new PaymentUseCase(_mockAcquiringBankService.Object, _mockPaymentsRepository.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidRequestAndSuccessfulBankResponse_ReturnsAuthorizedPayment()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new AcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123",
            SuccessfulRequest = true
        };

        _mockAcquiringBankService
            .Setup(x => 
                x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _paymentUseCase.ProcessPaymentAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(PaymentStatus.Authorized, result.Status);
        Assert.Equal("5678", result.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, result.ExpiryYear);
        Assert.Equal(request.Currency, result.Currency.ToString());
        Assert.Equal(request.Amount, result.Amount);

        _mockAcquiringBankService.Verify(x => x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidRequestAndDeclinedBankResponse_ReturnsDeclinedPayment()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new AcquiringBankResponse
        {
            Authorized = false,
            AuthorizationCode = null,
            SuccessfulRequest = true
        };

        _mockAcquiringBankService
            .Setup(x => x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _paymentUseCase.ProcessPaymentAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(PaymentStatus.Declined, result.Status);
        Assert.Equal("5678", result.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, result.ExpiryYear);
        Assert.Equal(request.Currency, result.Currency.ToString());
        Assert.Equal(request.Amount, result.Amount);

        _mockAcquiringBankService.Verify(x => x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithBankHttpError_ReturnsRejectedPayment()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new AcquiringBankResponse() { SuccessfulRequest = false };
        
        _mockAcquiringBankService
            .Setup(x => x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _paymentUseCase.ProcessPaymentAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(PaymentStatus.Rejected, result.Status);
        Assert.Equal("5678", result.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, result.ExpiryYear);
        Assert.Equal(request.Currency, result.Currency.ToString());
        Assert.Equal(request.Amount, result.Amount);

        _mockAcquiringBankService.Verify(x => x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithFailedAcquiringBankResponse_ReturnsRejectedPayment()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new AcquiringBankResponse() { SuccessfulRequest = false };
        
        _mockAcquiringBankService
            .Setup(x => x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _paymentUseCase.ProcessPaymentAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(PaymentStatus.Rejected, result.Status);
        Assert.Equal("5678", result.CardNumberLastFour);

        _mockAcquiringBankService.Verify(x => x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SavesPaymentToRepository()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new AcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123",
            SuccessfulRequest = true
        };

        _mockAcquiringBankService
            .Setup(x => x.SendPaymentToBankAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv))
            .ReturnsAsync(bankResponse);
        
        // Act
        var result = await _paymentUseCase.ProcessPaymentAsync(request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount, request.Cvv);

        // Assert
        _mockPaymentsRepository.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Once);
    }
}
