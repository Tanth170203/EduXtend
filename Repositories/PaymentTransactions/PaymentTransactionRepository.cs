using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.PaymentTransactions;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly EduXtendContext _context;

    public PaymentTransactionRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PaymentTransaction>> GetByClubIdAsync(int clubId)
    {
        return await _context.PaymentTransactions
            .Include(t => t.Student)
            .Include(t => t.Activity)
            .Include(t => t.Semester)
            .Include(t => t.CreatedBy)
            .Where(t => t.ClubId == clubId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<PaymentTransaction?> GetByIdAsync(int id)
    {
        return await _context.PaymentTransactions
            .Include(t => t.Club)
            .Include(t => t.Student)
            .Include(t => t.Activity)
            .Include(t => t.Semester)
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<PaymentTransaction> CreateAsync(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<PaymentTransaction> UpdateAsync(PaymentTransaction transaction)
    {
        transaction.UpdatedAt = DateTime.UtcNow;
        _context.PaymentTransactions.Update(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _context.PaymentTransactions.FindAsync(id);
        if (transaction == null) return false;

        _context.PaymentTransactions.Remove(transaction);
        await _context.SaveChangesAsync();
        return true;
    }
}





