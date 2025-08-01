using PaymentGateway.Application.Models.Responses;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Interfaces.Repositories;

namespace PaymentGateway.Application.Services;

public class PaymentRepository : IPaymentRepository
{
    public List<Payment?> Payments = new();
    
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