using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Interfaces.Repositories;
using PaymentGateway.Domain.Interfaces.Services;

namespace PaymentGateway.Application.UseCases;

public class PaymentUseCase(IAcquiringBankService acquiringBankHttpClient, IPaymentRepository paymentRepository)
    : IPaymentUseCase
{
    public async Task<Payment> ProcessPaymentAsync(string cardNumber, int expiryMonth, int expiryYear, string currency, int amount, string cvv)
    {
        var payment = new Payment(cardNumber, expiryMonth, expiryYear, currency, amount, cvv);
        
        var acquiringBankResponse = await acquiringBankHttpClient.SendPaymentToBankAsync(cardNumber, expiryMonth, expiryYear, currency, amount, cvv);

        payment.SetPaymentStatus(acquiringBankResponse.SuccessfulRequest, acquiringBankResponse.Authorized, acquiringBankResponse.AuthorizationCode);
        
        await paymentRepository.AddAsync(payment);

        return payment;
    }
}