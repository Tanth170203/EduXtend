using BusinessObject.DTOs.Semester;

namespace WebFE.Extensions
{
    /// <summary>
    /// Extension methods cho SemesterDto để tính toán các thuộc tính cho View
    /// </summary>
    public static class SemesterExtensions
    {
        /// <summary>
        /// Tính trạng thái của học kỳ dựa trên ngày hiện tại
        /// </summary>
        public static string GetStatus(this SemesterDto semester)
        {
            var now = DateTime.Now.Date;
            var start = semester.StartDate.Date;
            var end = semester.EndDate.Date;

            if (now < start)
                return "upcoming";      // Chưa đến ngày bắt đầu
            else if (now >= start && now <= end)
                return "active";        // Đang trong khoảng thời gian
            else
                return "completed";     // Đã qua ngày kết thúc
        }

        /// <summary>
        /// Lấy label hiển thị cho trạng thái
        /// </summary>
        public static string GetStatusLabel(this SemesterDto semester)
        {
            return semester.GetStatus() switch
            {
                "active" => "Đang diễn ra",
                "upcoming" => "Sắp tới",
                "completed" => "Đã kết thúc",
                _ => "Không xác định"
            };
        }

        /// <summary>
        /// Lấy CSS class cho badge trạng thái
        /// </summary>
        public static string GetStatusBadgeClass(this SemesterDto semester)
        {
            return semester.GetStatus() switch
            {
                "active" => "bg-success",
                "upcoming" => "bg-info",
                "completed" => "bg-secondary",
                _ => "bg-secondary"
            };
        }
    }
}

