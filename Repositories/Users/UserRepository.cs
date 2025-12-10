using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly EduXtendContext _db;

        public UserRepository(EduXtendContext db)
        {
            _db = db;
        }

        public async Task<User?> FindByEmailAsync(string email)
            => await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> FindByGoogleSubAsync(string googleSub)
            => await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.GoogleSubject == googleSub);

        public async Task<User?> GetByIdAsync(int id)
            => await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

        public async Task<User?> GetByIdWithRolesAsync(int id)
            => await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

        public async Task<List<User>> GetAllAsync()
            => await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

        public async Task<List<User>> GetAllWithRolesAsync()
            => await _db.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

        public async Task AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task AddUserTokenAsync(UserToken token)
        {
            _db.UserTokens.Add(token);
            await _db.SaveChangesAsync();
        }

        public async Task<UserToken?> GetValidTokenAsync(string refreshToken)
            => await _db.UserTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken && !t.Revoked && t.ExpiryDate > DateTime.UtcNow);

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task BanUserAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = false;
                await _db.SaveChangesAsync();
            }
        }

        public async Task UnbanUserAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task UpdateUserRolesAsync(int userId, List<int> roleIds)
        {
            // Now each user has only one role, so just update the RoleId
            var user = await _db.Users.FindAsync(userId);
            if (user != null && roleIds.Count > 0)
            {
                user.RoleId = roleIds[0]; // Take first role only
                await _db.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();

        public async Task<List<User>> GetUsersByEmailsAsync(List<string> emails)
            => await _db.Users
                .Include(u => u.Role)
                .Where(u => emails.Contains(u.Email))
                .ToListAsync();

        public async Task AddRangeAsync(List<User> users)
        {
            await _db.Users.AddRangeAsync(users);
            await _db.SaveChangesAsync();
        }

        public async Task<Dictionary<string, int>> GetRoleIdsByNamesAsync(List<string> roleNames)
        {
            var roles = await _db.Roles
                .Where(r => roleNames.Contains(r.RoleName))
                .ToDictionaryAsync(r => r.RoleName, r => r.Id);
            return roles;
        }

        public async Task<List<User>> GetUsersByRoleAsync(string roleName)
        {
            return await _db.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && u.Role.RoleName == roleName && u.IsActive)
                .ToListAsync();
        }
    }
}
