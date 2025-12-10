<<<<<<< HEAD
<<<<<<< HEAD
using System.Text.Json.Serialization;

=======
>>>>>>> c3efbe527c0562abdad920e99cb9009199d6a74b
=======
>>>>>>> c3efbe527c0562abdad920e99cb9009199d6a74b
namespace Services.Chatbot.Models
{
    public class GeminiPart
    {
<<<<<<< HEAD
<<<<<<< HEAD
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
=======
        public string Text { get; set; } = string.Empty;
>>>>>>> c3efbe527c0562abdad920e99cb9009199d6a74b
=======
        public string Text { get; set; } = string.Empty;
>>>>>>> c3efbe527c0562abdad920e99cb9009199d6a74b
    }
}
