using BusinessObject.DTOs.Proposal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Proposals;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProposalController : ControllerBase
{
    private readonly IProposalService _proposalService;

    public ProposalController(IProposalService proposalService)
    {
        _proposalService = proposalService;
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
}

