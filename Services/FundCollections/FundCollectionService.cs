using BusinessObject.DTOs.FundCollection;
using BusinessObject.Models;
using Repositories.FundCollectionRequests;
using Repositories.FundCollectionPayments;
using Repositories.ClubMembers;
using Repositories.Clubs;
using Microsoft.EntityFrameworkCore;
using DataAccess;

namespace Services.FundCollections
{
    public class FundCollectionService : IFundCollectionService
    {
        private readonly IFundCollectionRequestRepository _requestRepo;
        private readonly IFundCollectionPaymentRepository _paymentRepo;
        private readonly IClubMemberRepository _clubMemberRepo;
        private readonly IClubRepository _clubRepo;
        private readonly EduXtendContext _context;
        private readonly Services.Notifications.INotificationService _notificationService;
        private readonly Services.Emails.IEmailService _emailService;

        public FundCollectionService(
            IFundCollectionRequestRepository requestRepo,
            IFundCollectionPaymentRepository paymentRepo,
            IClubMemberRepository clubMemberRepo,
            IClubRepository clubRepo,
            EduXtendContext context,
            Services.Notifications.INotificationService notificationService,
            Services.Emails.IEmailService emailService)
        {
            _requestRepo = requestRepo;
            _paymentRepo = paymentRepo;
            _clubMemberRepo = clubMemberRepo;
            _clubRepo = clubRepo;
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<FundCollectionRequestDto> CreateRequestAsync(int clubId, CreateFundCollectionRequestDto dto, int createdById)
        {
            // Validate club exists
            var club = await _clubRepo.GetByIdAsync(clubId);
            if (club == null)
            {
                throw new ArgumentException("Club not found");
            }

            // Validate user is club manager
            await ValidateClubManagerPermissionAsync(clubId, createdById);

            // Validate due date
            if (dto.DueDate <= DateTime.UtcNow)
            {
                throw new ArgumentException("Due date must be in the future");
            }

            // Get SemesterId - use provided or default to active semester
            int semesterId;
            if (dto.SemesterId.HasValue)
            {
                // Validate semester exists
                var semester = await _context.Semesters.FindAsync(dto.SemesterId.Value);
                if (semester == null)
                {
                    throw new ArgumentException("Specified semester not found");
                }
                semesterId = dto.SemesterId.Value;
            }
            else
            {
                // Use active semester as default
                var activeSemester = await _context.Semesters
                    .FirstOrDefaultAsync(s => s.IsActive);
                
                if (activeSemester == null)
                {
                    throw new InvalidOperationException("No active semester found. Please set an active semester first.");
                }
                
                semesterId = activeSemester.Id;
            }

            // Get all club members
            var members = await _clubMemberRepo.GetByClubIdAsync(clubId);
            var membersList = members.ToList();
            
            if (!membersList.Any())
            {
                throw new InvalidOperationException("Cannot create fund collection request for a club with no members");
            }

            // Create request
            var request = new FundCollectionRequest
            {
                ClubId = clubId,
                Title = dto.Title,
                Description = dto.Description,
                AmountPerMember = dto.AmountPerMember,
                DueDate = dto.DueDate,
                SemesterId = semesterId,
                Status = "active",
                PaymentMethods = dto.PaymentMethods,
                Notes = dto.Notes,
                CreatedById = createdById,
                CreatedAt = DateTime.UtcNow
            };

            var createdRequest = await _requestRepo.CreateAsync(request);

            // Create payment records for all members
            var payments = membersList.Select(member => new FundCollectionPayment
            {
                FundCollectionRequestId = createdRequest.Id,
                ClubMemberId = member.Id,
                Amount = dto.AmountPerMember,
                Status = "pending",
                ReminderCount = 0,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _paymentRepo.CreateManyAsync(payments);

            // Send notifications to all members (in-app only, no email)
            try
            {
                await _notificationService.NotifyMembersAboutNewFundCollectionAsync(
                    clubId,
                    dto.Title,
                    dto.AmountPerMember,
                    dto.DueDate,
                    createdById
                );
            }
            catch (Exception ex)
            {
                // Log but don't fail the request creation
                Console.WriteLine($"Failed to send notifications: {ex.Message}");
            }

            // Reload with details
            var result = await _requestRepo.GetByIdWithDetailsAsync(createdRequest.Id);
            return MapToDto(result!);
        }

        public async Task<FundCollectionRequestDto> UpdateRequestAsync(int requestId, UpdateFundCollectionRequestDto dto, int userId)
        {
            var request = await _requestRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null)
            {
                throw new ArgumentException("Fund collection request not found");
            }

            // Validate user is club manager
            await ValidateClubManagerPermissionAsync(request.ClubId, userId);

            // Validate status transition
            if (request.Status == "completed" || request.Status == "cancelled")
            {
                throw new InvalidOperationException("Cannot update a completed or cancelled request");
            }

            // Update fields
            request.Title = dto.Title;
            request.Description = dto.Description;
            request.AmountPerMember = dto.AmountPerMember;
            request.DueDate = dto.DueDate;
            request.Status = dto.Status;
            request.PaymentMethods = dto.PaymentMethods;
            request.Notes = dto.Notes;

            // If amount changed, update all pending payments
            if (request.AmountPerMember != dto.AmountPerMember)
            {
                var pendingPayments = request.Payments.Where(p => p.Status == "pending").ToList();
                foreach (var payment in pendingPayments)
                {
                    payment.Amount = dto.AmountPerMember;
                    await _paymentRepo.UpdateAsync(payment);
                }
            }

            var updated = await _requestRepo.UpdateAsync(request);
            return MapToDto(updated);
        }

        public async Task<FundCollectionRequestDto> GetRequestByIdAsync(int requestId)
        {
            var request = await _requestRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null)
            {
                throw new ArgumentException("Fund collection request not found");
            }

            return MapToDto(request);
        }

        public async Task<IEnumerable<FundCollectionRequestListDto>> GetRequestsByClubIdAsync(int clubId)
        {
            var requests = await _requestRepo.GetByClubIdAsync(clubId);
            return requests.Select(MapToListDto);
        }

        public async Task<IEnumerable<FundCollectionRequestListDto>> GetActiveRequestsByClubIdAsync(int clubId)
        {
            var requests = await _requestRepo.GetActiveByClubIdAsync(clubId);
            return requests.Select(MapToListDto);
        }

        public async Task<bool> CancelRequestAsync(int requestId, int userId)
        {
            var request = await _requestRepo.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new ArgumentException("Fund collection request not found");
            }

            // Validate user is club manager
            await ValidateClubManagerPermissionAsync(request.ClubId, userId);

            // Validate can cancel
            if (request.Status != "active")
            {
                throw new InvalidOperationException("Only active requests can be cancelled");
            }

            request.Status = "cancelled";
            await _requestRepo.UpdateAsync(request);
            
            return true;
        }

