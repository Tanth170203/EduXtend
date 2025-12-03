using BusinessObject.Models;

namespace Repositories.ChatSessions
{
    public interface IChatSessionRepository
    {
        Task<ChatSession?> GetByIdAsync(int id, bool includeMessages = false);
        Task<ChatSession?> GetActiveSessionByStudentIdAsync(int studentId);
        Task<List<ChatSession>> GetSessionsByStudentIdAsync(int studentId);
        Task<ChatSession> CreateAsync(ChatSession session);
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
        Task UpdateSessionTimestampAsync(int sessionId);
    }
}
