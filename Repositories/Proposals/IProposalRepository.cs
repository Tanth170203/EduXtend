using BusinessObject.Models;

namespace Repositories.Proposals;

public interface IProposalRepository
{
    Task<Proposal?> GetByIdAsync(int id);
    Task<Proposal?> GetByIdWithDetailsAsync(int id);
    Task<List<Proposal>> GetByClubIdAsync(int clubId);
    Task<List<Proposal>> GetByCreatorIdAsync(int userId);
    Task<Proposal> AddAsync(Proposal proposal);
    Task<Proposal> UpdateAsync(Proposal proposal);
    Task<bool> DeleteAsync(int id);
    Task<bool> IsCreatorAsync(int proposalId, int userId);
    Task<bool> IsClubMemberAsync(int clubId, int studentId);
    Task<string?> GetClubRoleAsync(int clubId, int studentId);
}