        public async Task<bool> CompleteRequestAsync(int requestId, int userId)
        {
            var request = await _requestRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null)
            {
                throw new ArgumentException("Fund collection request not found");
            }

            // Validate user is club manager
            await ValidateClubManagerPermissionAsync(request.ClubId, userId);

            // Calculate total collected amount
            var totalCollected = request.Payments
                .Where(p => p.Status == "paid" || p.Status == "confirmed")
                .Sum(p => p.Amount);

            // Only create transaction if there's collected money
            if (totalCollected > 0)
            {
                // Create PaymentTransaction (Income)
                var transaction = new PaymentTransaction
                {
                    ClubId = request.ClubId,
                    Type = "Income",
                    Amount = totalCollected,
                    Title = $"Fund Collection: {request.Title}",
                    Description = $"Completed fund collection. Total collected from {request.Payments.Count(p => p.Status == "paid" || p.Status == "confirmed")} members.",
                    Category = "member_fees",
                    Method = "Multiple",
                    Status = "completed",
                    TransactionDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                };

                _context.PaymentTransactions.Add(transaction);
            }

            // Update fund collection status
            request.Status = "completed";
            request.UpdatedAt = DateTime.UtcNow;
            await _requestRepo.UpdateAsync(request);
            
            // Save all changes
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<IEnumerable<FundCollectionPaymentDto>> GetPaymentsByRequestIdAsync(int requestId)
        {
            var payments = await _paymentRepo.GetByRequestIdWithDetailsAsync(requestId);
            return payments.Select(MapPaymentToDto);
        }

