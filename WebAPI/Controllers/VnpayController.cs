using BusinessObject.DTOs.Vnpay;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Notifications;
using VNPAY;
using VNPAY.Models.Enums;
using VNPAY.Models.Exceptions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VnpayController : ControllerBase
{
    private readonly IVnpayClient _vnpayClient;
    private readonly EduXtendContext _context;
    private readonly ILogger<VnpayController> _logger;
    private readonly IConfiguration _configuration;
    private readonly INotificationService _notificationService;

    public VnpayController(
        IVnpayClient vnpayClient,
        EduXtendContext context,
        ILogger<VnpayController> logger,
        IConfiguration configuration,
        INotificationService notificationService)
    {
        _vnpayClient = vnpayClient;
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Tạo URL thanh toán VNPAY cho payment request
    /// </summary>
    [HttpPost("create-payment-url")]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] CreateVnpayPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Received create payment URL request for PaymentId: {PaymentId}", request.PaymentId);

            // Validate payment exists and is pending
            var payment = await _context.FundCollectionPayments
                .Include(p => p.FundCollectionRequest)
                .Include(p => p.ClubMember)
                    .ThenInclude(cm => cm.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p => p.Id == request.PaymentId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found: {PaymentId}", request.PaymentId);
                return NotFound(new { message = "Payment request not found" });
            }

            if (payment.Status != "pending")
            {
                _logger.LogWarning("Payment already processed: {PaymentId}, Status: {Status}", request.PaymentId, payment.Status);
                return BadRequest(new { message = "Payment has already been processed" });
            }

            // Check if there's already a pending VNPAY transaction for this payment
            var existingTransaction = await _context.VnpayTransactionDetails
                .FirstOrDefaultAsync(v => v.FundCollectionPaymentId == payment.Id && v.TransactionStatus == "pending");

            if (existingTransaction != null)
            {
                _logger.LogInformation("Found existing pending transaction for PaymentId: {PaymentId}, reusing it", payment.Id);
                
                // Reuse existing transaction - just return the payment URL again
                var existingPaymentUrl = _vnpayClient.CreatePaymentUrl(
                    money: (double)payment.Amount,
                    description: RemoveVietnameseDiacritics($"Thanh toan {payment.FundCollectionRequest.Title} - {payment.ClubMember.Student.User.FullName}"),
                    bankCode: BankCode.ANY
                );

                return Ok(new VnpayPaymentResponse
                {
                    PaymentUrl = existingPaymentUrl.Url,
                    TransactionId = existingPaymentUrl.PaymentId
                });
            }

            // Remove Vietnamese diacritics from description (VNPAY requirement)
            var description = RemoveVietnameseDiacritics($"Thanh toan {payment.FundCollectionRequest.Title} - {payment.ClubMember.Student.User.FullName}");
            
            _logger.LogInformation("Creating VNPAY payment URL for amount: {Amount}, description: {Description}", 
                payment.Amount, 
                description);

            // Generate VNPAY payment URL first to get the PaymentId
            var paymentUrlInfo = _vnpayClient.CreatePaymentUrl(
                money: (double)payment.Amount,
                description: description,
                bankCode: BankCode.ANY
            );

            _logger.LogInformation("VNPAY payment URL created successfully. PaymentId from VNPAY: {VnpayPaymentId}", paymentUrlInfo.PaymentId);

            // Create VNPAY transaction detail record with the PaymentId from VNPAY
            var vnpayTransaction = new BusinessObject.Models.VnpayTransactionDetail
            {
                FundCollectionPaymentId = payment.Id,
                VnpayTransactionId = paymentUrlInfo.PaymentId, // Use PaymentId from VNPAY library
                Amount = payment.Amount,
                OrderInfo = $"Thanh toan {payment.FundCollectionRequest.Title}",
                TransactionStatus = "pending",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.VnpayTransactionDetails.Add(vnpayTransaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created VNPAY payment URL for PaymentId: {PaymentId}, VnpayTransactionId: {VnpayTransactionId}, URL: {Url}",
                payment.Id,
                vnpayTransaction.VnpayTransactionId,
                paymentUrlInfo.Url
            );

            return Ok(new VnpayPaymentResponse
            {
                PaymentUrl = paymentUrlInfo.Url,
                TransactionId = vnpayTransaction.VnpayTransactionId
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating VNPAY payment URL for PaymentId: {PaymentId}", request.PaymentId);
            return BadRequest(new { message = "Invalid payment data", error = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error when creating VNPAY payment URL for PaymentId: {PaymentId}", request.PaymentId);
            return StatusCode(500, new { message = "Database error occurred. Please try again later." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating VNPAY payment URL for PaymentId: {PaymentId}", request.PaymentId);
            return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
        }
    }

    /// <summary>
    /// IPN endpoint - Nhận thông báo từ VNPAY khi thanh toán hoàn tất
    /// </summary>
    [HttpGet("ipn")]
    public async Task<IActionResult> ProcessIpn()
    {
        try
        {
            // Get payment result from VNPAY
            // If signature is invalid or payment failed, this will throw VnpayException
            var paymentResult = _vnpayClient.GetPaymentResult(Request);

            _logger.LogInformation(
                "Received IPN from VNPAY - TxnRef: {TxnRef}, VnpayTxnId: {VnpayTxnId}",
                paymentResult.PaymentId,
                paymentResult.VnpayTransactionId
            );

            // Find transaction by VnpayTransactionId (our generated ID)
            var transaction = await _context.VnpayTransactionDetails
                .Include(v => v.FundCollectionPayment)
                    .ThenInclude(p => p.FundCollectionRequest)
                        .ThenInclude(fcr => fcr.Club)
                            .ThenInclude(c => c.Members.Where(cm => cm.RoleInClub == "Manager"))
                                .ThenInclude(cm => cm.Student)
                                    .ThenInclude(s => s.User)
                .Include(v => v.FundCollectionPayment)
                    .ThenInclude(p => p.ClubMember)
                        .ThenInclude(cm => cm.Student)
                            .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(v => v.VnpayTransactionId == paymentResult.PaymentId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for VnpayTransactionId: {TransactionId}", paymentResult.PaymentId);
                return Ok(new { RspCode = "01", Message = "Order not found" });
            }

            // If we reach here, payment is successful (no exception thrown)
            transaction.ResponseCode = "00"; // Success code
            transaction.BankCode = paymentResult.BankingInfor?.BankCode;
            transaction.BankTransactionId = paymentResult.BankingInfor?.BankTransactionId;
            transaction.TransactionDate = paymentResult.Timestamp;
            transaction.TransactionStatus = "success";
            transaction.UpdatedAt = DateTime.UtcNow;

            // Update payment status to unconfirmed (waiting for club manager confirmation)
            var payment = transaction.FundCollectionPayment;
            payment.Status = "unconfirmed";
            payment.PaymentMethod = "VNPAY";
            payment.PaidAt = paymentResult.Timestamp;
            
            // Set the foreign key - transaction.Id should already have value since it was loaded from DB
            if (transaction.Id > 0)
            {
                payment.VnpayTransactionDetailId = transaction.Id;
            }
            
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Payment successful - PaymentId: {PaymentId}, Amount: {Amount}, Status updated to unconfirmed",
                payment.Id,
                payment.Amount
            );

            // Send notification to member about successful payment
            try
            {
                await _notificationService.NotifyMemberAboutSuccessfulVnpayPaymentAsync(
                    payment.ClubMember.Student.UserId,
                    payment.FundCollectionRequest.Title,
                    payment.Amount,
                    transaction.VnpayTransactionId
                );
                
                _logger.LogInformation(
                    "Notification sent to member UserId: {UserId} for successful payment",
                    payment.ClubMember.Student.UserId
                );
            }
            catch (Exception notifEx)
            {
                _logger.LogError(notifEx, "Failed to send notification to member for PaymentId: {PaymentId}", payment.Id);
                // Don't fail the payment if notification fails
            }

            // Send notification to club manager about new payment
            try
            {
                var clubManager = payment.FundCollectionRequest.Club.Members
                    .FirstOrDefault(cm => cm.RoleInClub == "Manager");
                
                if (clubManager != null)
                {
                    await _notificationService.NotifyClubManagerAboutVnpayPaymentAsync(
                        clubManager.Student.UserId,
                        payment.ClubMember.Student.User.FullName,
                        payment.FundCollectionRequest.Title,
                        payment.Amount,
                        transaction.VnpayTransactionId
                    );
                    
                    _logger.LogInformation(
                        "Notification sent to club manager UserId: {UserId} for new VNPAY payment",
                        clubManager.Student.UserId
                    );
                }
            }
            catch (Exception notifEx)
            {
                _logger.LogError(notifEx, "Failed to send notification to club manager for PaymentId: {PaymentId}", payment.Id);
                // Don't fail the payment if notification fails
            }

            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }
        catch (VnpayException ex)
        {
            // Payment failed or signature invalid
            _logger.LogWarning(ex, "VNPAY payment failed or invalid signature - TransactionStatus: {TransactionStatus}, PaymentResponse: {PaymentResponse}", 
                ex.TransactionStatusCode, ex.PaymentResponseCode);
            
            // Try to update transaction status if we can find it
            try
            {
                var paymentId = long.Parse(Request.Query["vnp_TxnRef"].ToString());
                var transaction = await _context.VnpayTransactionDetails
                    .FirstOrDefaultAsync(v => v.VnpayTransactionId == paymentId);
                
                if (transaction != null)
                {
                    transaction.TransactionStatus = "failed";
                    transaction.ResponseCode = ex.PaymentResponseCode.ToString();
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Updated transaction status to failed for PaymentId: {PaymentId}", paymentId);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update transaction status in error handler");
            }

            return Ok(new { RspCode = "00", Message = "Confirm Success" }); // Still return success to VNPAY
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error processing IPN from VNPAY");
            return Ok(new { RspCode = "99", Message = "Database error" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing IPN from VNPAY");
            return Ok(new { RspCode = "99", Message = "Unknown error" });
        }
    }

    /// <summary>
    /// Callback endpoint - Xử lý khi user được redirect về từ VNPAY
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> PaymentCallback()
    {
        try
        {
            // Get payment result from VNPAY
            var paymentResult = _vnpayClient.GetPaymentResult(Request);

            _logger.LogInformation(
                "Payment callback - TxnRef: {TxnRef}, Success",
                paymentResult.PaymentId
            );

            // Find and update transaction (same logic as IPN)
            var transaction = await _context.VnpayTransactionDetails
                .Include(v => v.FundCollectionPayment)
                    .ThenInclude(p => p.FundCollectionRequest)
                        .ThenInclude(fcr => fcr.Club)
                .Include(v => v.FundCollectionPayment)
                    .ThenInclude(p => p.ClubMember)
                        .ThenInclude(cm => cm.Student)
                .FirstOrDefaultAsync(v => v.VnpayTransactionId == paymentResult.PaymentId);

            if (transaction != null && transaction.TransactionStatus == "pending")
            {
                // Update transaction with all available info
                transaction.ResponseCode = "00";
                transaction.BankCode = paymentResult.BankingInfor?.BankCode ?? "VNPAY";
                transaction.BankTransactionId = paymentResult.BankingInfor?.BankTransactionId ?? paymentResult.VnpayTransactionId.ToString();
                transaction.TransactionDate = paymentResult.Timestamp;
                transaction.TransactionStatus = "success";
                transaction.SecureHash = Request.Query["vnp_SecureHash"].ToString();
                transaction.UpdatedAt = DateTime.UtcNow;

                // Update payment
                var payment = transaction.FundCollectionPayment;
                payment.Status = "unconfirmed";
                payment.PaymentMethod = "VNPAY";
                payment.PaidAt = paymentResult.Timestamp;
                payment.VnpayTransactionDetailId = transaction.Id;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Payment updated via callback - PaymentId: {PaymentId}, Status: unconfirmed, BankCode: {BankCode}",
                    payment.Id,
                    transaction.BankCode
                );
            }

            // Return success result
            return Ok(new
            {
                success = true,
                transactionId = paymentResult.PaymentId,
                vnpayTransactionId = paymentResult.VnpayTransactionId,
                description = paymentResult.Description,
                bankCode = paymentResult.BankingInfor?.BankCode,
                cardType = paymentResult.CardType,
                transactionDate = paymentResult.Timestamp
            });
        }
        catch (VnpayException ex)
        {
            _logger.LogError(ex, "VNPAY error in callback - TransactionStatus: {TransactionStatus}, PaymentResponse: {PaymentResponse}", 
                ex.TransactionStatusCode, ex.PaymentResponseCode);
            
            return Ok(new 
            { 
                success = false, 
                message = GetVnpayErrorMessage(ex.PaymentResponseCode),
                transactionStatusCode = ex.TransactionStatusCode.ToString(),
                paymentResponseCode = ex.PaymentResponseCode.ToString()
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error processing payment callback");
            return StatusCode(500, new { success = false, message = "Database error occurred. Please contact support." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment callback");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred. Please try again later." });
        }
    }

    /// <summary>
    /// Remove Vietnamese diacritics from string (VNPAY requirement)
    /// </summary>
    private static string RemoveVietnameseDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString()
            .Normalize(System.Text.NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }

    /// <summary>
    /// Get user-friendly error message from VNPAY response code
    /// </summary>
    private static string GetVnpayErrorMessage(VNPAY.Models.Enums.PaymentResponseCode responseCode)
    {
        var code = (int)responseCode;
        return code switch
        {
            07 => "Transaction is suspicious. Please contact your bank.",
            09 => "Card not registered for Internet Banking. Please register first.",
            10 => "Incorrect card authentication information. Please check and try again.",
            11 => "Payment timeout. Please try again.",
            12 => "Card is locked. Please contact your bank.",
            13 => "Incorrect OTP. Please try again.",
            24 => "Transaction cancelled by user.",
            51 => "Insufficient account balance.",
            65 => "Daily transaction limit exceeded.",
            75 => "Bank is under maintenance. Please try again later.",
            79 => "Payment timeout. Please try again.",
            _ => "Payment failed. Please try again or contact support."
        };
    }
}
