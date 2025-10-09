using BusinessObject.Models;

namespace Repositories.Roles
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetAllAsync();
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByNameAsync(string roleName);
        Task<List<UserRole>> GetUserRolesByUserIdAsync(int userId);
        Task AddUserRoleAsync(UserRole userRole);
        Task RemoveUserRoleAsync(UserRole userRole);
        Task RemoveAllUserRolesAsync(int userId);
        Task SaveChangesAsync();
    }
}

