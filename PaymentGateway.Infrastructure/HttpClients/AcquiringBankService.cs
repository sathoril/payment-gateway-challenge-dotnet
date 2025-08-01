using System.Net.Http.Json;

using PaymentGateway.Application.Models.Requests;
using PaymentGateway.Application.Models.Responses;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Interfaces.Services;

namespace PaymentGateway.Application.Services;

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