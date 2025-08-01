using PaymentGateway.Domain.ExternalServices.Responses;

namespace PaymentGateway.Domain.Interfaces.Services;

public interface IAcquiringBankService
{
    Task<AcquiringBankResponse> SendPaymentToBankAsync(string cardNumber, int expiryMonth, int expiryYear, string currency, int amount, string cvv);
}