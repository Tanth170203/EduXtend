using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Roles
{
    public class RoleRepository : IRoleRepository
    {
        private readonly EduXtendContext _db;

        public RoleRepository(EduXtendContext db)
        {
            _db = db;
        }

        public async Task<List<Role>> GetAllAsync()
            => await _db.Roles.OrderBy(r => r.RoleName).ToListAsync();

        public async Task<Role?> GetByIdAsync(int id)
            => await _db.Roles.FirstOrDefaultAsync(r => r.Id == id);

        public async Task<Role?> GetByNameAsync(string roleName)
            => await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

        public async Task<List<UserRole>> GetUserRolesByUserIdAsync(int userId)
            => await _db.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

        public async Task AddUserRoleAsync(UserRole userRole)
        {
            _db.UserRoles.Add(userRole);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveUserRoleAsync(UserRole userRole)
        {
            _db.UserRoles.Remove(userRole);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveAllUserRolesAsync(int userId)
        {
            var userRoles = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            _db.UserRoles.RemoveRange(userRoles);
            await _db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}

