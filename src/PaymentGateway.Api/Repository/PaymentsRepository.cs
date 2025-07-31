using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository
{
    public List<PostPaymentResponse?> Payments = new();
    
    public void Add(PostPaymentResponse? acquiringBank)
    {
        Payments.Add(acquiringBank);
    }

    public PostPaymentResponse? Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}