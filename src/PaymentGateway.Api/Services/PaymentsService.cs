using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsService(IBankHttpClient acquiringBankHttpClient, PaymentsRepository paymentsRepository)
    : IPaymentService
{
    public async Task<PostPaymentResponse?> ProcessPaymentAsync(PostPaymentRequest request)
    {
        var (isSuccessStatusCode, acquiringBankResponse) = await acquiringBankHttpClient.SendPaymentToBankAsync(request);
        if (isSuccessStatusCode)
        {
            var paymentStatus = acquiringBankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
            PostPaymentResponse? successfulResponse = CreateAcquiringBankResponse(request, paymentStatus);
            paymentsRepository.Add(successfulResponse);
            return successfulResponse;       
        }
        
        PostPaymentResponse? failedResponse = CreateAcquiringBankResponse(request, PaymentStatus.Rejected);
        paymentsRepository.Add(failedResponse);
        return failedResponse;
    }

    private static PostPaymentResponse? CreateAcquiringBankResponse(PostPaymentRequest request, PaymentStatus acquiringBankResponse)
    {
        return new()
        {
            ExpiryMonth = request.ExpiryMonth,
            Amount = request.Amount,
            CardNumberLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4),
            Currency = request.Currency,
            ExpiryYear = request.ExpiryYear,
            Status = acquiringBankResponse,
            Id = Guid.NewGuid()
        };
    }
}