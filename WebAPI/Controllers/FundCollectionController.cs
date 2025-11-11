using BusinessObject.DTOs.FundCollection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.FundCollections;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FundCollectionController : ControllerBase
    {
        private readonly IFundCollectionService _fundCollectionService;
        private readonly ILogger<FundCollectionController> _logger;

        public FundCollectionController(
            IFundCollectionService fundCollectionService,
            ILogger<FundCollectionController> logger)
        {
            _fundCollectionService = fundCollectionService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        // GET: api/FundCollection/club/{clubId}
        [HttpGet("club/{clubId}")]
        public async Task<IActionResult> GetClubRequests(int clubId, [FromQuery] bool activeOnly = false)
        {
            try
            {
                var requests = activeOnly
                    ? await _fundCollectionService.GetActiveRequestsByClubIdAsync(clubId)
                    : await _fundCollectionService.GetRequestsByClubIdAsync(clubId);

                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fund collection requests for club {ClubId}", clubId);
                return StatusCode(500, new { message = "An error occurred while retrieving requests" });
            }
        }

        // GET: api/FundCollection/{requestId}
        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetRequest(int requestId)
        {
            try
            {
                var request = await _fundCollectionService.GetRequestByIdAsync(requestId);
                return Ok(request);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fund collection request {RequestId}", requestId);
                return StatusCode(500, new { message = "An error occurred while retrieving the request" });
            }
        }

        // POST: api/FundCollection/club/{clubId}
        [HttpPost("club/{clubId}")]
        [Authorize]
        public async Task<IActionResult> CreateRequest(int clubId, [FromBody] CreateFundCollectionRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var request = await _fundCollectionService.CreateRequestAsync(clubId, dto, userId);
                return CreatedAtAction(nameof(GetRequest), new { requestId = request.Id }, request);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fund collection request for club {ClubId}", clubId);
                return StatusCode(500, new { message = "An error occurred while creating the request" });
            }
        }

        // PUT: api/FundCollection/{requestId}
        [HttpPut("{requestId}")]
        [Authorize]
        public async Task<IActionResult> UpdateRequest(int requestId, [FromBody] UpdateFundCollectionRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var request = await _fundCollectionService.UpdateRequestAsync(requestId, dto, userId);
                return Ok(request);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fund collection request {RequestId}", requestId);
                return StatusCode(500, new { message = "An error occurred while updating the request" });
            }
        }

        // POST: api/FundCollection/{requestId}/cancel
        [HttpPost("{requestId}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelRequest(int requestId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _fundCollectionService.CancelRequestAsync(requestId, userId);
                return Ok(new { message = "Request cancelled successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling fund collection request {RequestId}", requestId);
                return StatusCode(500, new { message = "An error occurred while cancelling the request" });
            }
        }

        // POST: api/FundCollection/{requestId}/complete
        [HttpPost("{requestId}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteRequest(int requestId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _fundCollectionService.CompleteRequestAsync(requestId, userId);
                return Ok(new { message = "Request completed successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing fund collection request {RequestId}", requestId);
                return StatusCode(500, new { message = "An error occurred while completing the request" });
            }
        }

        // GET: api/FundCollection/{requestId}/payments
        [HttpGet("{requestId}/payments")]
        public async Task<IActionResult> GetPayments(int requestId)
        {
            try
            {
                var payments = await _fundCollectionService.GetPaymentsByRequestIdAsync(requestId);
                return Ok(payments);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments for request {RequestId}", requestId);
                return StatusCode(500, new { message = "An error occurred while retrieving payments" });
            }
        }

        // POST: api/FundCollection/payment/{paymentId}/confirm
        [HttpPost("payment/{paymentId}/confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmPayment(int paymentId, [FromBody] ConfirmPaymentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var payment = await _fundCollectionService.ConfirmPaymentAsync(paymentId, dto, userId);
                return Ok(payment);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment {PaymentId}", paymentId);
                return StatusCode(500, new { message = "An error occurred while confirming the payment" });
            }
        }

        // POST: api/FundCollection/payment/{paymentId}/reject
        [HttpPost("payment/{paymentId}/reject")]
        [Authorize]
        public async Task<IActionResult> RejectPayment(int paymentId, [FromBody] string reason)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var payment = await _fundCollectionService.RejectPaymentAsync(paymentId, reason, userId);
                return Ok(payment);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payment {PaymentId}", paymentId);
                return StatusCode(500, new { message = "An error occurred while rejecting the payment" });
            }
        }

        // POST: api/FundCollection/club/{clubId}/send-reminder
        [HttpPost("club/{clubId}/send-reminder")]
        [Authorize]
        public async Task<IActionResult> SendReminder(int clubId, [FromBody] SendReminderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _fundCollectionService.SendReminderAsync(dto, clubId);
                return Ok(new { message = "Reminders sent successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminders for club {ClubId}", clubId);
                return StatusCode(500, new { message = "An error occurred while sending reminders" });
            }
        }

        // GET: api/FundCollection/club/{clubId}/statistics
        [HttpGet("club/{clubId}/statistics")]
        public async Task<IActionResult> GetStatistics(int clubId)
        {
            try
            {
                var statistics = await _fundCollectionService.GetClubStatisticsAsync(clubId);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for club {ClubId}", clubId);
                return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
            }
        }

        // GET: api/FundCollection/club/{clubId}/member-summaries
        [HttpGet("club/{clubId}/member-summaries")]
        public async Task<IActionResult> GetMemberSummaries(int clubId)
        {
            try
            {
                var summaries = await _fundCollectionService.GetMemberPaymentSummariesAsync(clubId);
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member summaries for club {ClubId}", clubId);
                return StatusCode(500, new { message = "An error occurred while retrieving member summaries" });
            }
        }

        // GET: api/FundCollection/club/{clubId}/my-payments
        [HttpGet("club/{clubId}/my-payments")]
        [Authorize]
        public async Task<IActionResult> GetMyPayments(int clubId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var payments = await _fundCollectionService.GetMemberPaymentsAsync(clubId, userId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments for user {UserId} in club {ClubId}", GetCurrentUserId(), clubId);
                return StatusCode(500, new { message = "An error occurred while retrieving your payments" });
            }
        }

        // POST: api/FundCollection/payment/{paymentId}/member-pay
        [HttpPost("payment/{paymentId}/member-pay")]
        [Authorize]
        public async Task<IActionResult> MemberPay(int paymentId, [FromBody] MemberPayDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var payment = await _fundCollectionService.MemberSubmitPaymentAsync(paymentId, dto, userId);
                return Ok(payment);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized member payment attempt by user {UserId} for payment {PaymentId}", GetCurrentUserId(), paymentId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid member payment request by user {UserId} for payment {PaymentId}", GetCurrentUserId(), paymentId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing member payment for payment {PaymentId} by user {UserId}", paymentId, GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while processing your payment" });
            }
        }
    }
}

