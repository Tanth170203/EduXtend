using BusinessObject.DTOs.User;
using Repositories.Users;
using Repositories.Roles;

namespace Services.Users;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;

    public UserManagementService(IUserRepository userRepo, IRoleRepository roleRepo)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
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

    public async Task UpdateUserRolesAsync(int userId, List<int> roleIds)
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

