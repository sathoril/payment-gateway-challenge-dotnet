using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Interfaces.Repositories;

namespace PaymentGateway.Infrastructure.Repository;

public class PaymentRepository : IPaymentRepository
{
    private List<Payment?> Payments = new();
    
    public Task AddAsync(Payment payment)
    {
        Payments.Add(payment);
        return Task.CompletedTask;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await Task.FromResult<Payment>(Payments.FirstOrDefault(p => p.Id == id));
    }
}