using BusinessObject.DTOs.JoinRequest;
using BusinessObject.Models;
using Repositories.JoinRequests;
using Repositories.Clubs;
using Repositories.Interviews;
using Microsoft.Extensions.Logging;

namespace Services.JoinRequests
{
    public class JoinRequestService : IJoinRequestService
    {
        private readonly IJoinRequestRepository _repo;
        private readonly IClubRepository _clubRepo;
        private readonly IInterviewRepository _interviewRepo;
        private readonly ILogger<JoinRequestService> _logger;

        public JoinRequestService(
            IJoinRequestRepository repo, 
            IClubRepository clubRepo,
            IInterviewRepository interviewRepo,
            ILogger<JoinRequestService> logger)
        {
            _repo = repo;
            _clubRepo = clubRepo;
            _interviewRepo = interviewRepo;
            _logger = logger;
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

