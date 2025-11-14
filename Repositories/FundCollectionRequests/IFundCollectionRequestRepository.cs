using BusinessObject.Models;

namespace Repositories.FundCollectionRequests
{
    public interface IFundCollectionRequestRepository
    {
        Task<FundCollectionRequest?> GetByIdAsync(int id);
        Task<FundCollectionRequest?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<FundCollectionRequest>> GetByClubIdAsync(int clubId);
        Task<IEnumerable<FundCollectionRequest>> GetActiveByClubIdAsync(int clubId);
        Task<FundCollectionRequest> CreateAsync(FundCollectionRequest request);
        Task<FundCollectionRequest> UpdateAsync(FundCollectionRequest request);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<int> GetTotalMembersAsync(int clubId);
    }
}





