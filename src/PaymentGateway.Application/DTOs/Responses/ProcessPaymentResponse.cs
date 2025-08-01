using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.DTOs.Responses;

public class ProcessPaymentResponse
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public string CardNumberLastFour { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public Currency Currency { get; set; }
    public int Amount { get; set; }
}