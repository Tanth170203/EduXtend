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
        Task AddAsync(User user);
        Task AddUserTokenAsync(UserToken token);
        Task<UserToken?> GetValidRefreshTokenAsync(string refreshToken);
        Task SaveChangesAsync();
    }
}
