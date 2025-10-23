using BusinessObject.Models;

namespace Repositories.JoinRequests
{
    public interface IJoinRequestRepository
    {
        Task<JoinRequest?> GetByIdAsync(int id);
        Task<List<JoinRequest>> GetByClubIdAsync(int clubId);
        Task<List<JoinRequest>> GetByUserIdAsync(int userId);
        Task<List<JoinRequest>> GetPendingByClubIdAsync(int clubId);
        Task<JoinRequest?> GetByUserAndClubAsync(int userId, int clubId);
        Task<JoinRequest?> GetActiveRequestByUserAndClubAsync(int userId, int clubId);
        Task<JoinRequest> CreateAsync(JoinRequest request);
        Task<bool> UpdateStatusAsync(int id, string status, int processedById);
        Task<bool> HasPendingRequestAsync(int userId, int clubId);
        Task<bool> CreateClubMemberAsync(int clubId, int userId, int? departmentId);
    }
}

