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
            => await _db.Users.Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> FindByGoogleSubAsync(string googleSub)
            => await _db.Users.Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.GoogleSubject == googleSub);

        public async Task<User?> GetByIdAsync(int id)
            => await _db.Users.Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

        public async Task<List<User>> GetAllAsync()
            => await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.FullName)
                .ToListAsync();

        public async Task<List<User>> GetUsersByRoleIdAsync(int roleId)
            => await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
                .OrderBy(u => u.FullName)
                .ToListAsync();

        public async Task<List<User>> GetUsersWithoutStudentProfileAsync()
            => await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.RoleId == 2) && !_db.Students.Any(s => s.UserId == u.Id))
                .OrderBy(u => u.FullName)
                .ToListAsync();

        public async Task AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
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

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
