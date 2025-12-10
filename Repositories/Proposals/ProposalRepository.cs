using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Proposals;

public class ProposalRepository : IProposalRepository
{
    private readonly EduXtendContext _context;

    public ProposalRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<Proposal?> GetByIdAsync(int id)
    {
        return await _context.Proposals
            .Include(p => p.Club)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Proposal?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Proposals
            .Include(p => p.Club)
            .Include(p => p.CreatedBy)
            .Include(p => p.Votes)
                .ThenInclude(v => v.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Proposal>> GetByClubIdAsync(int clubId)
    {
        return await _context.Proposals
            .Include(p => p.Club)
            .Include(p => p.CreatedBy)
            .Include(p => p.Votes)
            .Where(p => p.ClubId == clubId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Proposal>> GetByCreatorIdAsync(int userId)
    {
        return await _context.Proposals
            .Include(p => p.Club)
            .Include(p => p.CreatedBy)
            .Include(p => p.Votes)
            .Where(p => p.CreatedById == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Proposal> AddAsync(Proposal proposal)
    {
        _context.Proposals.Add(proposal);
        await _context.SaveChangesAsync();
        return proposal;
    }

    public async Task<Proposal> UpdateAsync(Proposal proposal)
    {
        _context.Proposals.Update(proposal);
        await _context.SaveChangesAsync();
        return proposal;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var proposal = await _context.Proposals.FindAsync(id);
        if (proposal == null) return false;

        _context.Proposals.Remove(proposal);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsCreatorAsync(int proposalId, int userId)
    {
        return await _context.Proposals
            .AnyAsync(p => p.Id == proposalId && p.CreatedById == userId);
    }

    public async Task<bool> IsClubMemberAsync(int clubId, int studentId)
    {
        return await _context.ClubMembers
            .AnyAsync(cm => cm.ClubId == clubId && cm.StudentId == studentId && cm.IsActive);
    }

    public async Task<string?> GetClubRoleAsync(int clubId, int studentId)
    {
        var member = await _context.ClubMembers
            .FirstOrDefaultAsync(cm => cm.ClubId == clubId && cm.StudentId == studentId && cm.IsActive);
        return member?.RoleInClub;
    }
}

