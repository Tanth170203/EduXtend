using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ChatSessions
{
    public class ChatSessionRepository : IChatSessionRepository
    {
        private readonly EduXtendContext _db;

        public ChatSessionRepository(EduXtendContext db)
        {
            _db = db;
        }

        public async Task<ChatSession?> GetByIdAsync(int id, bool includeMessages = false)
        {
            var query = _db.ChatSessions.AsQueryable();

            if (includeMessages)
            {
                query = query.Include(cs => cs.Messages.OrderBy(m => m.CreatedAt));
            }

            return await query
                .Include(cs => cs.Student)
                .FirstOrDefaultAsync(cs => cs.Id == id);
        }

        public async Task<ChatSession?> GetActiveSessionByStudentIdAsync(int studentId)
        {
            return await _db.ChatSessions
                .Include(cs => cs.Student)
                .Include(cs => cs.Messages.OrderBy(m => m.CreatedAt))
                .Where(cs => cs.StudentId == studentId && cs.IsActive)
                .OrderByDescending(cs => cs.LastMessageAt ?? cs.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ChatSession>> GetSessionsByStudentIdAsync(int studentId)
        {
            return await _db.ChatSessions
                .Include(cs => cs.Student)
                .Where(cs => cs.StudentId == studentId)
                .OrderByDescending(cs => cs.LastMessageAt ?? cs.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatSession> CreateAsync(ChatSession session)
        {
            _db.ChatSessions.Add(session);
            await _db.SaveChangesAsync();
            return session;
        }

        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();
            return message;
        }

        public async Task UpdateSessionTimestampAsync(int sessionId)
        {
            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastMessageAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
