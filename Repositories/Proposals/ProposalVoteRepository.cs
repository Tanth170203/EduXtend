using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Proposals;

public class ProposalVoteRepository : IProposalVoteRepository
{
    private readonly EduXtendContext _context;

    public ProposalVoteRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<ProposalVote?> GetVoteAsync(int proposalId, int userId)
    {
        return await _context.ProposalVotes
            .FirstOrDefaultAsync(v => v.ProposalId == proposalId && v.UserId == userId);
    }

    public async Task<List<ProposalVote>> GetVotesByProposalIdAsync(int proposalId)
    {
        return await _context.ProposalVotes
            .Include(v => v.User)
            .Where(v => v.ProposalId == proposalId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProposalVote> AddVoteAsync(ProposalVote vote)
    {
        _context.ProposalVotes.Add(vote);
        await _context.SaveChangesAsync();
        return vote;
    }

    public async Task<ProposalVote> UpdateVoteAsync(ProposalVote vote)
    {
        _context.ProposalVotes.Update(vote);
        await _context.SaveChangesAsync();
        return vote;
    }

    public async Task<bool> DeleteVoteAsync(int proposalId, int userId)
    {
        var vote = await _context.ProposalVotes
            .FirstOrDefaultAsync(v => v.ProposalId == proposalId && v.UserId == userId);
        
        if (vote == null) return false;

        _context.ProposalVotes.Remove(vote);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasVotedAsync(int proposalId, int userId)
    {
        return await _context.ProposalVotes
            .AnyAsync(v => v.ProposalId == proposalId && v.UserId == userId);
    }
}

