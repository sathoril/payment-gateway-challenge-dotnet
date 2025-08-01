using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Domain.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<Payment?> GetByIdAsync(Guid id);
}