using BusinessObject.DTOs.JoinRequest;

namespace Services.JoinRequests
{
    public interface IJoinRequestService
    {
        Task<JoinRequestDto?> GetByIdAsync(int id);
        Task<List<JoinRequestDto>> GetByClubIdAsync(int clubId);
        Task<List<JoinRequestDto>> GetByUserIdAsync(int userId);
        Task<List<JoinRequestDto>> GetPendingByClubIdAsync(int clubId);
        Task<JoinRequestDto> CreateAsync(int userId, CreateJoinRequestDto dto);
        Task<bool> ProcessRequestAsync(int requestId, int processedById, string action);
        Task<bool> CanApplyAsync(int userId, int clubId);
        Task<List<DepartmentDto>> GetClubDepartmentsAsync(int clubId);
        Task<JoinRequestDto?> GetMyRequestForClubAsync(int userId, int clubId);
    }
}

