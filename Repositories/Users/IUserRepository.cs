using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Users
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByGoogleSubAsync(string googleSub);
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByIdWithRolesAsync(int id);
        Task<List<User>> GetAllAsync();
        Task<List<User>> GetAllWithRolesAsync();
        Task AddAsync(User user);
        Task AddUserTokenAsync(UserToken token);
        Task<UserToken?> GetValidTokenAsync(string refreshToken);
        Task UpdateAsync(User user);
        Task BanUserAsync(int userId);
        Task UnbanUserAsync(int userId);
        Task UpdateUserRolesAsync(int userId, List<int> roleIds);
        Task SaveChangesAsync();
        
        // Bulk operations for import
        Task<List<User>> GetUsersByEmailsAsync(List<string> emails);
        Task AddRangeAsync(List<User> users);
        Task<Dictionary<string, int>> GetRoleIdsByNamesAsync(List<string> roleNames);
    }
}
