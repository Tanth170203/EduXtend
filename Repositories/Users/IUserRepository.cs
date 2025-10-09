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
        Task<List<User>> GetAllAsync();
        Task<List<User>> GetUsersByRoleIdAsync(int roleId);
        Task<List<User>> GetUsersWithoutStudentProfileAsync();
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task AddUserTokenAsync(UserToken token);
        Task<UserToken?> GetValidTokenAsync(string refreshToken);
        Task SaveChangesAsync();
    }
}
