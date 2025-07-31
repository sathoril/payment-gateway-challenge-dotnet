using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Requests;

public class PostAcquiringBankRequest(
    string cardNumber,
    int expiryMonth,
    int expiryYear,
    string currency,
    int amount,
    string cvv)
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; set; } = cardNumber;

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; } = $"{expiryMonth:00}/{expiryYear:0000}";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = currency;

    [JsonPropertyName("amount")]
    public int Amount { get; set; } = amount;

    [JsonPropertyName("cvv")]
    public string Cvv { get; set; } = cvv;
}