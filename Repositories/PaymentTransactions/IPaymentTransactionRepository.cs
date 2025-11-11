using BusinessObject.Models;

namespace Repositories.PaymentTransactions;

public interface IPaymentTransactionRepository
{
    Task<IEnumerable<PaymentTransaction>> GetByClubIdAsync(int clubId);
    Task<PaymentTransaction?> GetByIdAsync(int id);
    Task<PaymentTransaction> CreateAsync(PaymentTransaction transaction);
    Task<PaymentTransaction> UpdateAsync(PaymentTransaction transaction);
    Task<bool> DeleteAsync(int id);
}

