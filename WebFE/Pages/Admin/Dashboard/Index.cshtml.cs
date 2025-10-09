using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.Admin.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Dashboard Statistics
        public int TotalStudents { get; set; }
        public int ActiveClubs { get; set; }
        public int MonthlyActivities { get; set; }
        public int PendingProposals { get; set; }

        // Recent Activities
        public List<RecentActivityDto> RecentActivities { get; set; } = new();

        public IActionResult OnGet()
        {
            try
            {
                // TODO: Load actual data from API
                // For now, using placeholder data
                TotalStudents = 1250;
                ActiveClubs = 24;
                MonthlyActivities = 45;
                PendingProposals = 8;

                // Load recent activities (placeholder)
                RecentActivities = new List<RecentActivityDto>
                {
                    new RecentActivityDto
                    {
                        Name = "Hội thảo Công nghệ AI 2025",
                        ClubName = "CLB Lập trình",
                        StartDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new RecentActivityDto
                    {
                        Name = "Giải bóng đá sinh viên",
                        ClubName = "CLB Thể thao",
                        StartDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new RecentActivityDto
                    {
                        Name = "Workshop Thiết kế UI/UX",
                        ClubName = "CLB Thiết kế",
                        StartDate = DateTime.UtcNow.AddDays(-7)
                    }
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return Page();
            }
        }
    }

    public class RecentActivityDto
    {
        public string Name { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
    }
}