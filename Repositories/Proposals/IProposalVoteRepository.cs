using BusinessObject.Models;

namespace Repositories.Proposals;

public interface IProposalVoteRepository
{
    Task<ProposalVote?> GetVoteAsync(int proposalId, int userId);
    Task<List<ProposalVote>> GetVotesByProposalIdAsync(int proposalId);
    Task<ProposalVote> AddVoteAsync(ProposalVote vote);
    Task<ProposalVote> UpdateVoteAsync(ProposalVote vote);
    Task<bool> DeleteVoteAsync(int proposalId, int userId);
    Task<bool> HasVotedAsync(int proposalId, int userId);
}

