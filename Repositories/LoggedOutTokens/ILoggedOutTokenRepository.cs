using System;
using System.Threading.Tasks;

namespace Repositories.LoggedOutTokens
{
    public interface ILoggedOutTokenRepository
    {
        /// <summary>
        /// Add a token to the blacklist
        /// </summary>
        Task AddAsync(string token, int? userId, DateTime expiresAt, string? reason = null);

        /// <summary>
        /// Check if a token has been logged out (blacklisted)
        /// </summary>
        Task<bool> IsTokenBlacklistedAsync(string token);

        /// <summary>
        /// Remove expired tokens from blacklist (cleanup job)
        /// </summary>
        Task<int> RemoveExpiredTokensAsync();
    }
}
