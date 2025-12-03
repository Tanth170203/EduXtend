namespace BusinessObject.DTOs.Chatbot
{
    /// <summary>
    /// Represents a single recommendation (club or activity) from the AI chatbot
    /// </summary>
    public class RecommendationDto
    {
        /// <summary>
        /// Type of recommendation: "Club" or "Activity"
        /// </summary>
        /// <example>Club</example>
        public string Type { get; set; } = null!;
        
        /// <summary>
        /// The ID of the recommended club or activity
        /// </summary>
        /// <example>42</example>
        public int Id { get; set; }
        
        /// <summary>
        /// Name of the recommended club or activity
        /// </summary>
        /// <example>Câu lạc bộ Lập trình</example>
        public string Name { get; set; } = null!;
        
        /// <summary>
        /// Description of the club or activity
        /// </summary>
        /// <example>Câu lạc bộ dành cho sinh viên yêu thích lập trình và công nghệ</example>
        public string? Description { get; set; }
        
        /// <summary>
        /// Explanation of why this is recommended for the student
        /// </summary>
        /// <example>Phù hợp với chuyên ngành Công nghệ thông tin của bạn</example>
        public string Reason { get; set; } = null!;
        
        /// <summary>
        /// Confidence score of the recommendation (0.0 to 1.0)
        /// </summary>
        /// <example>0.85</example>
        public double ConfidenceScore { get; set; }
    }
}
