using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.JoinRequests
{
    public class JoinRequestRepository : IJoinRequestRepository
    {
        private readonly EduXtendContext _ctx;

        public JoinRequestRepository(EduXtendContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<JoinRequest?> GetByIdAsync(int id)
        {
            return await _ctx.JoinRequests
                .AsNoTracking()
                .Include(jr => jr.Club)
                .Include(jr => jr.User)
                .Include(jr => jr.Department)
                .Include(jr => jr.ProcessedBy)
                .FirstOrDefaultAsync(jr => jr.Id == id);
        }

        public async Task<List<JoinRequest>> GetByClubIdAsync(int clubId)
        {
            return await _ctx.JoinRequests
                .AsNoTracking()
                .Include(jr => jr.Club)
                .Include(jr => jr.User)
                .Include(jr => jr.Department)
                .Include(jr => jr.ProcessedBy)
                .Where(jr => jr.ClubId == clubId)
                .OrderByDescending(jr => jr.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<JoinRequest>> GetByUserIdAsync(int userId)
        {
            return await _ctx.JoinRequests
                .AsNoTracking()
                .Include(jr => jr.Club)
                .Include(jr => jr.User)
                .Include(jr => jr.Department)
                .Include(jr => jr.ProcessedBy)
                .Where(jr => jr.UserId == userId)
                .OrderByDescending(jr => jr.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<JoinRequest>> GetPendingByClubIdAsync(int clubId)
        {
            return await _ctx.JoinRequests
                .AsNoTracking()
                .Include(jr => jr.Club)
                .Include(jr => jr.User)
                .Include(jr => jr.Department)
                .Where(jr => jr.ClubId == clubId && jr.Status == "Pending")
                .OrderBy(jr => jr.CreatedAt)
                .ToListAsync();
        }

        public async Task<JoinRequest?> GetByUserAndClubAsync(int userId, int clubId)
        {
            return await _ctx.JoinRequests
                .AsNoTracking()
                .Include(jr => jr.Club)
                .Include(jr => jr.Department)
                .FirstOrDefaultAsync(jr => jr.UserId == userId && jr.ClubId == clubId);
        }

        public async Task<JoinRequest?> GetActiveRequestByUserAndClubAsync(int userId, int clubId)
        {
            // Get the most recent request (Pending, Approved, or Rejected)
            return await _ctx.JoinRequests
                .AsNoTracking()
                .Include(jr => jr.Club)
                .Include(jr => jr.Department)
                .Include(jr => jr.ProcessedBy)
                .Where(jr => jr.UserId == userId && jr.ClubId == clubId)
                .OrderByDescending(jr => jr.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<JoinRequest> CreateAsync(JoinRequest request)
        {
            _ctx.JoinRequests.Add(request);
            await _ctx.SaveChangesAsync();
            return request;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status, int processedById)
        {
            var request = await _ctx.JoinRequests.FindAsync(id);
            if (request == null) return false;

            request.Status = status;
            request.ProcessedById = processedById;
            request.ProcessedAt = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasPendingRequestAsync(int userId, int clubId)
        {
            return await _ctx.JoinRequests
                .AnyAsync(jr => jr.UserId == userId 
                    && jr.ClubId == clubId 
                    && jr.Status == "Pending");
        }

        public async Task<bool> CreateClubMemberAsync(int clubId, int userId, int? departmentId)
        {
            try
            {
                Console.WriteLine($"[CreateClubMember] Starting - ClubId: {clubId}, UserId: {userId}, DepartmentId: {departmentId}");
                
                // Find student by userId
                var student = await _ctx.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    Console.WriteLine($"[CreateClubMember] ERROR: Student not found for UserId: {userId}");
                    throw new Exception("Student not found for this user");
                }

                Console.WriteLine($"[CreateClubMember] Found Student - StudentId: {student.Id}");

                // Check if already a member
                var existingMember = await _ctx.ClubMembers
                    .FirstOrDefaultAsync(cm => cm.ClubId == clubId && cm.StudentId == student.Id);

                if (existingMember != null)
                {
                    Console.WriteLine($"[CreateClubMember] Already a member - MemberId: {existingMember.Id}");
                    // Already a member, no need to create again
                    return true;
                }

                // Create new club member
                var clubMember = new ClubMember
                {
                    ClubId = clubId,
                    StudentId = student.Id,
                    RoleInClub = "Member", // Default role
                    DepartmentId = departmentId,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _ctx.ClubMembers.Add(clubMember);
                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[CreateClubMember] SUCCESS - Created ClubMember Id: {clubMember.Id}");
                return true;
            }
            catch (Exception ex)
            {
                // Log error but don't fail the whole operation
                Console.WriteLine($"[CreateClubMember] ERROR: {ex.Message}");
                Console.WriteLine($"[CreateClubMember] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task UpdateAsync(JoinRequest request)
        {
            _ctx.JoinRequests.Update(request);
            await _ctx.SaveChangesAsync();
        }
    }
}

