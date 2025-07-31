using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter<Currency>))]
public enum Currency
{
    USD,
    BRL,
    GBP
}