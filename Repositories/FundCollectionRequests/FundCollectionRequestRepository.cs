using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.FundCollectionRequests
{
    public class FundCollectionRequestRepository : IFundCollectionRequestRepository
    {
        private readonly EduXtendContext _context;

        public FundCollectionRequestRepository(EduXtendContext context)
        {
            _context = context;
        }

        public async Task<FundCollectionRequest?> GetByIdAsync(int id)
        {
            return await _context.FundCollectionRequests
                .Include(f => f.Club)
                .Include(f => f.CreatedBy)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<FundCollectionRequest?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.FundCollectionRequests
                .Include(f => f.Club)
                .Include(f => f.CreatedBy)
                .Include(f => f.Payments)
                    .ThenInclude(p => p.ClubMember)
                        .ThenInclude(cm => cm.Student)
                .Include(f => f.Payments)
                    .ThenInclude(p => p.ConfirmedBy)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<FundCollectionRequest>> GetByClubIdAsync(int clubId)
        {
            return await _context.FundCollectionRequests
                .Include(f => f.CreatedBy)
                .Include(f => f.Payments)
                .Where(f => f.ClubId == clubId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FundCollectionRequest>> GetActiveByClubIdAsync(int clubId)
        {
            return await _context.FundCollectionRequests
                .Include(f => f.CreatedBy)
                .Include(f => f.Payments)
                .Where(f => f.ClubId == clubId && f.Status == "active")
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<FundCollectionRequest> CreateAsync(FundCollectionRequest request)
        {
            _context.FundCollectionRequests.Add(request);
            await _context.SaveChangesAsync();
            
            // Reload with relationships
            return (await GetByIdAsync(request.Id))!;
        }

        public async Task<FundCollectionRequest> UpdateAsync(FundCollectionRequest request)
        {
            request.UpdatedAt = DateTime.UtcNow;
            _context.FundCollectionRequests.Update(request);
            await _context.SaveChangesAsync();
            
            return (await GetByIdAsync(request.Id))!;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var request = await _context.FundCollectionRequests.FindAsync(id);
            if (request == null) return false;

            _context.FundCollectionRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.FundCollectionRequests.AnyAsync(f => f.Id == id);
        }

        public async Task<int> GetTotalMembersAsync(int clubId)
        {
            return await _context.ClubMembers
                .Where(cm => cm.ClubId == clubId)
                .CountAsync();
        }
    }
}

