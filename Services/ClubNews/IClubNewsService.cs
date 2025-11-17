using BusinessObject.DTOs.News;

namespace Services.ClubNews;

public interface IClubNewsService
{
	Task<List<ClubNewsListItemDto>> GetAllAsync(int? clubId = null, bool? approvedOnly = null);
	Task<List<ClubNewsListItemDto>> GetPendingApprovalAsync();
	Task<ClubNewsDetailDto?> GetByIdAsync(int id);
	Task<ClubNewsDetailDto> CreateAsync(int creatorUserId, int clubId, CreateClubNewsRequest request);
	Task<ClubNewsDetailDto> UpdateAsync(int id, int userId, UpdateClubNewsRequest request);
	Task<ClubNewsDetailDto> ApproveAsync(int id, bool approve);
	Task<bool> DeleteAsync(int id, int userId, bool isAdmin);
}
