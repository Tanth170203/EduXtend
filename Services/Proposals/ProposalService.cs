using BusinessObject.DTOs.Proposal;
using BusinessObject.Models;
using Repositories.Proposals;
using Repositories.Students;

namespace Services.Proposals;

public class ProposalService : IProposalService
{
    private readonly IProposalRepository _proposalRepository;
    private readonly IProposalVoteRepository _voteRepository;
    private readonly IStudentRepository _studentRepository;

    public ProposalService(
        IProposalRepository proposalRepository,
        IProposalVoteRepository voteRepository,
        IStudentRepository studentRepository)
    {
        _proposalRepository = proposalRepository;
        _voteRepository = voteRepository;
        _studentRepository = studentRepository;
    }

    public async Task<ProposalDTO?> GetProposalByIdAsync(int id, int currentUserId)
    {
            var proposal = await _proposalRepository.GetByIdAsync(id);
            if (proposal == null) return null;

            return MapToProposalDTO(proposal, currentUserId);
    }

    public async Task<ProposalDetailDTO?> GetProposalDetailByIdAsync(int id, int currentUserId)
    {
        var proposal = await _proposalRepository.GetByIdWithDetailsAsync(id);
        if (proposal == null) return null;

        var votes = proposal.Votes.Select(v => new VoteInfoDTO
        {
            Id = v.Id,
            UserId = v.UserId,
            UserName = v.User.FullName,
            IsAgree = v.IsAgree,
            CreatedAt = v.CreatedAt
        }).ToList();

        var currentUserVote = proposal.Votes.FirstOrDefault(v => v.UserId == currentUserId);

        return new ProposalDetailDTO
        {
            Id = proposal.Id,
            ClubId = proposal.ClubId,
            ClubName = proposal.Club.Name,
            CreatedById = proposal.CreatedById,
            CreatedByName = proposal.CreatedBy.FullName,
            Title = proposal.Title,
            Description = proposal.Description,
            Status = proposal.Status,
            CreatedAt = proposal.CreatedAt,
            ClosedAt = proposal.ClosedAt,
            AgreeCount = proposal.Votes.Count(v => v.IsAgree),
            DisagreeCount = proposal.Votes.Count(v => !v.IsAgree),
            CurrentUserVote = currentUserVote?.IsAgree,
            Votes = votes
        };
    }

    public async Task<List<ProposalDTO>> GetProposalsByClubIdAsync(int clubId, int currentUserId)
    {
        var proposals = await _proposalRepository.GetByClubIdAsync(clubId);
        var result = new List<ProposalDTO>();

            foreach (var proposal in proposals)
            {
                result.Add(MapToProposalDTO(proposal, currentUserId));
            }

        return result;
    }

    public async Task<List<ProposalDTO>> GetMyProposalsAsync(int userId)
    {
        var proposals = await _proposalRepository.GetByCreatorIdAsync(userId);
        var result = new List<ProposalDTO>();

            foreach (var proposal in proposals)
            {
                result.Add(MapToProposalDTO(proposal, userId));
            }

        return result;
    }

    public async Task<ProposalDTO> CreateProposalAsync(CreateProposalDTO dto, int creatorId)
    {
        // Check if user is a member of the club
        var student = await _studentRepository.GetByUserIdAsync(creatorId);
        if (student == null)
            throw new UnauthorizedAccessException("Only students can create proposals");

        var isMember = await _proposalRepository.IsClubMemberAsync(dto.ClubId, student.Id);
        if (!isMember)
            throw new UnauthorizedAccessException("You must be a member of this club to create proposals");

        var proposal = new Proposal
        {
            ClubId = dto.ClubId,
            CreatedById = creatorId,
            Title = dto.Title,
            Description = dto.Description,
            Status = "PendingVote",
            CreatedAt = DateTime.UtcNow
        };

        var created = await _proposalRepository.AddAsync(proposal);
        var result = await _proposalRepository.GetByIdAsync(created.Id);
        return MapToProposalDTO(result!, creatorId);
    }

