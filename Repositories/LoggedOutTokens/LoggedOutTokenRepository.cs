using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task AddAsync(string token, int? userId, DateTime expiresAt, string? reason = null)
        {
            try
            {
                // Check if token already exists
                var existing = await _context.LoggedOutTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (existing != null)
                    return; // Already blacklisted

                var loggedOutToken = new LoggedOutToken
                {
                    Token = token,
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
                // 2601: Cannot insert duplicate key row
                // 2627: Violation of unique constraint
                return;
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            return await _context.LoggedOutTokens
                .AnyAsync(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);
        }

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
