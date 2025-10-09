using BusinessObject.Models;

namespace Services.Users
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> ToggleUserActiveStatusAsync(int userId);
        Task<bool> UpdateUserRoleAsync(int userId, int newRoleId);
        Task<List<Role>> GetAllRolesAsync();
        Task<List<UserRole>> GetUserRolesAsync(int userId);
    }
}

