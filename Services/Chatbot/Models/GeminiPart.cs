using System.Text.Json.Serialization;

namespace Services.Chatbot.Models
{
    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        
        [JsonPropertyName("inline_data")]
        public GeminiInlineData? InlineData { get; set; }
    }
    
    public class GeminiInlineData
    {
        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = string.Empty;
        
        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }
}
