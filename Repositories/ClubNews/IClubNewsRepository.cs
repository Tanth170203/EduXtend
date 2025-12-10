using BusinessObject.Models;

namespace Repositories.ClubNews;

public interface IClubNewsRepository
{
	Task<BusinessObject.Models.ClubNews?> GetByIdAsync(int id);
	Task<List<BusinessObject.Models.ClubNews>> GetAllAsync(int? clubId = null, bool? approvedOnly = null);
	Task<List<BusinessObject.Models.ClubNews>> GetPendingApprovalAsync();
	Task<BusinessObject.Models.ClubNews> CreateAsync(BusinessObject.Models.ClubNews news);
	Task<BusinessObject.Models.ClubNews> UpdateAsync(BusinessObject.Models.ClubNews news);
	Task<bool> DeleteAsync(int id);
}
