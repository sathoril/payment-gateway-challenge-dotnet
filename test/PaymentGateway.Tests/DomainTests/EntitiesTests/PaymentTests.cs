using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Tests.DomainTests;

public class PaymentTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreatePaymentSuccessfully()
    {
        // Arrange
        var cardNumber = "1234567890123456";
        var expiryMonth = 12;
        var expiryYear = DateTime.Now.Year + 1;
        var currency = "USD";
        var amount = 1000;
        var cvv = "123";

        // Act
        var payment = new Payment(cardNumber, expiryMonth, expiryYear, currency, amount, cvv);

        // Assert
        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal("3456", payment.CardNumberLastFour);
        Assert.Equal(expiryMonth, payment.ExpiryMonth);
        Assert.Equal(expiryYear, payment.ExpiryYear);
        Assert.Equal(Currency.USD, payment.Currency);
        Assert.Equal(amount, payment.Amount);
        Assert.Equal(cvv, payment.CVV);
        Assert.Equal(PaymentStatus.Authorized, payment.Status); // Default status
    }

    #region Card Number Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCardNumber_NullOrEmpty_ShouldThrowArgumentException(string cardNumber)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment(cardNumber, 12, DateTime.Now.Year + 1, "USD", 1000, "123"));
        
        Assert.Equal("Card number is required", exception.Message);
    }

    [Theory]
    [InlineData("123456789012")] // 12 digits - too short
    [InlineData("1234567890123")] // 13 digits - too short
    [InlineData("12345678901234567890")] // 20 digits - too long
    public void Constructor_WithInvalidCardNumberLength_ShouldThrowArgumentException(string cardNumber)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment(cardNumber, 12, DateTime.Now.Year + 1, "USD", 1000, "123"));
        
        Assert.Equal("Card number must be between 14-19 characters", exception.Message);
    }

    [Theory]
    [InlineData("123456789012345a")] // Contains letter
    [InlineData("1234567890123456!")] // Contains special character
    [InlineData("1234 5678 9012 3456")] // Contains spaces
    public void Constructor_WithNonNumericCardNumber_ShouldThrowArgumentException(string cardNumber)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment(cardNumber, 12, DateTime.Now.Year + 1, "USD", 1000, "123"));
        
        Assert.Equal("Card number must contain only numeric characters", exception.Message);
    }

    [Theory]
    [InlineData("12345678901234", "1234")] // 14 digits
    [InlineData("123456789012345", "2345")] // 15 digits
    [InlineData("1234567890123456", "3456")] // 16 digits
    [InlineData("12345678901234567", "4567")] // 17 digits
    [InlineData("123456789012345678", "5678")] // 18 digits
    [InlineData("1234567890123456789", "6789")] // 19 digits
    public void Constructor_WithValidCardNumberLength_ShouldExtractLastFourDigitsCorrectly(string cardNumber, string expectedLastFour)
    {
        // Arrange & Act
        var payment = new Payment(cardNumber, 12, DateTime.Now.Year + 1, "USD", 1000, "123");

        // Assert
        Assert.Equal(expectedLastFour, payment.CardNumberLastFour);
    }

    #endregion

    #region Expiry Month Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(15)]
    public void Constructor_WithInvalidExpiryMonth_ShouldThrowArgumentException(int expiryMonth)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", expiryMonth, DateTime.Now.Year + 1, "USD", 1000, "123"));
        
        Assert.Equal("Expiry month must be between 1-12", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void Constructor_WithValidExpiryMonth_ShouldSetCorrectly(int expiryMonth)
    {
        // Arrange & Act
        var payment = new Payment("1234567890123456", expiryMonth, DateTime.Now.Year + 1, "USD", 1000, "123");

        // Assert
        Assert.Equal(expiryMonth, payment.ExpiryMonth);
    }

    #endregion

    #region Expiry Year Tests

    [Fact]
    public void Constructor_WithPastYear_ShouldThrowArgumentException()
    {
        // Arrange
        var currentYear = DateTime.Now.AddYears(-1).Year;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", 12, currentYear, "USD", 1000, "123"));
        
        Assert.Equal("Expiry year must be greater or equal than current year", exception.Message);
    }

    [Fact]
    public void Constructor_WithFutureExpiryYear_ShouldSetCorrectly()
    {
        // Arrange
        var futureYear = DateTime.Now.Year + 2;

        // Act
        var payment = new Payment("1234567890123456", 12, futureYear, "USD", 1000, "123");

        // Assert
        Assert.Equal(futureYear, payment.ExpiryYear);
    }

    #endregion

    #region Expiry Date Validation Tests

    [Fact]
    public void Constructor_WithExpiredDate_ShouldThrowArgumentExceptionWithExpectedMessage()
    {
        // Arrange
        var currentDate = DateTime.Now;
        var expiredMonth = currentDate.Month - 1;
        var expiredYear = currentDate.Year;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", expiredMonth, expiredYear, "USD", 1000, "123"));
        
        Assert.Equal("Expiry date must be in the future", exception.Message);
    }

    #endregion

    #region Currency Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCurrency_NullOrEmpty_ShouldThrowArgumentExceptionWithExpectedMessage(string currency)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", 12, DateTime.Now.Year + 1, currency, 1000, "123"));
        
        Assert.Equal("Currency is required", exception.Message);
    }

    [Theory]
    [InlineData("US")] // Too short
    [InlineData("USDD")] // Too long
    public void Constructor_WithInvalidCurrencyLength_ShouldThrowArgumentExceptionWithExpectedMessage(string currency)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", 12, DateTime.Now.Year + 1, currency, 1000, "123"));
        
        Assert.Equal("Currency must be 3 characters", exception.Message);
    }

    [Theory]
    [InlineData("USD", Currency.USD)]
    [InlineData("BRL", Currency.BRL)]
    [InlineData("GBP", Currency.GBP)]
    public void Constructor_WithValidCurrency_ShouldSetCorrectly(string currencyString, Currency expectedCurrency)
    {
        // Arrange & Act
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, currencyString, 1000, "123");

        // Assert
        Assert.Equal(expectedCurrency, payment.Currency);
    }

    #endregion

    #region Amount Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidAmount_ShouldThrowArgumentExceptionWithExpectedMessage(int amount)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", amount, "123"));
        
        Assert.Equal("Amount must be greater than zero", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Constructor_WithValidAmount_ShouldSetCorrectly(int amount)
    {
        // Arrange & Act
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", amount, "123");

        // Assert
        Assert.Equal(amount, payment.Amount);
    }

    #endregion

    #region CVV Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCVV_NullOrEmpty_ShouldThrowArgumentExceptionWithExpectedMessage(string cvv)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, cvv));
        
        Assert.Equal("CVV is required", exception.Message);
    }

    [Theory]
    [InlineData("12")] // Too short
    [InlineData("12345")] // Too long
    public void Constructor_WithInvalidCVVLength_ShouldThrowArgumentExceptionWithExpectedMessage(string cvv)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, cvv));
        
        Assert.Equal("CVV must be 3-4 characters long", exception.Message);
    }

    [Theory]
    [InlineData("12a")] // Contains letter
    [InlineData("12!")] // Contains special character
    [InlineData("1 3")] // Contains space
    public void Constructor_WithNonNumericCVV_ShouldThrowArgumentExceptionWithExpectedMessage(string cvv)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, cvv));
        
        Assert.Equal("CVV must contain only numeric characters", exception.Message);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void Constructor_WithValidCVV_ShouldSetCorrectly(string cvv)
    {
        // Arrange & Act
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, cvv);

        // Assert
        Assert.Equal(cvv, payment.CVV);
    }

    #endregion

    #region Payment Status Methods Tests

    [Fact]
    public void SetAuthorized_ShouldSetStatusAndAuthorizationCode()
    {
        // Arrange
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, "123");
        var authCode = "AUTH123456";

        // Act
        payment.SetAuthorized(authCode);

        // Assert
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal(authCode, payment.AuthorizationCode);
    }

    [Fact]
    public void SetRejected_ShouldSetStatusToRejected()
    {
        // Arrange
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, "123");

        // Act
        payment.SetRejected();

        // Assert
        Assert.Equal(PaymentStatus.Rejected, payment.Status);
    }

    [Fact]
    public void SetDeclined_ShouldSetStatusToDeclined()
    {
        // Arrange
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, "123");

        // Act
        payment.SetDeclined();

        // Assert
        Assert.Equal(PaymentStatus.Declined, payment.Status);
    }

    #endregion

    #region SetPaymentStatus Method Tests

    [Fact]
    public void SetPaymentStatus_WhenBankRequestUnsuccessful_ShouldSetRejected()
    {
        // Arrange
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, "123");

        // Act
        payment.SetPaymentStatus(isBankRequestSuccessful: false, authorized: true, authorizationCode: "AUTH123");

        // Assert
        Assert.Equal(PaymentStatus.Rejected, payment.Status);
    }

    [Fact]
    public void SetPaymentStatus_WhenBankRequestSuccessfulAndAuthorized_ShouldSetAuthorized()
    {
        // Arrange
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, "123");
        var authCode = "AUTH123456";

        // Act
        payment.SetPaymentStatus(isBankRequestSuccessful: true, authorized: true, authorizationCode: authCode);

        // Assert
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal(authCode, payment.AuthorizationCode);
    }

    [Fact]
    public void SetPaymentStatus_WhenBankRequestSuccessfulButNotAuthorized_ShouldSetDeclined()
    {
        // Arrange
        var payment = new Payment("1234567890123456", 12, DateTime.Now.Year + 1, "USD", 1000, "123");

        // Act
        payment.SetPaymentStatus(isBankRequestSuccessful: true, authorized: false, authorizationCode: "");

        // Assert
        Assert.Equal(PaymentStatus.Declined, payment.Status);
    }

    #endregion
}