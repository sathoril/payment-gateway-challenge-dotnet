using System.Text.Json.Serialization;

namespace PaymentGateway.Domain;

public class AcquiringBankResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; set; }
    
    public bool SuccessfulRequest { get; set; }
}
