using Moq;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.ServiceTests;

public class PaymentsServiceTests
{
    private readonly Mock<IBankHttpClient> _mockBankHttpClient;
    private readonly PaymentsRepository _paymentsRepository;
    private readonly PaymentsService _paymentsService;
    private readonly Random _random = new();

    public PaymentsServiceTests()
    {
        _mockBankHttpClient = new Mock<IBankHttpClient>();
        _paymentsRepository = new PaymentsRepository();
        _paymentsService = new PaymentsService(_mockBankHttpClient.Object, _paymentsRepository);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidRequestAndSuccessfulBankResponse_ReturnsAuthorizedPayment()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync((true, bankResponse));

        // Act
        var result = await _paymentsService.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(PaymentStatus.Authorized, result.Status);
        Assert.Equal("5678", result.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, result.ExpiryYear);
        Assert.Equal(request.Currency, result.Currency);
        Assert.Equal(request.Amount, result.Amount);

        _mockBankHttpClient.Verify(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidRequestAndDeclinedBankResponse_ReturnsDeclinedPayment()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = false,
            AuthorizationCode = null
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync((true, bankResponse));

        // Act
        var result = await _paymentsService.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(PaymentStatus.Declined, result.Status);
        Assert.Equal("5678", result.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, result.ExpiryYear);
        Assert.Equal(request.Currency, result.Currency);
        Assert.Equal(request.Amount, result.Amount);

        _mockBankHttpClient.Verify(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithBankHttpError_ReturnsRejectedPayment()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync((false, (PostAcquiringBankResponse?)null));

        // Act
        var result = await _paymentsService.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(PaymentStatus.Rejected, result.Status);
        Assert.Equal("5678", result.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, result.ExpiryYear);
        Assert.Equal(request.Currency, result.Currency);
        Assert.Equal(request.Amount, result.Amount);

        _mockBankHttpClient.Verify(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithFailedAcquiringBankResponse_ReturnsRejectedPayment()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync((false, (PostAcquiringBankResponse?)null));

        // Act
        var result = await _paymentsService.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(PaymentStatus.Rejected, result.Status);
        Assert.Equal("5678", result.CardNumberLastFour);

        _mockBankHttpClient.Verify(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SavesPaymentToRepository()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync((true, bankResponse));

        // Act
        var result = await _paymentsService.ProcessPaymentAsync(request);

        // Assert
        var savedPayment = _paymentsRepository.Get(result.Id);
        Assert.NotNull(savedPayment);
        Assert.Equal(result.Id, savedPayment.Id);
        Assert.Equal(result.Status, savedPayment.Status);
        Assert.Equal(result.CardNumberLastFour, savedPayment.CardNumberLastFour);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithDifferentCardNumbers_ReturnsCorrectLastFourDigits()
    {
        // Arrange
        var testCases = new[]
        {
            ("1234567812345678", "5678"),
            ("9876543210123456", "3456"),
            ("1111222233334444", "4444"),
            ("5555666677778888", "8888")
        };

        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync((true, bankResponse));

        foreach (var (cardNumber, expectedLastFour) in testCases)
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = cardNumber,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var result = await _paymentsService.ProcessPaymentAsync(request);

            // Assert
            Assert.Equal(expectedLastFour, result.CardNumberLastFour);
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankThrowsException_ThrowsException()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ThrowsAsync(new HttpRequestException("Bank service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _paymentsService.ProcessPaymentAsync(request));

        _mockBankHttpClient.Verify(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_GeneratesUniquePaymentIds()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567812345678",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync((true, bankResponse));

        // Act
        var result1 = await _paymentsService.ProcessPaymentAsync(request);
        var result2 = await _paymentsService.ProcessPaymentAsync(request);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
        Assert.NotEqual(Guid.Empty, result1.Id);
        Assert.NotEqual(Guid.Empty, result2.Id);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithDifferentCurrencies_PreservesOriginalCurrency()
    {
        // Arrange
        var currencies = new[] { "USD", "EUR", "GBP", "JPY" };
        var bankResponse = new PostAcquiringBankResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _mockBankHttpClient
            .Setup(x => x.SendPaymentToBankAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync((true, bankResponse));

        foreach (var currency in currencies)
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "1234567812345678",
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = currency,
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var result = await _paymentsService.ProcessPaymentAsync(request);

            // Assert
            Assert.Equal(currency, result.Currency);
        }
    }
}
