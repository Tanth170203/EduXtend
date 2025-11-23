using BusinessObject.DTOs.CommunicationPlan;
using BusinessObject.Models;
using Repositories.CommunicationPlans;
using Repositories.Activities;
using Repositories.Users;
using Services.Clubs;

namespace Services.CommunicationPlans
{
    public class CommunicationPlanService : ICommunicationPlanService
    {
        private readonly ICommunicationPlanRepository _planRepo;
        private readonly IActivityRepository _activityRepo;
        private readonly IUserRepository _userRepo;
        private readonly IClubService _clubService;

        public CommunicationPlanService(
            ICommunicationPlanRepository planRepo,
            IActivityRepository activityRepo,
            IUserRepository userRepo,
            IClubService clubService)
        {
            _planRepo = planRepo;
            _activityRepo = activityRepo;
            _userRepo = userRepo;
            _clubService = clubService;
        }

        public async Task<CommunicationPlanDto> CreatePlanAsync(int userId, CreateCommunicationPlanDto dto)
        {
            // Validate activity exists
            var activity = await _activityRepo.GetByIdAsync(dto.ActivityId);
            if (activity == null)
            {
                throw new Exception("Activity not found");
            }

            // Validate activity belongs to a club
            if (!activity.ClubId.HasValue)
            {
                throw new Exception("Activity must belong to a club");
            }

            // Check authorization (Manager or Admin)
            var user = await _userRepo.GetByIdWithRolesAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var isAdmin = user.Role.RoleName == "Admin";
            var isManager = await _activityRepo.IsUserManagerOfClubAsync(userId, activity.ClubId.Value);

            if (!isAdmin && !isManager)
            {
                throw new UnauthorizedAccessException("You don't have permission to create communication plan for this activity");
            }

            // Check unique constraint (one plan per activity)
            var existingPlan = await _planRepo.GetByActivityIdAsync(dto.ActivityId);
            if (existingPlan != null)
            {
                throw new Exception("Communication plan already exists for this activity");
            }

            // Create plan with items
            var plan = new CommunicationPlan
            {
                ActivityId = dto.ActivityId,
                ClubId = activity.ClubId.Value,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CommunicationItem>()
            };

            // Auto-assign order to items
            int order = 1;
            foreach (var itemDto in dto.Items)
            {
                var item = new CommunicationItem
                {
                    Order = order++,
                    Content = itemDto.Content,
                    ScheduledDate = itemDto.ScheduledDate,
                    ResponsiblePerson = itemDto.ResponsiblePerson,
                    Notes = itemDto.Notes
                };
                plan.Items.Add(item);
            }

            var createdPlan = await _planRepo.CreateAsync(plan);
            return MapToDto(createdPlan);
        }

        public async Task<CommunicationPlanDto> UpdatePlanAsync(int userId, int planId, UpdateCommunicationPlanDto dto)
        {
            // Get existing plan
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                throw new Exception("Communication plan not found");
            }

            // Check authorization (Manager or Admin)
            var user = await _userRepo.GetByIdWithRolesAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var isAdmin = user.Role.RoleName == "Admin";
            var isManager = await _activityRepo.IsUserManagerOfClubAsync(userId, plan.ClubId);

            if (!isAdmin && !isManager)
            {
                throw new UnauthorizedAccessException("You don't have permission to update this communication plan");
            }

            // Replace all items with new ones
            plan.Items.Clear();

            // Auto-assign order to new items
            int order = 1;
            foreach (var itemDto in dto.Items)
            {
                var item = new CommunicationItem
                {
                    CommunicationPlanId = planId,
                    Order = order++,
                    Content = itemDto.Content,
                    ScheduledDate = itemDto.ScheduledDate,
                    ResponsiblePerson = itemDto.ResponsiblePerson,
                    Notes = itemDto.Notes
                };
                plan.Items.Add(item);
            }

            var updatedPlan = await _planRepo.UpdateAsync(plan);
            return MapToDto(updatedPlan);
        }

        public async Task<CommunicationPlanDto> GetPlanAsync(int userId, int planId)
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                throw new Exception("Communication plan not found");
            }

            return MapToDto(plan);
        }

        public async Task<List<CommunicationPlanDto>> GetClubPlansAsync(int userId, int clubId)
        {
            var plans = await _planRepo.GetByClubIdAsync(clubId);
            return plans.Select(MapToDto).ToList();
        }

        public async Task<bool> DeletePlanAsync(int userId, int planId)
        {
            // Get existing plan
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null)
            {
                throw new Exception("Communication plan not found");
            }

            // Check authorization (Manager or Admin)
            var user = await _userRepo.GetByIdWithRolesAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var isAdmin = user.Role.RoleName == "Admin";
            var isManager = await _activityRepo.IsUserManagerOfClubAsync(userId, plan.ClubId);

            if (!isAdmin && !isManager)
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this communication plan");
            }

            return await _planRepo.DeleteAsync(planId);
        }

        public async Task<List<AvailableActivityDto>> GetAvailableActivitiesAsync(int userId)
        {
            // Get user's managed club
            var club = await _clubService.GetManagedClubByUserIdAsync(userId);
            if (club == null)
            {
                throw new Exception("User is not managing any club");
            }

            var clubId = club.Id;

            // Get all activities of the club that are Approved or Completed
            var activities = await _activityRepo.GetActivitiesByClubIdAsync(clubId);
            
            // Get all activity IDs that already have communication plans
            var existingPlanActivityIds = await _planRepo.GetActivityIdsWithPlansAsync(clubId);

            // Filter activities: only those without communication plans
            var availableActivities = activities
                .Where(a => (a.Status == "Approved" || a.Status == "Completed" || a.Status == "Scheduled") 
                            && !existingPlanActivityIds.Contains(a.Id))
                .OrderByDescending(a => a.StartTime)
                .Select(a => new AvailableActivityDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Status = a.Status,
                    StartTime = a.StartTime
                })
                .ToList();

            return availableActivities;
        }

        private CommunicationPlanDto MapToDto(CommunicationPlan plan)
        {
            return new CommunicationPlanDto
            {
                Id = plan.Id,
                ActivityId = plan.ActivityId,
                ActivityTitle = plan.Activity?.Title ?? string.Empty,
                CreatedAt = plan.CreatedAt,
                Items = plan.Items
                    .OrderBy(i => i.Order)
                    .Select(i => new CommunicationItemDto
                    {
                        Order = i.Order,
                        Content = i.Content,
                        ScheduledDate = i.ScheduledDate,
                        ResponsiblePerson = i.ResponsiblePerson,
                        Notes = i.Notes
                    })
                    .ToList()
            };
        }
    }
}
