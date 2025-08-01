using System.Text.Json.Serialization;

namespace PaymentGateway.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<Currency>))]
public enum Currency
{
    USD,
    BRL,
    GBP
}