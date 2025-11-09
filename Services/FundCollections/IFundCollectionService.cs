using BusinessObject.DTOs.FundCollection;

namespace Services.FundCollections
{
    public interface IFundCollectionService
    {
        // Fund Collection Request Management
        Task<FundCollectionRequestDto> CreateRequestAsync(int clubId, CreateFundCollectionRequestDto dto, int createdById);
        Task<FundCollectionRequestDto> UpdateRequestAsync(int requestId, UpdateFundCollectionRequestDto dto, int userId);
        Task<FundCollectionRequestDto> GetRequestByIdAsync(int requestId);
        Task<IEnumerable<FundCollectionRequestListDto>> GetRequestsByClubIdAsync(int clubId);
        Task<IEnumerable<FundCollectionRequestListDto>> GetActiveRequestsByClubIdAsync(int clubId);
        Task<bool> CancelRequestAsync(int requestId, int userId);
        Task<bool> CompleteRequestAsync(int requestId, int userId);
        
        // Payment Management
        Task<IEnumerable<FundCollectionPaymentDto>> GetPaymentsByRequestIdAsync(int requestId);
        Task<FundCollectionPaymentDto> ConfirmPaymentAsync(int paymentId, ConfirmPaymentDto dto, int confirmedById);
        Task<FundCollectionPaymentDto> RejectPaymentAsync(int paymentId, string reason, int userId);
        Task<bool> SendReminderAsync(SendReminderDto dto, int clubId);
        
        // Statistics
        Task<FundCollectionStatisticsDto> GetClubStatisticsAsync(int clubId);
        Task<IEnumerable<MemberPaymentSummaryDto>> GetMemberPaymentSummariesAsync(int clubId);
        
        // Member payments
        Task<IEnumerable<FundCollectionPaymentDto>> GetMemberPaymentsAsync(int clubId, int userId);
        Task<FundCollectionPaymentDto> MemberSubmitPaymentAsync(int paymentId, MemberPayDto dto, int userId);
        
        // Validation
        Task ValidateClubManagerPermissionAsync(int clubId, int userId);
    }
}