        public async Task<FundCollectionPaymentDto> ConfirmPaymentAsync(int paymentId, ConfirmPaymentDto dto, int confirmedById)
        {
            var payment = await _paymentRepo.GetByIdWithDetailsAsync(paymentId);
            if (payment == null)
            {
                throw new ArgumentException("Payment not found");
            }

            // Validate user is club manager
            await ValidateClubManagerPermissionAsync(payment.FundCollectionRequest.ClubId, confirmedById);

            // Validate payment status - allow confirming both "pending" and "unconfirmed"
            if (payment.Status != "pending" && payment.Status != "unconfirmed")
            {
                throw new InvalidOperationException("Only pending or unconfirmed payments can be confirmed");
            }

            // Validate paid date
            if (dto.PaidAt > DateTime.UtcNow)
            {
                throw new ArgumentException("Payment date cannot be in the future");
            }

            // Update payment
            payment.Status = "paid";
            payment.PaidAt = dto.PaidAt;
            payment.PaymentMethod = dto.PaymentMethod;
            payment.Notes = dto.Notes;
            payment.ConfirmedById = confirmedById;

            var updated = await _paymentRepo.UpdateAsync(payment);

            // Send notification to member (in-app only, no email)
            try
            {
                await _notificationService.NotifyMemberAboutPaymentConfirmationAsync(
                    payment.ClubMember.Student.UserId,
                    payment.FundCollectionRequest.ClubId,
                    payment.FundCollectionRequest.Title,
                    payment.Amount,
                    payment.PaymentMethod ?? "N/A"
                );
            }
            catch (Exception ex)
            {
                // Log but don't fail the confirmation
                Console.WriteLine($"Failed to send notification: {ex.Message}");
            }

            // Auto-complete fund collection if all members have paid
            await CheckAndAutoCompleteRequestAsync(payment.FundCollectionRequestId, confirmedById);

            return MapPaymentToDto(updated);
        }

        private async Task CheckAndAutoCompleteRequestAsync(int requestId, int userId)
        {
            // Get request with all payments
            var request = await _requestRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null || request.Status != "active")
            {
                return; // Only auto-complete active requests
            }

            // Check if all payments are paid
            var totalPayments = request.Payments.Count;
            var paidPayments = request.Payments.Count(p => p.Status == "paid");

            // If 100% members have paid, auto-complete the request
            if (totalPayments > 0 && paidPayments == totalPayments)
            {
                // Calculate total collected amount
                var totalCollected = request.Payments.Sum(p => p.Amount);

                // Create PaymentTransaction (Income)
                var transaction = new PaymentTransaction
                {
                    ClubId = request.ClubId,
                    Type = "Income",
                    Amount = totalCollected,
                    Title = $"Fund Collection: {request.Title}",
                    Description = $"Auto-completed fund collection. Total collected from {paidPayments} members (100%).",
                    Category = "member_fees",
                    Method = "Multiple",
                    Status = "completed",
                    TransactionDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                };

                _context.PaymentTransactions.Add(transaction);

                // Update fund collection status to completed
                request.Status = "completed";
                request.UpdatedAt = DateTime.UtcNow;
                await _requestRepo.UpdateAsync(request);

                // Save all changes
                await _context.SaveChangesAsync();

                Console.WriteLine($"Auto-completed fund collection request {requestId} - 100% members paid ({paidPayments}/{totalPayments})");
            }
        }

        public async Task<FundCollectionPaymentDto> RejectPaymentAsync(int paymentId, string reason, int userId)
        {
            var payment = await _paymentRepo.GetByIdWithDetailsAsync(paymentId);
            if (payment == null)
            {
                throw new ArgumentException("Payment not found");
            }

            // Validate user is club manager
            await ValidateClubManagerPermissionAsync(payment.FundCollectionRequest.ClubId, userId);

            // Reset to pending
            payment.Status = "pending";
            payment.PaidAt = null;
            payment.PaymentMethod = null;
            payment.Notes = $"Rejected: {reason}";
            payment.ConfirmedById = null;

            var updated = await _paymentRepo.UpdateAsync(payment);
            return MapPaymentToDto(updated);
        }

