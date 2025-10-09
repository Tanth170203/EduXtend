using BusinessObject.Models;
using Repositories.Users;
using Repositories.Roles;

namespace Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;

        public UserService(IUserRepository userRepo, IRoleRepository roleRepo)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepo.GetAllAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepo.GetByIdAsync(id);
        }

        public async Task<bool> ToggleUserActiveStatusAsync(int userId)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = !user.IsActive;
                await _userRepo.UpdateAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, int newRoleId)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                var role = await _roleRepo.GetByIdAsync(newRoleId);
                if (role == null)
                {
                    return false;
                }

                // Remove all existing roles
                await _roleRepo.RemoveAllUserRolesAsync(userId);

                // Add new role
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = newRoleId,
                    AssignedAt = DateTime.UtcNow
                };
                await _roleRepo.AddUserRoleAsync(userRole);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _roleRepo.GetAllAsync();
        }

        public async Task<List<UserRole>> GetUserRolesAsync(int userId)
        {
            return await _roleRepo.GetUserRolesByUserIdAsync(userId);
        }
    }
}

