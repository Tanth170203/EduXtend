using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ClubMembers
{
    public class ClubMemberRepository : IClubMemberRepository
    {
        private readonly EduXtendContext _context;

        public ClubMemberRepository(EduXtendContext context)
        {
            _context = context;
        }

        public async Task<ClubMember?> GetByIdAsync(int id)
        {
            return await _context.ClubMembers
                .Include(cm => cm.Student)
                .Include(cm => cm.Club)
                .Include(cm => cm.Department)
                .FirstOrDefaultAsync(cm => cm.Id == id);
        }

        public async Task<ClubMember?> GetByClubAndStudentIdAsync(int clubId, int studentId)
        {
            return await _context.ClubMembers
                .Include(cm => cm.Student)
                .Include(cm => cm.Club)
                .Include(cm => cm.Department)
                .FirstOrDefaultAsync(cm => cm.ClubId == clubId && cm.StudentId == studentId);
        }

        public async Task<ClubMember?> GetByClubAndUserIdAsync(int clubId, int userId)
        {
            return await _context.ClubMembers
                .Include(cm => cm.Student)
                .Include(cm => cm.Club)
                .Include(cm => cm.Department)
                .FirstOrDefaultAsync(cm => cm.ClubId == clubId && cm.Student.UserId == userId);
        }

        public async Task<IEnumerable<ClubMember>> GetByClubIdAsync(int clubId)
        {
            return await _context.ClubMembers
                .Include(cm => cm.Student)
                .Include(cm => cm.Department)
                .Where(cm => cm.ClubId == clubId)
                .OrderBy(cm => cm.Student.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClubMember>> GetByStudentIdAsync(int studentId)
        {
            return await _context.ClubMembers
                .Include(cm => cm.Club)
                .Include(cm => cm.Department)
                .Where(cm => cm.StudentId == studentId)
                .OrderBy(cm => cm.Club.Name)
                .ToListAsync();
        }

        public async Task<ClubMember> CreateAsync(ClubMember clubMember)
        {
            _context.ClubMembers.Add(clubMember);
            await _context.SaveChangesAsync();
            
            return (await GetByIdAsync(clubMember.Id))!;
        }

        public async Task<ClubMember> UpdateAsync(ClubMember clubMember)
        {
            _context.ClubMembers.Update(clubMember);
            await _context.SaveChangesAsync();
            
            return (await GetByIdAsync(clubMember.Id))!;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var clubMember = await _context.ClubMembers.FindAsync(id);
            if (clubMember == null) return false;

            _context.ClubMembers.Remove(clubMember);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.ClubMembers.AnyAsync(cm => cm.Id == id);
        }
    }
}