        public async Task<bool> SendReminderAsync(SendReminderDto dto, int clubId)
        {
            if (!dto.PaymentIds.Any())
            {
                throw new ArgumentException("At least one payment ID is required");
            }

            foreach (var paymentId in dto.PaymentIds)
            {
                var payment = await _paymentRepo.GetByIdWithDetailsAsync(paymentId);
                if (payment == null) continue;

                // Validate payment belongs to club
                if (payment.FundCollectionRequest?.ClubId != clubId) continue;

                // Update reminder count
                payment.ReminderCount++;
                payment.LastReminderAt = DateTime.UtcNow;
                await _paymentRepo.UpdateAsync(payment);

                // Send email reminder ONLY (manual reminder by manager)
                // In-app notifications are handled automatically by background service at 3 & 1 day before due
                try
                {
                    var dueDate = payment.FundCollectionRequest.DueDate;
                    var daysUntilDue = (dueDate - DateTime.UtcNow).Days;
                    var studentEmail = payment.ClubMember.Student.Email;
                    var studentName = payment.ClubMember.Student.FullName;

                    // Send email reminder only (no in-app notification to avoid duplication)
                    if (!string.IsNullOrEmpty(studentEmail))
                    {
                        var clubName = payment.FundCollectionRequest.Club?.Name ?? "Club";
                        await _emailService.SendPaymentReminderEmailAsync(
                            studentEmail,
                            studentName,
                            clubName,
                            payment.FundCollectionRequest.Title,
                            payment.Amount,
                            dueDate,
                            daysUntilDue
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the reminder
                    Console.WriteLine($"Failed to send reminder: {ex.Message}");
                }
            }

            return true;
        }

        public async Task<FundCollectionStatisticsDto> GetClubStatisticsAsync(int clubId)
        {
            var requests = await _requestRepo.GetActiveByClubIdAsync(clubId);
            var allPayments = requests.SelectMany(r => r.Payments).ToList();

            var totalMembers = await _requestRepo.GetTotalMembersAsync(clubId);
            
            // Count unique members by status (a member may have multiple payments across different requests)
            // Group by ClubMemberId and check if they have ANY payment with each status
            var memberPaymentGroups = allPayments.GroupBy(p => p.ClubMemberId).ToList();
            
            var paidMembers = memberPaymentGroups.Count(g => g.Any(p => p.Status == "paid" || p.Status == "confirmed"));
            var pendingMembers = memberPaymentGroups.Count(g => g.Any(p => p.Status == "pending"));
            var unconfirmedMembers = memberPaymentGroups.Count(g => g.Any(p => p.Status == "unconfirmed"));
            var overdueMembers = memberPaymentGroups.Count(g => g.Any(p => 
                (p.Status == "pending" || p.Status == "unconfirmed") && 
                p.FundCollectionRequest.DueDate < DateTime.UtcNow &&
                p.FundCollectionRequest.Status == "active"));

            var totalCollected = allPayments.Where(p => p.Status == "paid" || p.Status == "confirmed").Sum(p => p.Amount);
            var totalPending = allPayments.Where(p => p.Status == "pending" || p.Status == "unconfirmed").Sum(p => p.Amount);
            var expectedTotal = totalCollected + totalPending;

            return new FundCollectionStatisticsDto
            {
                TotalMembers = totalMembers,
                PaidMembers = paidMembers,
                PendingMembers = pendingMembers,
                UnconfirmedMembers = unconfirmedMembers,
                OverdueMembers = overdueMembers,
                TotalCollected = totalCollected,
                TotalPending = totalPending,
                ExpectedTotal = expectedTotal,
                CollectionRate = expectedTotal > 0 ? (double)totalCollected / (double)expectedTotal * 100 : 0
            };
        }

        public async Task<IEnumerable<MemberPaymentSummaryDto>> GetMemberPaymentSummariesAsync(int clubId)
        {
            var members = await _clubMemberRepo.GetByClubIdAsync(clubId);
            var requests = await _requestRepo.GetByClubIdAsync(clubId);
            
            var summaries = new List<MemberPaymentSummaryDto>();

            foreach (var member in members)
            {
                var payments = requests.SelectMany(r => r.Payments)
                    .Where(p => p.ClubMemberId == member.Id)
                    .ToList();

                summaries.Add(new MemberPaymentSummaryDto
                {
                    StudentId = member.StudentId,
                    StudentCode = member.Student.StudentCode,
                    StudentName = member.Student.FullName,
                    StudentEmail = member.Student.Email,
                    AvatarUrl = null, // Avatar from User model if needed
                    TotalRequests = payments.Count,
                    PaidCount = payments.Count(p => p.Status == "paid"),
                    PendingCount = payments.Count(p => p.Status == "pending"),
                    OverdueCount = payments.Count(p => 
                        p.Status == "pending" && 
                        p.FundCollectionRequest.DueDate < DateTime.UtcNow &&
                        p.FundCollectionRequest.Status == "active"),
                    TotalPaid = payments.Where(p => p.Status == "paid").Sum(p => p.Amount),
                    TotalPending = payments.Where(p => p.Status == "pending").Sum(p => p.Amount),
                    LastPaymentDate = payments.Where(p => p.PaidAt.HasValue)
                        .OrderByDescending(p => p.PaidAt)
                        .FirstOrDefault()?.PaidAt
                });
            }

            return summaries.OrderBy(s => s.StudentName);
        }

        public async Task<IEnumerable<FundCollectionPaymentDto>> GetMemberPaymentsAsync(int clubId, int userId)
        {
            // Get club member by user ID
            var member = await _clubMemberRepo.GetByClubAndUserIdAsync(clubId, userId);
            if (member == null)
            {
                return Enumerable.Empty<FundCollectionPaymentDto>();
            }

            // Get all payments for this member
            var payments = await _paymentRepo.GetByClubMemberIdAsync(member.Id);
            return payments.Select(MapPaymentToDto);
        }

        public async Task<FundCollectionPaymentDto> MemberSubmitPaymentAsync(int paymentId, MemberPayDto dto, int userId)
        {
            // Get payment with details
            var payment = await _paymentRepo.GetByIdWithDetailsAsync(paymentId);
            if (payment == null)
            {
                throw new ArgumentException("Payment not found");
            }

            // Verify user is the member for this payment
            var member = await _clubMemberRepo.GetByClubAndUserIdAsync(payment.FundCollectionRequest.ClubId, userId);
            if (member == null || member.Id != payment.ClubMemberId)
            {
                throw new UnauthorizedAccessException("You are not authorized to pay this request");
            }

            // Check if already paid
            if (payment.Status == "paid" || payment.Status == "confirmed")
            {
                throw new ArgumentException("This payment has already been completed");
            }

            // Validate payment method against allowed methods
            var allowedMethods = payment.FundCollectionRequest.PaymentMethods ?? "All";
            if (allowedMethods != "All" && allowedMethods != dto.PaymentMethod)
            {
                throw new ArgumentException($"Payment method '{dto.PaymentMethod}' is not allowed. Only '{allowedMethods}' is accepted.");
            }

            // Update payment
            payment.PaymentMethod = dto.PaymentMethod;
            payment.Notes = dto.Notes;
            
            // Set status based on payment method
            // Cash: "unconfirmed" - requires manager confirmation
            // Bank Transfer: "pending" - awaiting verification
            if (dto.PaymentMethod == "Cash")
            {
                payment.Status = "unconfirmed";
            }
            else
            {
                payment.Status = "pending";
                payment.PaidAt = DateTime.UtcNow; // Member claims payment completed
            }

            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepo.UpdateAsync(payment);

            // Send notification to club manager
            try
            {
                // Get club manager
                var clubManagers = await _clubMemberRepo.GetByClubIdAsync(payment.FundCollectionRequest.ClubId);
                var manager = clubManagers.FirstOrDefault(m => 
                    m.RoleInClub == "Manager" || 
                    m.RoleInClub == "President" || 
                    m.RoleInClub == "VicePresident");

                if (manager != null)
                {
                    var memberName = payment.ClubMember.Student.FullName;
                    var fundTitle = payment.FundCollectionRequest.Title;

                    if (dto.PaymentMethod == "Cash")
                    {
                        await _notificationService.NotifyClubManagerAboutCashPaymentAsync(
                            manager.Student.UserId,
                            payment.FundCollectionRequest.ClubId,
                            memberName,
                            fundTitle,
                            payment.Amount
                        );
                    }
                    else if (dto.PaymentMethod == "Bank Transfer")
                    {
                        await _notificationService.NotifyClubManagerAboutBankTransferPaymentAsync(
                            manager.Student.UserId,
                            payment.FundCollectionRequest.ClubId,
                            memberName,
                            fundTitle,
                            payment.Amount
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the payment submission
                Console.WriteLine($"Failed to send notification: {ex.Message}");
            }

            return MapPaymentToDto(payment);
        }

        public async Task ValidateClubManagerPermissionAsync(int clubId, int userId)
        {
            var member = await _clubMemberRepo.GetByClubAndUserIdAsync(clubId, userId);
            if (member == null)
            {
                throw new UnauthorizedAccessException("You are not a member of this club");
            }
            
            // Allow Manager (same as President) and VicePresident roles
            if (member.RoleInClub != "Manager" && 
                member.RoleInClub != "President" && 
                member.RoleInClub != "VicePresident")
            {
                throw new UnauthorizedAccessException($"Only club managers can perform this action. Your role: {member.RoleInClub}");
            }
        }

        // Helper mapping methods
        private FundCollectionRequestDto MapToDto(FundCollectionRequest request)
        {
            var paidCount = request.Payments.Count(p => p.Status == "paid" || p.Status == "confirmed");
            var pendingCount = request.Payments.Count(p => p.Status == "pending");
            var unconfirmedCount = request.Payments.Count(p => p.Status == "unconfirmed");
            var totalCollected = request.Payments.Where(p => p.Status == "paid" || p.Status == "confirmed").Sum(p => p.Amount);

            return new FundCollectionRequestDto
            {
                Id = request.Id,
                ClubId = request.ClubId,
                ClubName = request.Club.Name,
                Title = request.Title,
                Description = request.Description,
                AmountPerMember = request.AmountPerMember,
                DueDate = request.DueDate,
                Status = request.Status,
                PaymentMethods = request.PaymentMethods,
                Notes = request.Notes,
                CreatedById = request.CreatedById,
                CreatedByName = request.CreatedBy.FullName,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                TotalMembers = request.Payments.Count,
                PaidCount = paidCount,
                PendingCount = pendingCount,
                UnconfirmedCount = unconfirmedCount,
                TotalCollected = totalCollected,
                ExpectedTotal = request.AmountPerMember * request.Payments.Count
            };
        }

        private FundCollectionRequestListDto MapToListDto(FundCollectionRequest request)
        {
            var paidCount = request.Payments.Count(p => p.Status == "paid" || p.Status == "confirmed");
            var pendingCount = request.Payments.Count(p => p.Status == "pending");
            var unconfirmedCount = request.Payments.Count(p => p.Status == "unconfirmed");
            var totalCollected = request.Payments.Where(p => p.Status == "paid" || p.Status == "confirmed").Sum(p => p.Amount);

            return new FundCollectionRequestListDto
            {
                Id = request.Id,
                Title = request.Title,
                AmountPerMember = request.AmountPerMember,
                DueDate = request.DueDate,
                Status = request.Status,
                TotalMembers = request.Payments.Count,
                PaidCount = paidCount,
                PendingCount = pendingCount,
                UnconfirmedCount = unconfirmedCount,
                TotalCollected = totalCollected,
                CreatedAt = request.CreatedAt
            };
        }

        private FundCollectionPaymentDto MapPaymentToDto(FundCollectionPayment payment)
        {
            return new FundCollectionPaymentDto
            {
                Id = payment.Id,
                FundCollectionRequestId = payment.FundCollectionRequestId,
                FundCollectionTitle = payment.FundCollectionRequest.Title,
                FundCollectionRequest = new FundCollectionRequestSummaryDto
                {
                    Id = payment.FundCollectionRequest.Id,
                    Title = payment.FundCollectionRequest.Title,
                    Description = payment.FundCollectionRequest.Description,
                    AmountPerMember = payment.FundCollectionRequest.AmountPerMember,
                    DueDate = payment.FundCollectionRequest.DueDate,
                    Status = payment.FundCollectionRequest.Status,
                    CreatedAt = payment.FundCollectionRequest.CreatedAt
                },
                ClubMemberId = payment.ClubMemberId,
                StudentId = payment.ClubMember.StudentId,
                StudentCode = payment.ClubMember.Student.StudentCode,
                StudentName = payment.ClubMember.Student.FullName,
                StudentEmail = payment.ClubMember.Student.Email,
                AvatarUrl = null, // Avatar from User model if needed
                Amount = payment.Amount,
                Status = payment.Status,
                PaidAt = payment.PaidAt,
                PaymentMethod = payment.PaymentMethod,
                PaymentTransactionId = payment.PaymentTransactionId,
                Notes = payment.Notes,
                ConfirmedById = payment.ConfirmedById,
                ConfirmedByName = payment.ConfirmedBy?.FullName,
                ReminderCount = payment.ReminderCount,
                LastReminderAt = payment.LastReminderAt,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt
            };
        }
    }
}

