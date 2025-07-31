using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class AcquiringBankHttpClient(HttpClient httpClient) : IBankHttpClient
{
    public async Task<(bool IsSuccessStatusCode, PostAcquiringBankResponse?)> SendPaymentToBankAsync(PostPaymentRequest request)
    {
        var bankRequest = new PostAcquiringBankRequest(
            request.CardNumber, 
            request.ExpiryMonth,
            request.ExpiryYear, 
            request.Currency, 
            request.Amount, 
            request.Cvv);
        
        var paymentGatewayResponse =
            await httpClient
                .PostAsJsonAsync("payments", bankRequest);

        var response = (
            paymentGatewayResponse.IsSuccessStatusCode,
            await paymentGatewayResponse.Content.ReadFromJsonAsync<PostAcquiringBankResponse>()
        ); 

        return response;
    }
}