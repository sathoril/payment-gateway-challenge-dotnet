using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Domain.Interfaces.Services;

public interface IPaymentUseCase
{
    Task<Payment> ProcessPaymentAsync(
        string cardNumber,
        int expiryMonth,
        int expiryYear,
        string currency,
        int amount,
        string cvv);
}