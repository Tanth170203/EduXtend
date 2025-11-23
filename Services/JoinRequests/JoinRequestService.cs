using BusinessObject.DTOs.JoinRequest;
using BusinessObject.Models;
using Repositories.JoinRequests;
using Repositories.Clubs;
using Repositories.Interviews;
using Repositories.Users;
using Microsoft.Extensions.Logging;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Services.JoinRequests
{
    public class JoinRequestService : IJoinRequestService
    {
        private readonly IJoinRequestRepository _repo;
        private readonly IClubRepository _clubRepo;
        private readonly IInterviewRepository _interviewRepo;
        private readonly ILogger<JoinRequestService> _logger;
        private readonly Services.Notifications.INotificationService _notificationService;
        private readonly IUserRepository _userRepo;
        private readonly EduXtendContext _context;

        public JoinRequestService(
            IJoinRequestRepository repo, 
            IClubRepository clubRepo,
            IInterviewRepository interviewRepo,
            ILogger<JoinRequestService> logger,
            Services.Notifications.INotificationService notificationService,
            IUserRepository userRepo,
            EduXtendContext context)
        {
            _repo = repo;
            _clubRepo = clubRepo;
            _interviewRepo = interviewRepo;
            _logger = logger;
            _notificationService = notificationService;
            _userRepo = userRepo;
            _context = context;
        }

        public async Task<JoinRequestDto?> GetByIdAsync(int id)
        {
            var request = await _repo.GetByIdAsync(id);
            if (request == null) return null;

            return MapToDto(request);
        }

        public async Task<List<JoinRequestDto>> GetByClubIdAsync(int clubId)
        {
            var requests = await _repo.GetByClubIdAsync(clubId);
            return requests.Select(MapToDto).ToList();
        }

        public async Task<List<JoinRequestDto>> GetByClubIdWithFilterAsync(int clubId, string? status)
        {
            // If no status filter, return all requests
            if (string.IsNullOrEmpty(status))
            {
                var allRequests = await _repo.GetByClubIdAsync(clubId);
                var allDtos = allRequests.Select(MapToDto).ToList();
                
                // Include interview information
                foreach (var dto in allDtos)
                {
                    var interview = await _interviewRepo.GetByJoinRequestIdAsync(dto.Id);
                    dto.HasInterview = interview != null;
                    dto.InterviewId = interview?.Id;
                }
                
                return allDtos;
            }

            // Filter by status
            var requests = await _repo.GetByClubIdAsync(clubId);
            var filteredRequests = requests.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            var dtos = filteredRequests.Select(MapToDto).ToList();
            
            // Include interview information
            foreach (var dto in dtos)
            {
                var interview = await _interviewRepo.GetByJoinRequestIdAsync(dto.Id);
                dto.HasInterview = interview != null;
                dto.InterviewId = interview?.Id;
            }
            
            return dtos;
        }

        public async Task<List<JoinRequestDto>> GetByUserIdAsync(int userId)
        {
            var requests = await _repo.GetByUserIdAsync(userId);
            var dtos = requests.Select(MapToDto).ToList();
            
            // Check if each request has an interview
            foreach (var dto in dtos)
            {
                var interview = await _interviewRepo.GetByJoinRequestIdAsync(dto.Id);
                dto.HasInterview = interview != null;
                dto.InterviewId = interview?.Id;
            }
            
            return dtos;
        }

        public async Task<List<JoinRequestDto>> GetPendingByClubIdAsync(int clubId)
        {
            var requests = await _repo.GetPendingByClubIdAsync(clubId);
            var dtos = requests.Select(MapToDto).ToList();
            
            // Check if each request has an interview
            foreach (var dto in dtos)
            {
                var interview = await _interviewRepo.GetByJoinRequestIdAsync(dto.Id);
                dto.HasInterview = interview != null;
                dto.InterviewId = interview?.Id;
            }
            
            return dtos;
        }

        public async Task<JoinRequestDto> CreateAsync(int userId, CreateJoinRequestDto dto)
        {
            // Check if club recruitment is open
            var club = await _clubRepo.GetByIdAsync(dto.ClubId);
            if (club == null)
                throw new InvalidOperationException("Club not found");

            if (!club.IsRecruitmentOpen)
                throw new InvalidOperationException("Club recruitment is closed");

            // Check if user already has a pending request
            if (await _repo.HasPendingRequestAsync(userId, dto.ClubId))
                throw new InvalidOperationException("You already have a pending request for this club");

            var request = new JoinRequest
            {
                ClubId = dto.ClubId,
                UserId = userId,
                DepartmentId = dto.DepartmentId,
                Motivation = dto.Motivation,
                CvUrl = dto.CvUrl,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(request);
            
            // Reload with includes
            var result = await _repo.GetByIdAsync(created.Id);
            return MapToDto(result!);
        }

        public async Task<bool> ProcessRequestAsync(int requestId, int processedById, string action)
        {
            if (action != "Approve" && action != "Reject")
                throw new ArgumentException("Action must be 'Approve' or 'Reject'");

            // Get the join request
            var joinRequest = await _repo.GetByIdAsync(requestId);
            if (joinRequest == null)
                throw new Exception("Join request not found");

            var status = action == "Approve" ? "Approved" : "Rejected";
            var result = await _repo.UpdateStatusAsync(requestId, status, processedById);

            // If approved, create ClubMember
            if (result && action == "Approve")
            {
                _logger.LogInformation("Creating ClubMember for UserId: {UserId}, ClubId: {ClubId}", joinRequest.UserId, joinRequest.ClubId);
                var memberCreated = await _repo.CreateClubMemberAsync(joinRequest.ClubId, joinRequest.UserId, joinRequest.DepartmentId);
                _logger.LogInformation("ClubMember creation result: {Result}", memberCreated);

                // Update user role from Student to Member if needed
                try
                {
                    var user = await _userRepo.GetByIdWithRolesAsync(joinRequest.UserId);
                    if (user != null && user.Role.RoleName == "Student")
                    {
                        _logger.LogInformation("Updating user {UserId} role from Student to Member", joinRequest.UserId);
                        
                        // Get Member role ID
                        var memberRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Member");
                        if (memberRole != null)
                        {
                            user.RoleId = memberRole.Id;
                            await _userRepo.UpdateAsync(user);
                            _logger.LogInformation("User {UserId} role updated to Member successfully", joinRequest.UserId);
                        }
                        else
                        {
                            _logger.LogWarning("Member role not found in database");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update user role to Member");
                    // Don't fail the approval process if role update fails
                }

                // Send approval notification
                try
                {
                    await _notificationService.NotifyUserAboutJoinRequestApprovalAsync(
                        joinRequest.UserId,
                        joinRequest.ClubId,
                        joinRequest.Club?.Name ?? "the club"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send approval notification");
                }
            }
            else if (result && action == "Reject")
            {
                // Send rejection notification
                try
                {
                    await _notificationService.NotifyUserAboutJoinRequestRejectionAsync(
                        joinRequest.UserId,
                        joinRequest.ClubId,
                        joinRequest.Club?.Name ?? "the club"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send rejection notification");
                }
            }

            return result;
        }

        public async Task<bool> CanApplyAsync(int userId, int clubId)
        {
            // Check if club exists and recruitment is open
            var club = await _clubRepo.GetByIdAsync(clubId);
            if (club == null || !club.IsRecruitmentOpen)
                return false;

            // Check if user already has a pending request
            return !await _repo.HasPendingRequestAsync(userId, clubId);
        }

        public async Task<List<DepartmentDto>> GetClubDepartmentsAsync(int clubId)
        {
            var club = await _clubRepo.GetByIdWithDetailsAsync(clubId);
            if (club == null) return new List<DepartmentDto>();

            return club.Departments.Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                MemberCount = d.Members.Count(m => m.IsActive)
            }).ToList();
        }

        public async Task<JoinRequestDto?> GetMyRequestForClubAsync(int userId, int clubId)
        {
            var request = await _repo.GetActiveRequestByUserAndClubAsync(userId, clubId);
            if (request == null) return null;

            var dto = MapToDto(request);
            
            // Check if request has an interview
            var interview = await _interviewRepo.GetByJoinRequestIdAsync(dto.Id);
            dto.HasInterview = interview != null;
            dto.InterviewId = interview?.Id;
            
            return dto;
        }

        public async Task<JoinRequestDto?> UpdateRequestAsync(int requestId, int userId, UpdateJoinRequestDto dto)
        {
            var request = await _repo.GetByIdAsync(requestId);
            if (request == null) return null;

            // Verify ownership
            if (request.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only update your own requests");
            }

            // Only allow update if status is Pending and no interview scheduled
            if (request.Status != "Pending")
            {
                throw new InvalidOperationException("Cannot update request that has been processed");
            }

            // Check if interview exists
            var hasInterview = await _interviewRepo.ExistsForJoinRequestAsync(requestId);
            if (hasInterview)
            {
                throw new InvalidOperationException("Cannot update request after interview has been scheduled");
            }

            // Update fields
            request.DepartmentId = dto.DepartmentId;
            request.Motivation = dto.Motivation;
            request.CvUrl = dto.CvUrl;

            await _repo.UpdateAsync(request);

            return MapToDto(request);
        }

        private JoinRequestDto MapToDto(JoinRequest request)
        {
            return new JoinRequestDto
            {
                Id = request.Id,
                ClubId = request.ClubId,
                ClubName = request.Club?.Name ?? "",
                ClubLogoUrl = request.Club?.LogoUrl,
                UserId = request.UserId,
                UserName = request.User?.FullName ?? "",
                UserEmail = request.User?.Email ?? "",
                DepartmentId = request.DepartmentId,
                DepartmentName = request.Department?.Name,
                Motivation = request.Motivation,
                CvUrl = request.CvUrl,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                ProcessedAt = request.ProcessedAt,
                ProcessedById = request.ProcessedById,
                ProcessedByName = request.ProcessedBy?.FullName,
                HasInterview = false, // Will be set later
                InterviewId = null
            };
        }
    }
}

