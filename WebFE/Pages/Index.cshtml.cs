using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public List<FeaturedClub> FeaturedClubs { get; set; } = new();
        public List<UpcomingActivity> UpcomingActivities { get; set; } = new();
        public List<StatItem> Stats { get; set; } = new();

        public void OnGet()
        {
            // Initialize stats
            Stats = new List<StatItem>
            {
                new StatItem { Label = "Clubs", Value = "48+", Icon = "users" },
                new StatItem { Label = "Activities/Month", Value = "120+", Icon = "calendar" },
                new StatItem { Label = "Participating Students", Value = "5,000+", Icon = "trending-up" },
                new StatItem { Label = "Average Score", Value = "85", Icon = "award" }
            };

            // Initialize featured clubs
            FeaturedClubs = new List<FeaturedClub>
            {
                new FeaturedClub
                {
                    Id = 1,
                    Name = "Technology Club",
                    Description = "Explore and develop programming, AI, and new technology skills",
                    Members = 245,
                    Image = "https://images.unsplash.com/photo-1707301280380-56f7e7a00aef?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=M3w3Nzg4Nzd8MHwxfHNlYXJjaHwxfHxidXNpbmVzcyUyMG1lZXRpbmclMjB0ZWFtd29ya3xlbnwxfHx8fDE3NTk1ODYzNzl8MA&ixlib=rb-4.1.0&q=80&w=1080",
                    Category = "Academic"
                },
                new FeaturedClub
                {
                    Id = 2,
                    Name = "Arts Club",
                    Description = "Create and express artistic passion through various forms",
                    Members = 189,
                    Image = "https://images.unsplash.com/photo-1700087209989-5a83d1a7c484?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=M3w3Nzg4Nzd8MHwxfHNlYXJjaHwxfHxtdXNpYyUyMHBlcmZvcm1hbmNlJTIwY29uY2VydHxlbnwxfHx8fDE3NTk1ODYzODB8MA&ixlib=rb-4.1.0&q=80&w=1080",
                    Category = "Cultural"
                },
                new FeaturedClub
                {
                    Id = 3,
                    Name = "Sports Club",
                    Description = "Build health and team spirit through various sports",
                    Members = 312,
                    Image = "https://images.unsplash.com/photo-1526646560565-39e6d9fe9e58?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=M3w3Nzg4Nzd8MHwxfHNlYXJjaHwxfHxzcG9ydHMlMjB0ZWFtJTIwY29sbGVnZXxlbnwxfHx8fDE3NTk1ODYzODB8MA&ixlib=rb-4.1.0&q=80&w=1080",
                    Category = "Sports"
                }
            };

            // Initialize upcoming activities
            UpcomingActivities = new List<UpcomingActivity>
            {
                new UpcomingActivity
                {
                    Id = 1,
                    Title = "Hackathon 2025",
                    Date = "15/10/2025",
                    Location = "Hall A",
                    Points = 20,
                    Type = "Academic",
                    Image = "https://images.unsplash.com/photo-1557734864-c78b6dfef1b1?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=M3w3Nzg4Nzd8MHwxfHNlYXJjaHwxfHxzdHVkZW50JTIwY2x1YiUyMGFjdGl2aXRpZXN8ZW58MXx8fHwxNzU5NTg2Mzc4fDA&ixlib=rb-4.1.0&q=80&w=1080"
                },
                new UpcomingActivity
                {
                    Id = 2,
                    Title = "Cultural Festival",
                    Date = "20/10/2025",
                    Location = "School Yard",
                    Points = 15,
                    Type = "Cultural",
                    Image = "https://images.unsplash.com/photo-1589872880544-76e896b0592c?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=M3w3Nzg4Nzd8MHwxfHNlYXJjaHwxfHxzdHVkeSUyMGdyb3VwJTIwbGlicmFyeXxlbnwxfHx8fDE3NTk1ODYzNzl8MA&ixlib=rb-4.1.0&q=80&w=1080"
                },
                new UpcomingActivity
                {
                    Id = 3,
                    Title = "Faculty Football Tournament",
                    Date = "25/10/2025",
                    Location = "Stadium",
                    Points = 10,
                    Type = "Sports",
                    Image = "https://images.unsplash.com/photo-1526646560565-39e6d9fe9e58?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=M3w3Nzg4Nzd8MHwxfHNlYXJjaHwxfHxzcG9ydHMlMjB0ZWFtJTIwY29sbGVnZXxlbnwxfHx8fDE3NTk1ODYzODB8MA&ixlib=rb-4.1.0&q=80&w=1080"
                }
            };
        }
    }
}
