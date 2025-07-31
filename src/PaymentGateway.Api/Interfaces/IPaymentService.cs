using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Interfaces;

public interface IPaymentService
{
    Task<PostPaymentResponse?> ProcessPaymentAsync(PostPaymentRequest request);
}