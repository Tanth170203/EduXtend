using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFE.Pages
{
    public class FeaturedClub
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Members { get; set; }
        public string Image { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class UpcomingActivity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Points { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }

    public class StatItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public List<FeaturedClub> FeaturedClubs { get; set; } = new();
        public List<UpcomingActivity> UpcomingActivities { get; set; } = new();
        public List<StatItem> Stats { get; set; } = new();

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                // Fetch real clubs data
                var clubsResponse = await client.GetFromJsonAsync<List<BusinessObject.DTOs.Club.ClubListItemDto>>("api/club");
                if (clubsResponse != null && clubsResponse.Any())
                {
                    FeaturedClubs = clubsResponse
                        .Where(c => c.IsActive)
                        .OrderByDescending(c => c.MemberCount)
                        .Take(3)
                        .Select(c => new FeaturedClub
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Description = c.Description ?? "Join us to explore and develop your skills",
                            Members = c.MemberCount,
                            Image = c.LogoUrl ?? "https://via.placeholder.com/400x225/003366/FFFFFF?text=" + Uri.EscapeDataString(c.Name),
                            Category = c.CategoryName ?? "General"
                        })
                        .ToList();
                }

                // Fetch all activities (Approved and Completed)
                var allActivitiesResponse = await client.GetFromJsonAsync<List<BusinessObject.DTOs.Activity.ActivityListItemDto>>("api/activity");
                
                // Filter for upcoming activities display
                if (allActivitiesResponse != null && allActivitiesResponse.Any())
                {
                    UpcomingActivities = allActivitiesResponse
                        .Where(a => (a.Status == "Approved" || a.Status == "Completed") && a.StartTime >= DateTime.Now)
                        .OrderBy(a => a.StartTime)
                        .Take(3)
                        .Select(a => new UpcomingActivity
                        {
                            Id = a.Id,
                            Title = a.Title,
                            Date = a.StartTime.ToString("dd/MM/yyyy"),
                            Location = a.Location ?? "TBA",
                            Points = (int)a.MovementPoint,
                            Type = a.Type,
                            Image = a.ImageUrl ?? "https://via.placeholder.com/400x225/007ACC/FFFFFF?text=" + Uri.EscapeDataString(a.Title)
                        })
                        .ToList();
                }

                // Calculate real stats
                var totalClubs = clubsResponse?.Count(c => c.IsActive) ?? 0;
                var totalActivities = allActivitiesResponse?.Count(a => a.Status == "Approved" || a.Status == "Completed") ?? 0;
                var totalMembers = clubsResponse?.Sum(c => c.MemberCount) ?? 0;

                _logger.LogInformation($"Stats - Clubs: {totalClubs}, Activities: {totalActivities}, Members: {totalMembers}");

                Stats = new List<StatItem>
                {
                    new StatItem { Label = "Clubs", Value = totalClubs.ToString(), Icon = "users" },
                    new StatItem { Label = "Activities", Value = totalActivities.ToString(), Icon = "calendar" },
                    new StatItem { Label = "Members", Value = totalMembers.ToString(), Icon = "trending-up" },
                    new StatItem { Label = "Points", Value = "100", Icon = "award" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading homepage data");
                
                // Fallback to empty lists if API fails
                Stats = new List<StatItem>
                {
                    new StatItem { Label = "Clubs", Value = "0", Icon = "users" },
                    new StatItem { Label = "Activities", Value = "0", Icon = "calendar" },
                    new StatItem { Label = "Members", Value = "0", Icon = "trending-up" },
                    new StatItem { Label = "Points", Value = "0", Icon = "award" }
                };
            }
        }
    }
}
