using System.Net.Http.Json;

using PaymentGateway.Domain;
using PaymentGateway.Domain.Interfaces.Services;

namespace PaymentGateway.Infrastructure.HttpClients;

public class AcquiringBankService(HttpClient httpClient) : IAcquiringBankService
{

    public async Task<AcquiringBankResponse> SendPaymentToBankAsync(string cardNumber, int expiryMonth, int expiryYear, string currency, int amount, string cvv)
    {
        try
        {
            PostAcquiringBankRequest bankRequest = new()
            {
                CardNumber = cardNumber, 
                ExpiryDate = $"{expiryMonth:00}/{expiryYear:0000}",
                Currency = currency, 
                Amount = amount, 
                Cvv = cvv
            };
        
            var paymentGatewayResponse =
                await httpClient
                    .PostAsJsonAsync("payments", bankRequest);
        
            if (paymentGatewayResponse.IsSuccessStatusCode)
            {
                var response = await paymentGatewayResponse.Content.ReadFromJsonAsync<AcquiringBankResponse>();
                return new AcquiringBankResponse()
                {
                    Authorized = response.Authorized,
                    AuthorizationCode = response.AuthorizationCode,
                    SuccessfulRequest = true
                };
            } 

            return new AcquiringBankResponse()
            {
                SuccessfulRequest = false
            };
        }
        catch
        {
            return new AcquiringBankResponse()
            {
                SuccessfulRequest = false
            };
        }
    }
}