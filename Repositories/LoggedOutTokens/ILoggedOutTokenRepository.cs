using System;
using System.Threading.Tasks;

namespace Repositories.LoggedOutTokens
{
    public interface ILoggedOutTokenRepository
    {
        /// <summary>
        /// Add a token hash to the blacklist (uses SHA256 hash of token)
        /// </summary>
        /// <param name="tokenHash">SHA256 hash of the JWT token (64 char hex)</param>
        /// <param name="userId">User ID who is logging out</param>
        /// <param name="expiresAt">When the token expires</param>
        /// <param name="reason">Reason for blacklisting</param>
        Task AddAsync(string tokenHash, int? userId, DateTime expiresAt, string? reason = null);

        /// <summary>
        /// Add a full token to the blacklist (token will be hashed internally)
        /// </summary>
        /// <param name="token">Full JWT token</param>
        /// <param name="userId">User ID who is logging out</param>
        /// <param name="expiresAt">When the token expires</param>
        /// <param name="reason">Reason for blacklisting</param>
        Task AddFullTokenAsync(string token, int? userId, DateTime expiresAt, string? reason = null);

        /// <summary>
        /// Check if a token has been logged out (blacklisted)
        /// </summary>
        /// <param name="token">Full JWT token to check</param>
        Task<bool> IsTokenBlacklistedAsync(string token);

        /// <summary>
        /// Remove expired tokens from blacklist (cleanup job)
        /// </summary>
        Task<int> RemoveExpiredTokensAsync();
    }
}
