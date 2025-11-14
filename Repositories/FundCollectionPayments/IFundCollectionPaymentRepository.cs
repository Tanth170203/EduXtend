using BusinessObject.Models;

namespace Repositories.FundCollectionPayments
{
    public interface IFundCollectionPaymentRepository
    {
        Task<FundCollectionPayment?> GetByIdAsync(int id);
        Task<FundCollectionPayment?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<FundCollectionPayment>> GetByRequestIdAsync(int requestId);
        Task<IEnumerable<FundCollectionPayment>> GetByRequestIdWithDetailsAsync(int requestId);
        Task<IEnumerable<FundCollectionPayment>> GetByClubMemberIdAsync(int clubMemberId);
        Task<IEnumerable<FundCollectionPayment>> GetPendingByRequestIdAsync(int requestId);
        Task<IEnumerable<FundCollectionPayment>> GetOverduePaymentsAsync(int clubId);
        Task<FundCollectionPayment> CreateAsync(FundCollectionPayment payment);
        Task<IEnumerable<FundCollectionPayment>> CreateManyAsync(IEnumerable<FundCollectionPayment> payments);
        Task<FundCollectionPayment> UpdateAsync(FundCollectionPayment payment);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}





