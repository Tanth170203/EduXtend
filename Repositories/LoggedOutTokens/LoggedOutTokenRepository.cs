using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.LoggedOutTokens
{
    public class LoggedOutTokenRepository : ILoggedOutTokenRepository
    {
        private readonly EduXtendContext _context;

        public LoggedOutTokenRepository(EduXtendContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hash a token using SHA256
        /// </summary>
        private static string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Add a token hash to the blacklist
        /// </summary>
        public async Task AddAsync(string tokenHash, int? userId, DateTime expiresAt, string? reason = null)
        {
            try
            {
                // Check if token hash already exists
                var existing = await _context.LoggedOutTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

                if (existing != null)
                    return; // Already blacklisted

                var loggedOutToken = new LoggedOutToken
                {
                    TokenHash = tokenHash,
                    UserId = userId,
                    ExpiresAt = expiresAt,
                    LoggedOutAt = DateTime.UtcNow,
                    Reason = reason
                };

                _context.LoggedOutTokens.Add(loggedOutToken);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                                               (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                // Duplicate key error - token already blacklisted, ignore
                return;
            }
        }

        /// <summary>
        /// Add a full token to the blacklist (hashes it internally)
        /// </summary>
        public async Task AddFullTokenAsync(string token, int? userId, DateTime expiresAt, string? reason = null)
        {
            var tokenHash = HashToken(token);
            
            try
            {
                // Check if token hash already exists
                var existing = await _context.LoggedOutTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

                if (existing != null)
                    return; // Already blacklisted

                var loggedOutToken = new LoggedOutToken
                {
                    TokenHash = tokenHash,
                    TokenFull = token, // Store full token for debugging
                    UserId = userId,
                    ExpiresAt = expiresAt,
                    LoggedOutAt = DateTime.UtcNow,
                    Reason = reason
                };

                _context.LoggedOutTokens.Add(loggedOutToken);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                                               (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                // Duplicate key error - token already blacklisted, ignore
                return;
            }
        }

        /// <summary>
        /// Check if a token is blacklisted
        /// </summary>
        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            var tokenHash = HashToken(token);
            return await _context.LoggedOutTokens
                .AnyAsync(t => t.TokenHash == tokenHash && t.ExpiresAt > DateTime.UtcNow);
        }

        /// <summary>
        /// Remove expired tokens from blacklist
        /// </summary>
        public async Task<int> RemoveExpiredTokensAsync()
        {
            var expiredTokens = await _context.LoggedOutTokens
                .Where(t => t.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.LoggedOutTokens.RemoveRange(expiredTokens);
                return await _context.SaveChangesAsync();
            }

            return 0;
        }
    }
}
