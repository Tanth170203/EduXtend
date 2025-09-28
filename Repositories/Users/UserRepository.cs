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
        public UserRepository(EduXtendContext db) => _db = db;

        public Task<User?> FindByEmailAsync(string email)
            => _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

        public Task<User?> FindByGoogleSubAsync(string googleSub)
            => _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.GoogleSubject == googleSub);

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

        public Task<UserToken?> GetValidRefreshTokenAsync(string refreshToken)
            => _db.UserTokens.Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken && !t.Revoked);

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }

}
