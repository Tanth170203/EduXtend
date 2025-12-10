using BusinessObject.DTOs.User;
using BusinessObject.Models;
using Repositories.Users;
using Repositories.Roles;
using Repositories.ClubMembers;
using Repositories.Students;
using Repositories.Clubs;

namespace Services.Users;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IClubMemberRepository _clubMemberRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IClubRepository _clubRepo;

    public UserManagementService(
        IUserRepository userRepo, 
        IRoleRepository roleRepo,
        IClubMemberRepository clubMemberRepo,
        IStudentRepository studentRepo,
        IClubRepository clubRepo)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _clubMemberRepo = clubMemberRepo;
        _studentRepo = studentRepo;
        _clubRepo = clubRepo;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userRepo.GetAllWithRolesAsync();
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            AvatarUrl = u.AvatarUrl,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            Roles = u.Role != null ? new List<string> { u.Role.RoleName } : new List<string>()
        }).ToList();
    }

    public async Task<List<UserWithRolesDto>> GetAllWithRolesAsync()
    {
        var users = await _userRepo.GetAllWithRolesAsync();
        return users.Select(u => new UserWithRolesDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            AvatarUrl = u.AvatarUrl,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            Roles = u.Role != null ? new List<RoleDto>
            {
                new RoleDto
                {
                    Id = u.Role.Id,
                    RoleName = u.Role.RoleName,
                    Description = u.Role.Description
                }
            } : new List<RoleDto>(),
            RoleIds = u.Role != null ? new List<int> { u.RoleId } : new List<int>()
        }).ToList();
    }

    public async Task<UserWithRolesDto?> GetByIdWithRolesAsync(int id)
    {
        var user = await _userRepo.GetByIdWithRolesAsync(id);
        if (user == null)
            return null;

        return new UserWithRolesDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = user.Role != null ? new List<RoleDto>
            {
                new RoleDto
                {
                    Id = user.Role.Id,
                    RoleName = user.Role.RoleName,
                    Description = user.Role.Description
                }
            } : new List<RoleDto>(),
            RoleIds = user.Role != null ? new List<int> { user.RoleId } : new List<int>()
        };
    }

    public async Task BanUserAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        await _userRepo.BanUserAsync(userId);
    }

    public async Task UnbanUserAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        await _userRepo.UnbanUserAsync(userId);
    }

    public async Task UpdateUserRolesAsync(int userId, List<int> roleIds, int? clubId = null)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        // Validate all roles exist
        var allRoles = await _roleRepo.GetAllAsync();
        var validRoleIds = allRoles.Select(r => r.Id).ToList();
        var invalidRoles = roleIds.Except(validRoleIds).ToList();

        if (invalidRoles.Any())
            throw new ArgumentException($"Invalid role IDs: {string.Join(", ", invalidRoles)}");

        // Get the role being assigned
        var roleId = roleIds.FirstOrDefault();
        var role = allRoles.FirstOrDefault(r => r.Id == roleId);
        
        // Check if role is ClubMember or ClubManager - require clubId
        if (role != null && (role.RoleName == "ClubMember" || role.RoleName == "ClubManager"))
        {
            if (!clubId.HasValue)
                throw new ArgumentException("Club selection is required for ClubMember or ClubManager role");

            // Validate club exists
            var club = await _clubRepo.GetByIdAsync(clubId.Value);
            if (club == null)
                throw new ArgumentException($"Club with ID {clubId} not found");

            // Get student record for this user
            var student = await _studentRepo.GetByUserIdAsync(userId);
            if (student == null)
                throw new ArgumentException("User must have a student record to be assigned ClubMember or ClubManager role");

            // Check if already a member of this club
            var existingMember = await _clubMemberRepo.GetByClubAndStudentIdAsync(clubId.Value, student.Id);
            
            if (existingMember == null)
            {
                // Create new ClubMember record
                var clubMember = new ClubMember
                {
                    ClubId = clubId.Value,
                    StudentId = student.Id,
                    RoleInClub = role.RoleName == "ClubManager" ? "Manager" : "Member",
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                };
                await _clubMemberRepo.CreateAsync(clubMember);
            }
            else
            {
                // Update existing member's role in club
                existingMember.RoleInClub = role.RoleName == "ClubManager" ? "Manager" : "Member";
                existingMember.IsActive = true;
                await _clubMemberRepo.UpdateAsync(existingMember);
            }
        }

        await _userRepo.UpdateUserRolesAsync(userId, roleIds);
    }

    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleRepo.GetAllAsync();
        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            RoleName = r.RoleName,
            Description = r.Description
        }).ToList();
    }
}

