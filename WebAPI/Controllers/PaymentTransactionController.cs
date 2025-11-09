using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessObject.DTOs.PaymentTransaction;
using BusinessObject.Models;
using Repositories.PaymentTransactions;
using Repositories.Clubs;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentTransactionController : ControllerBase
{
    private readonly IPaymentTransactionRepository _transactionRepo;
    private readonly IClubRepository _clubRepo;
    private readonly EduXtendContext _context;
    private readonly ILogger<PaymentTransactionController> _logger;

    public PaymentTransactionController(
        IPaymentTransactionRepository transactionRepo,
        IClubRepository clubRepo,
        EduXtendContext context,
        ILogger<PaymentTransactionController> logger)
    {
        _transactionRepo = transactionRepo;
        _clubRepo = clubRepo;
        _context = context;
        _logger = logger;
    }

    // GET: api/PaymentTransaction/club/{clubId}
    [HttpGet("club/{clubId}")]
    public async Task<IActionResult> GetByClubId(int clubId)
    {
        try
        {
            var transactions = await _transactionRepo.GetByClubIdAsync(clubId);
            var dtos = transactions.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for club {ClubId}", clubId);
            return StatusCode(500, new { message = "An error occurred while fetching transactions", error = ex.Message });
        }
    }

    // GET: api/PaymentTransaction/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var transaction = await _transactionRepo.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound(new { message = "Transaction not found" });
            }

            return Ok(MapToDto(transaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction {Id}", id);
            return StatusCode(500, new { message = "An error occurred while fetching transaction", error = ex.Message });
        }
    }

    // POST: api/PaymentTransaction/club/{clubId}
    [HttpPost("club/{clubId}")]
    public async Task<IActionResult> Create(int clubId, [FromBody] CreatePaymentTransactionDto dto)
    {
        try
        {
            // Validate club exists
            var club = await _clubRepo.GetByIdAsync(clubId);
            if (club == null)
            {
                return NotFound(new { message = "Club not found" });
            }

            // Get userId from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Get or use active semester
            int? semesterId = dto.SemesterId;
            if (!semesterId.HasValue)
            {
                var activeSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
                semesterId = activeSemester?.Id;
            }

            var transaction = new PaymentTransaction
            {
                ClubId = clubId,
                Title = dto.Title,
                Type = dto.Type,
                Category = dto.Category,
                Amount = dto.Amount,
                Status = "completed",  // Default to completed
                Description = dto.Description,
                Notes = dto.Notes,
                Method = dto.Method,
                ReceiptUrl = dto.ReceiptUrl,
                StudentId = dto.StudentId,
                ActivityId = dto.ActivityId,
                SemesterId = semesterId,
                TransactionDate = dto.TransactionDate ?? DateTime.UtcNow,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _transactionRepo.CreateAsync(transaction);
            
            // Reload with relations
            var result = await _transactionRepo.GetByIdAsync(created.Id);
            return Ok(MapToDto(result!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction for club {ClubId}", clubId);
            return StatusCode(500, new { message = "An error occurred while creating transaction", error = ex.Message });
        }
    }

    // PUT: api/PaymentTransaction/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentTransactionDto dto)
    {
        try
        {
            var transaction = await _transactionRepo.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound(new { message = "Transaction not found" });
            }

            // Update fields
            transaction.Title = dto.Title;
            transaction.Type = dto.Type;
            transaction.Category = dto.Category;
            transaction.Amount = dto.Amount;
            transaction.Status = dto.Status;
            transaction.Description = dto.Description;
            transaction.Notes = dto.Notes;
            transaction.Method = dto.Method;
            transaction.ReceiptUrl = dto.ReceiptUrl;
            transaction.TransactionDate = dto.TransactionDate;

            var updated = await _transactionRepo.UpdateAsync(transaction);
            var result = await _transactionRepo.GetByIdAsync(updated.Id);
            return Ok(MapToDto(result!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating transaction", error = ex.Message });
        }
    }

    // DELETE: api/PaymentTransaction/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _transactionRepo.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Transaction not found" });
            }

            return Ok(new { message = "Transaction deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting transaction", error = ex.Message });
        }
    }

    private PaymentTransactionDto MapToDto(PaymentTransaction transaction)
    {
        return new PaymentTransactionDto
        {
            Id = transaction.Id,
            ClubId = transaction.ClubId,
            ClubName = transaction.Club?.Name ?? "",
            Title = transaction.Title,
            Type = transaction.Type,
            Category = transaction.Category,
            Amount = transaction.Amount,
            Status = transaction.Status,
            Description = transaction.Description,
            Notes = transaction.Notes,
            Method = transaction.Method,
            ReceiptUrl = transaction.ReceiptUrl,
            StudentId = transaction.StudentId,
            StudentName = transaction.Student?.FullName,
            StudentCode = transaction.Student?.StudentCode,
            ActivityId = transaction.ActivityId,
            ActivityTitle = transaction.Activity?.Title,
            SemesterId = transaction.SemesterId,
            SemesterName = transaction.Semester?.Name,
            CreatedById = transaction.CreatedById,
            CreatedByName = transaction.CreatedBy?.FullName,
            TransactionDate = transaction.TransactionDate,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}

