using BusinessObject.Models;

namespace Repositories.ClubMembers
{
    public interface IClubMemberRepository
    {
        Task<ClubMember?> GetByIdAsync(int id);
        Task<ClubMember?> GetByClubAndStudentIdAsync(int clubId, int studentId);
        Task<ClubMember?> GetByClubAndUserIdAsync(int clubId, int userId);
        Task<IEnumerable<ClubMember>> GetByClubIdAsync(int clubId);
        Task<IEnumerable<ClubMember>> GetByStudentIdAsync(int studentId);
        Task<ClubMember> CreateAsync(ClubMember clubMember);
        Task<ClubMember> UpdateAsync(ClubMember clubMember);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}

