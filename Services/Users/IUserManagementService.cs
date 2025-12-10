using BusinessObject.DTOs.User;

namespace Services.Users;

public interface IUserManagementService
{
    Task<List<UserDto>> GetAllAsync();
    Task<List<UserWithRolesDto>> GetAllWithRolesAsync();
    Task<UserWithRolesDto?> GetByIdWithRolesAsync(int id);
    Task BanUserAsync(int userId);
    Task UnbanUserAsync(int userId);
    Task UpdateUserRolesAsync(int userId, List<int> roleIds, int? clubId = null);
    Task<List<RoleDto>> GetAllRolesAsync();
}