    public async Task<ProposalDTO> UpdateProposalAsync(int id, UpdateProposalDTO dto, int userId)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id);
        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found");

        // Check if user is the creator
        if (proposal.CreatedById != userId)
            throw new UnauthorizedAccessException("You can only update your own proposals");

        // Can only update if status is PendingVote
        if (proposal.Status != "PendingVote")
            throw new InvalidOperationException("Can only update proposals that are pending vote");

        proposal.Title = dto.Title;
        proposal.Description = dto.Description;

        await _proposalRepository.UpdateAsync(proposal);
        var updated = await _proposalRepository.GetByIdAsync(id);
        return MapToProposalDTO(updated!, userId);
    }

    public async Task<bool> DeleteProposalAsync(int id, int userId)
    {
        var proposal = await _proposalRepository.GetByIdAsync(id);
        if (proposal == null)
            return false;

        // Check if user is the creator or a manager
        var student = await _studentRepository.GetByUserIdAsync(userId);
        if (student == null)
            throw new UnauthorizedAccessException("Invalid user");

        var isCreator = proposal.CreatedById == userId;
        var clubRole = await _proposalRepository.GetClubRoleAsync(proposal.ClubId, student.Id);
        var isManager = clubRole == "President" || clubRole == "VicePresident" || clubRole == "Manager";

        if (!isCreator && !isManager)
            throw new UnauthorizedAccessException("You can only delete your own proposals or you must be a club manager");

        return await _proposalRepository.DeleteAsync(id);
    }

    public async Task<ProposalDTO> VoteProposalAsync(int proposalId, bool isAgree, int userId)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId);
        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found");

        // Check if proposal is still open for voting
        if (proposal.Status != "PendingVote")
            throw new InvalidOperationException("This proposal is no longer open for voting");

        // Check if user is a member of the club
        var student = await _studentRepository.GetByUserIdAsync(userId);
        if (student == null)
            throw new UnauthorizedAccessException("Only students can vote on proposals");

        var isMember = await _proposalRepository.IsClubMemberAsync(proposal.ClubId, student.Id);
        if (!isMember)
            throw new UnauthorizedAccessException("You must be a member of this club to vote");

        // Check if user has already voted
        var existingVote = await _voteRepository.GetVoteAsync(proposalId, userId);
        
        if (existingVote != null)
        {
            // Update existing vote
            existingVote.IsAgree = isAgree;
            await _voteRepository.UpdateVoteAsync(existingVote);
        }
        else
        {
            // Create new vote
            var vote = new ProposalVote
            {
                ProposalId = proposalId,
                UserId = userId,
                IsAgree = isAgree,
                CreatedAt = DateTime.UtcNow
            };
            await _voteRepository.AddVoteAsync(vote);
        }

        var updated = await _proposalRepository.GetByIdAsync(proposalId);
        return MapToProposalDTO(updated!, userId);
    }

    public async Task<bool> RemoveVoteAsync(int proposalId, int userId)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId);
        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found");

        if (proposal.Status != "PendingVote")
            throw new InvalidOperationException("Cannot remove vote from closed proposal");

        return await _voteRepository.DeleteVoteAsync(proposalId, userId);
    }

    public async Task<ProposalDTO> CloseProposalAsync(int proposalId, int userId)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId);
        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found");

        // Check if user is the creator or a manager
        var student = await _studentRepository.GetByUserIdAsync(userId);
        if (student == null)
            throw new UnauthorizedAccessException("Invalid user");

        var isCreator = proposal.CreatedById == userId;
        var clubRole = await _proposalRepository.GetClubRoleAsync(proposal.ClubId, student.Id);
        var isManager = clubRole == "President" || clubRole == "VicePresident" || clubRole == "Manager";

        if (!isCreator && !isManager)
            throw new UnauthorizedAccessException("Only the creator or club managers can close proposals");

        if (proposal.Status != "PendingVote")
            throw new InvalidOperationException("Proposal is already closed");

        // Count votes to determine status
        var votes = await _voteRepository.GetVotesByProposalIdAsync(proposalId);
        var agreeCount = votes.Count(v => v.IsAgree);
        var disagreeCount = votes.Count(v => !v.IsAgree);

        proposal.Status = agreeCount > disagreeCount ? "ApprovedByClub" : "Rejected";
        proposal.ClosedAt = DateTime.UtcNow;

        await _proposalRepository.UpdateAsync(proposal);
        var updated = await _proposalRepository.GetByIdAsync(proposalId);
        return MapToProposalDTO(updated!, userId);
    }

    public async Task<bool> IsUserClubManagerAsync(int userId, int clubId)
    {
        var student = await _studentRepository.GetByUserIdAsync(userId);
        if (student == null)
            return false;

        var clubRole = await _proposalRepository.GetClubRoleAsync(clubId, student.Id);
        return clubRole == "President" || clubRole == "VicePresident" || clubRole == "Manager";
    }

    private ProposalDTO MapToProposalDTO(Proposal proposal, int currentUserId)
    {
        var currentUserVote = proposal.Votes.FirstOrDefault(v => v.UserId == currentUserId);

        return new ProposalDTO
        {
            Id = proposal.Id,
            ClubId = proposal.ClubId,
            ClubName = proposal.Club.Name,
            CreatedById = proposal.CreatedById,
            CreatedByName = proposal.CreatedBy.FullName,
            Title = proposal.Title,
            Description = proposal.Description,
            Status = proposal.Status,
            CreatedAt = proposal.CreatedAt,
            ClosedAt = proposal.ClosedAt,
            AgreeCount = proposal.Votes.Count(v => v.IsAgree),
            DisagreeCount = proposal.Votes.Count(v => !v.IsAgree),
            CurrentUserVote = currentUserVote?.IsAgree
        };
    }
}

