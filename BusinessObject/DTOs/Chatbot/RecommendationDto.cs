using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Chatbot
{
    /// <summary>
    /// Represents a single recommendation (club or activity) from the AI chatbot
    /// </summary>
    public class RecommendationDto
    {
        /// <summary>
        /// The ID of the recommended club or activity
        /// </summary>
        /// <example>42</example>
        public int Id { get; set; }

        /// <summary>
        /// Name of the recommended club or activity
        /// </summary>
        /// <example>Câu lạc bộ Lập trình</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of recommendation: "club" or "activity"
        /// </summary>
        /// <example>club</example>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Description of the club or activity
        /// </summary>
        /// <example>Câu lạc bộ dành cho sinh viên yêu thích lập trình và công nghệ</example>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Explanation of why this is recommended for the student
        /// </summary>
        /// <example>Phù hợp với chuyên ngành Công nghệ thông tin của bạn</example>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Relevance score indicating how well this recommendation matches the student's profile (0-100)
        /// </summary>
        /// <example>95</example>
        [Range(0, 100)]
        public int RelevanceScore { get; set; }
    }
}
