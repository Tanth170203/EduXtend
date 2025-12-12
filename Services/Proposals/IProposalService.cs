using BusinessObject.DTOs.Proposal;

namespace Services.Proposals;

public interface IProposalService
{
    Task<ProposalDTO?> GetProposalByIdAsync(int id, int currentUserId);
    Task<ProposalDetailDTO?> GetProposalDetailByIdAsync(int id, int currentUserId);
    Task<List<ProposalDTO>> GetProposalsByClubIdAsync(int clubId, int currentUserId);
    Task<List<ProposalDTO>> GetMyProposalsAsync(int userId);
    Task<ProposalDTO> CreateProposalAsync(CreateProposalDTO dto, int creatorId);
    Task<ProposalDTO> UpdateProposalAsync(int id, UpdateProposalDTO dto, int userId);
    Task<bool> DeleteProposalAsync(int id, int userId);
    Task<ProposalDTO> VoteProposalAsync(int proposalId, bool isAgree, int userId);
    Task<bool> RemoveVoteAsync(int proposalId, int userId);
    Task<ProposalDTO> CloseProposalAsync(int proposalId, int userId);
    Task<bool> IsUserClubManagerAsync(int userId, int clubId);
}

