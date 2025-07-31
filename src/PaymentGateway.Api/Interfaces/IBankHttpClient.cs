using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Interfaces;

public interface IBankHttpClient
{
    Task<(bool IsSuccessStatusCode, PostAcquiringBankResponse?)> SendPaymentToBankAsync(PostPaymentRequest request);
}