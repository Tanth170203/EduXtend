using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.FundCollectionPayments
{
    public class FundCollectionPaymentRepository : IFundCollectionPaymentRepository
    {
        private readonly EduXtendContext _context;

        public FundCollectionPaymentRepository(EduXtendContext context)
        {
            _context = context;
        }

        public async Task<FundCollectionPayment?> GetByIdAsync(int id)
        {
            return await _context.FundCollectionPayments
                .Include(p => p.FundCollectionRequest)
                .Include(p => p.ClubMember)
                    .ThenInclude(cm => cm.Student)
                .Include(p => p.ConfirmedBy)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<FundCollectionPayment?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.FundCollectionPayments
                .Include(p => p.FundCollectionRequest)
                    .ThenInclude(r => r.Club)
                .Include(p => p.ClubMember)
                    .ThenInclude(cm => cm.Student)
                .Include(p => p.PaymentTransaction)
                .Include(p => p.ConfirmedBy)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<FundCollectionPayment>> GetByRequestIdAsync(int requestId)
        {
            return await _context.FundCollectionPayments
                .Include(p => p.ClubMember)
                    .ThenInclude(cm => cm.Student)
                .Where(p => p.FundCollectionRequestId == requestId)
                .OrderBy(p => p.Status)
                .ThenBy(p => p.ClubMember.Student.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<FundCollectionPayment>> GetByRequestIdWithDetailsAsync(int requestId)
        {
            return await _context.FundCollectionPayments
                .Include(p => p.FundCollectionRequest)
                .Include(p => p.ClubMember)
                    .ThenInclude(cm => cm.Student)
                .Include(p => p.ConfirmedBy)
                .Include(p => p.PaymentTransaction)
                .Where(p => p.FundCollectionRequestId == requestId)
                .OrderBy(p => p.Status)
                .ThenBy(p => p.ClubMember.Student.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<FundCollectionPayment>> GetByClubMemberIdAsync(int clubMemberId)
        {
            return await _context.FundCollectionPayments
                .Include(p => p.FundCollectionRequest)
                .Where(p => p.ClubMemberId == clubMemberId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FundCollectionPayment>> GetPendingByRequestIdAsync(int requestId)
        {
            return await _context.FundCollectionPayments
                .Include(p => p.ClubMember)
                    .ThenInclude(cm => cm.Student)
                .Where(p => p.FundCollectionRequestId == requestId && p.Status == "pending")
                .ToListAsync();
        }

        public async Task<IEnumerable<FundCollectionPayment>> GetOverduePaymentsAsync(int clubId)
        {
            return await _context.FundCollectionPayments
                .Include(p => p.FundCollectionRequest)
                .Include(p => p.ClubMember)
                    .ThenInclude(cm => cm.Student)
                .Where(p => p.FundCollectionRequest.ClubId == clubId 
                    && p.Status == "pending"
                    && p.FundCollectionRequest.DueDate < DateTime.UtcNow
                    && p.FundCollectionRequest.Status == "active")
                .OrderBy(p => p.FundCollectionRequest.DueDate)
                .ToListAsync();
        }

        public async Task<FundCollectionPayment> CreateAsync(FundCollectionPayment payment)
        {
            _context.FundCollectionPayments.Add(payment);
            await _context.SaveChangesAsync();
            
            return (await GetByIdAsync(payment.Id))!;
        }

        public async Task<IEnumerable<FundCollectionPayment>> CreateManyAsync(IEnumerable<FundCollectionPayment> payments)
        {
            _context.FundCollectionPayments.AddRange(payments);
            await _context.SaveChangesAsync();
            
            return payments;
        }

        public async Task<FundCollectionPayment> UpdateAsync(FundCollectionPayment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _context.FundCollectionPayments.Update(payment);
            await _context.SaveChangesAsync();
            
            return (await GetByIdAsync(payment.Id))!;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var payment = await _context.FundCollectionPayments.FindAsync(id);
            if (payment == null) return false;

            _context.FundCollectionPayments.Remove(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.FundCollectionPayments.AnyAsync(p => p.Id == id);
        }
    }
}

