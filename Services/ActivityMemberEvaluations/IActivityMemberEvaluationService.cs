using BusinessObject.DTOs.ActivityMemberEvaluation;

namespace Services.ActivityMemberEvaluations;

public interface IActivityMemberEvaluationService
{
    /// <summary>
    /// Lấy danh sách assignments của activity để đánh giá
    /// </summary>
    /// <param name="activityId">ID của activity</param>
    /// <returns>Danh sách assignments với trạng thái đánh giá</returns>
    Task<List<ActivityMemberEvaluationListDto>> GetAssignmentsForEvaluationAsync(int activityId);

    /// <summary>
    /// Tạo đánh giá mới cho thành viên
    /// </summary>
    /// <param name="evaluatorId">ID của người đánh giá</param>
    /// <param name="dto">Thông tin đánh giá</param>
    /// <returns>Đánh giá đã được tạo</returns>
    Task<ActivityMemberEvaluationDto> CreateEvaluationAsync(int evaluatorId, CreateActivityMemberEvaluationDto dto);

    /// <summary>
    /// Cập nhật đánh giá hiện có
    /// </summary>
    /// <param name="evaluatorId">ID của người đánh giá</param>
    /// <param name="evaluationId">ID của đánh giá cần cập nhật</param>
    /// <param name="dto">Thông tin đánh giá mới</param>
    /// <returns>Đánh giá đã được cập nhật hoặc null nếu không tìm thấy</returns>
    Task<ActivityMemberEvaluationDto?> UpdateEvaluationAsync(int evaluatorId, int evaluationId, CreateActivityMemberEvaluationDto dto);

    /// <summary>
    /// Xem chi tiết đánh giá theo ID
    /// </summary>
    /// <param name="evaluationId">ID của đánh giá</param>
    /// <returns>Chi tiết đánh giá hoặc null nếu không tìm thấy</returns>
    Task<ActivityMemberEvaluationDto?> GetEvaluationByIdAsync(int evaluationId);

    /// <summary>
    /// Xem đánh giá theo assignment ID
    /// </summary>
    /// <param name="assignmentId">ID của assignment</param>
    /// <returns>Đánh giá hoặc null nếu chưa có đánh giá</returns>
    Task<ActivityMemberEvaluationDto?> GetEvaluationByAssignmentIdAsync(int assignmentId);

    /// <summary>
    /// Lấy báo cáo tổng hợp đánh giá của activity
    /// </summary>
    /// <param name="activityId">ID của activity</param>
    /// <returns>Báo cáo tổng hợp với các chỉ số thống kê</returns>
    Task<ActivityEvaluationReportDto> GetActivityEvaluationReportAsync(int activityId);

    /// <summary>
    /// Lấy lịch sử đánh giá của thành viên
    /// </summary>
    /// <param name="userId">ID của thành viên</param>
    /// <returns>Lịch sử đánh giá với điểm trung bình tổng hợp</returns>
    Task<MemberEvaluationHistoryDto> GetMemberEvaluationHistoryAsync(int userId);

    /// <summary>
    /// Xem đánh giá của chính mình
    /// </summary>
    /// <param name="userId">ID của user hiện tại</param>
    /// <returns>Danh sách đánh giá của user</returns>
    Task<List<ActivityMemberEvaluationDto>> GetMyEvaluationsAsync(int userId);

    /// <summary>
    /// Kiểm tra user có quyền đánh giá activity không
    /// </summary>
    /// <param name="evaluatorId">ID của người đánh giá</param>
    /// <param name="activityId">ID của activity</param>
    /// <returns>True nếu có quyền, False nếu không</returns>
    Task<bool> CanEvaluateAsync(int evaluatorId, int activityId);
}
