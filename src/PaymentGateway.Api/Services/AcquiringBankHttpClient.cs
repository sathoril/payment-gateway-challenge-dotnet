using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class AcquiringBankHttpClient(HttpClient httpClient) : IBankHttpClient
{
    public async Task<(bool IsSuccessStatusCode, PostAcquiringBankResponse?)> SendPaymentToBankAsync(PostPaymentRequest request)
    {
        PostAcquiringBankRequest bankRequest = new()
        {
            CardNumber = request.CardNumber, 
            ExpiryDate = $"{request.ExpiryMonth:00}/{request.ExpiryYear:0000}",
            Currency = request.Currency, 
            Amount = request.Amount, 
            Cvv = request.Cvv
        };
        
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