using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs.Proposal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Activities;
using Services.Proposals;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProposalController : ControllerBase
{
    private readonly IProposalService _proposalService;
    private readonly IActivityExtractorService _extractorService;
    private readonly ILogger<ProposalController> _logger;

    public ProposalController(
        IProposalService proposalService,
        IActivityExtractorService extractorService,
        ILogger<ProposalController> logger)
    {
        _proposalService = proposalService;
        _extractorService = extractorService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    /// <summary>
    /// Get a proposal by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProposalDTO>> GetProposal(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var proposal = await _proposalService.GetProposalByIdAsync(id, userId);
            
            if (proposal == null)
                return NotFound(new { message = "Proposal not found" });

            return Ok(proposal);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get proposal detail with votes
    /// </summary>
    [HttpGet("{id}/detail")]
    public async Task<ActionResult<ProposalDetailDTO>> GetProposalDetail(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var proposal = await _proposalService.GetProposalDetailByIdAsync(id, userId);
            
            if (proposal == null)
                return NotFound(new { message = "Proposal not found" });

            return Ok(proposal);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all proposals for a specific club (Club Manager can see all)
    /// </summary>
    [HttpGet("club/{clubId}")]
    public async Task<ActionResult<List<ProposalDTO>>> GetClubProposals(int clubId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var proposals = await _proposalService.GetProposalsByClubIdAsync(clubId, userId);
            return Ok(proposals);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all proposals created by the current user
    /// </summary>
    [HttpGet("my-proposals")]
    public async Task<ActionResult<List<ProposalDTO>>> GetMyProposals()
    {
        try
        {
            var userId = GetCurrentUserId();
            var proposals = await _proposalService.GetMyProposalsAsync(userId);
            return Ok(proposals);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new proposal (Club Members only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProposalDTO>> CreateProposal([FromBody] CreateProposalDTO dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var proposal = await _proposalService.CreateProposalAsync(dto, userId);
            return CreatedAtAction(nameof(GetProposal), new { id = proposal.Id }, proposal);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a proposal (Creator only, only when status is PendingVote)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ProposalDTO>> UpdateProposal(int id, [FromBody] UpdateProposalDTO dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var proposal = await _proposalService.UpdateProposalAsync(id, dto, userId);
            return Ok(proposal);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a proposal (Creator or Club Manager)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProposal(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _proposalService.DeleteProposalAsync(id, userId);
            
            if (!result)
                return NotFound(new { message = "Proposal not found" });

            return Ok(new { message = "Proposal deleted successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Vote on a proposal (Club Members only)
    /// </summary>
    [HttpPost("{id}/vote")]
    public async Task<ActionResult<ProposalDTO>> VoteProposal(int id, [FromBody] ProposalVoteDTO dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var proposal = await _proposalService.VoteProposalAsync(id, dto.IsAgree, userId);
            return Ok(proposal);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove vote from a proposal
    /// </summary>
    [HttpDelete("{id}/vote")]
    public async Task<ActionResult> RemoveVote(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _proposalService.RemoveVoteAsync(id, userId);
            
            if (!result)
                return NotFound(new { message = "Vote not found" });

            return Ok(new { message = "Vote removed successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Close a proposal and determine final status (Creator or Club Manager)
    /// </summary>
    [HttpPost("{id}/close")]
    public async Task<ActionResult<ProposalDTO>> CloseProposal(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var proposal = await _proposalService.CloseProposalAsync(id, userId);
            return Ok(proposal);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Extract activity data from an approved proposal using AI (Club Manager only)
    /// </summary>
    [HttpPost("{proposalId:int}/extract-to-activity")]
    [Authorize(Roles = "ClubManager")]
    public async Task<ActionResult<ExtractedActivityDto>> ExtractToActivity(int proposalId)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Get proposal details
            var proposal = await _proposalService.GetProposalDetailByIdAsync(proposalId, userId);
            if (proposal == null)
            {
                return NotFound(new { message = "Proposal not found" });
            }
            
            // Verify user is club manager of this club
            var isManager = await _proposalService.IsUserClubManagerAsync(userId, proposal.ClubId);
            if (!isManager)
            {
                return StatusCode(403, new { message = "You don't have permission to perform this action" });
            }
            
            // Verify proposal status is ApprovedByClub
            if (proposal.Status != "ApprovedByClub")
            {
                return BadRequest(new { message = "Only approved proposals can be converted to activities" });
            }
            
            // Extract activity data using AI
            ExtractedActivityDto extractedData;
            try
            {
                _logger.LogInformation("Extracting activity from proposal {ProposalId} for user {UserId}", proposalId, userId);
                
                extractedData = await _extractorService.ExtractActivityFromProposalAsync(
                    proposal.Title,
                    proposal.Description,
                    proposal.Id,
                    proposal.ClubId
                );
                
                _logger.LogInformation("Successfully extracted activity from proposal {ProposalId}", proposalId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "AI service error during proposal extraction for proposal {ProposalId}", proposalId);
                // Fallback: return minimal data
                extractedData = new ExtractedActivityDto
                {
                    Title = proposal.Title,
                    Description = proposal.Description,
                    ProposalId = proposal.Id,
                    ClubId = proposal.ClubId,
                    RawExtractedText = proposal.Description
                };
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "AI service timeout during proposal extraction for proposal {ProposalId}", proposalId);
                // Fallback: return minimal data
                extractedData = new ExtractedActivityDto
                {
                    Title = proposal.Title,
                    Description = proposal.Description,
                    ProposalId = proposal.Id,
                    ClubId = proposal.ClubId,
                    RawExtractedText = proposal.Description
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during proposal extraction for proposal {ProposalId}", proposalId);
                // Fallback: return minimal data
                extractedData = new ExtractedActivityDto
                {
                    Title = proposal.Title,
                    Description = proposal.Description,
                    ProposalId = proposal.Id,
                    ClubId = proposal.ClubId,
                    RawExtractedText = proposal.Description
                };
            }
            
            return Ok(extractedData);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ExtractToActivity for proposal {ProposalId}", proposalId);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }
}

