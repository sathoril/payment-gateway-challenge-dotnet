using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

public class Payment
{
    public Payment(string cardNumber, int expiryMonth, int expiryYear, string currency, int amount, string cvv)
    {
        Id = Guid.NewGuid();
        CardNumberLastFour = SetCardNumberLastFour(cardNumber);
        ExpiryMonth = SetExpiryMonth(expiryMonth);
        ExpiryYear = SetExpiryYear(expiryYear);
        Currency = SetCurrency(currency);
        Amount = SetAmount(amount);
        CVV = SetCVV(cvv);
        
        ValidateExpiryDate();
    }

    public Guid Id { get; private set; }
    public string CardNumberLastFour { get; private set; }
    public int ExpiryMonth { get; private set; }
    public int ExpiryYear { get; private set; }
    public Currency Currency { get; private set; }
    public int Amount { get; private set; }
    public string CVV { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string AuthorizationCode { get; set; }
    
    public void SetAuthorized(string authorizationCode)
    {
        Status = PaymentStatus.Authorized;
        AuthorizationCode = authorizationCode;
    }

    public void SetRejected()
    {
        Status = PaymentStatus.Rejected;
    }
    
    public void SetDeclined()
    {
        Status = PaymentStatus.Declined;
    }
    
    private string SetCardNumberLastFour(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new ArgumentException("Card number is required");

        if (cardNumber.Length < 14 || cardNumber.Length > 19)
            throw new ArgumentException("Card number must be between 14-19 characters");

        if (!cardNumber.All(Char.IsDigit))
            throw new ArgumentException("Card number must contain only numeric characters");

        return cardNumber.Substring(cardNumber.Length - 4);
    }
    
    private void ValidateExpiryDate()
    {
        var currentDate = DateTime.Now;
        var expiryDate = new DateTime(ExpiryYear, ExpiryMonth, DateTime.DaysInMonth(ExpiryYear, ExpiryMonth));

        if (expiryDate <= currentDate)
            throw new ArgumentException("Expiry date must be in the future");
    }

    private int SetExpiryYear(int year)
    {
        if (year < DateTime.Now.Year)
            throw new ArgumentException("Expiry year must be greater or equal than current year");

        return year;
    }

    private int SetExpiryMonth(int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentException("Expiry month must be between 1-12");

        return month;
    }

    private string SetCVV(string cvv)
    {
        if (string.IsNullOrWhiteSpace(cvv))
            throw new ArgumentException("CVV is required");

        if (cvv.Length < 3 || cvv.Length > 4)
            throw new ArgumentException("CVV must be 3-4 characters long");

        if (!cvv.All(Char.IsDigit))
            throw new ArgumentException("CVV must contain only numeric characters");

        return cvv;
    }

    private int SetAmount(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");

        return amount;
    }

    private Currency SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required");

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be 3 characters");
        
        if (!Enum.IsDefined(typeof(Currency), Currency))
            throw new ArgumentException($"Currency {currency} is not supported");
        
        return Enum.Parse<Currency>(currency);
    }

    public void SetPaymentStatus(bool isBankRequestSuccessful, bool authorized, string authorizationCode)
    {
        if (!isBankRequestSuccessful)
            SetRejected();
        else if (authorized)
            SetAuthorized(authorizationCode);
        else
            SetDeclined();
    }
}