using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Chatbot;
using Services.Chatbot.Models;

namespace Services.Activities
{
    public class ActivityExtractorService : IActivityExtractorService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiAIOptions _options;
        private readonly ILogger<ActivityExtractorService> _logger;
        
        private static readonly string[] SupportedImageTypes = { "image/jpeg", "image/png", "image/jpg" };
        private static readonly string[] SupportedTextTypes = { "text/plain" };
        private static readonly string[] SupportedDocTypes = { 
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        public ActivityExtractorService(
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiAIOptions> options,
            ILogger<ActivityExtractorService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ExtractedActivityDto> ExtractActivityFromFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            var contentType = file.ContentType.ToLower();
            var extension = Path.GetExtension(file.FileName).ToLower();
            
            _logger.LogInformation("Processing file: {FileName}, ContentType: {ContentType}, Size: {Size}",
                file.FileName, contentType, file.Length);

            string extractedText;
            
            // Handle based on file type
            if (SupportedImageTypes.Contains(contentType) || IsImageExtension(extension))
            {
                extractedText = await ExtractFromImageAsync(file);
            }
            else if (SupportedTextTypes.Contains(contentType) || extension == ".txt")
            {
                extractedText = await ExtractFromTextFileAsync(file);
            }
            else if (contentType == "application/pdf" || extension == ".pdf")
            {
                extractedText = await ExtractFromPdfAsync(file);
            }
            else if (contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" 
                     || extension == ".docx")
            {
                extractedText = await ExtractFromDocxAsync(file);
            }
            else
            {
                throw new NotSupportedException($"File type '{contentType}' is not supported. Supported types: TXT, JPG, PNG, PDF, DOCX");
            }

            // Parse extracted text to activity DTO
            return await ParseToActivityDtoAsync(extractedText);
        }

        private bool IsImageExtension(string extension)
        {
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png";
        }

        private async Task<string> ExtractFromTextFileAsync(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private async Task<string> ExtractFromImageAsync(IFormFile file)
        {
            // Convert image to base64 and use Gemini Vision
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var base64Data = Convert.ToBase64String(memoryStream.ToArray());
            
            var mimeType = file.ContentType;
            if (string.IsNullOrEmpty(mimeType) || mimeType == "application/octet-stream")
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                mimeType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => "image/jpeg"
                };
            }

            return await CallGeminiVisionAsync(base64Data, mimeType);
        }

        private async Task<string> ExtractFromPdfAsync(IFormFile file)
        {
            // For PDF, we'll extract text using a simple approach
            // In production, consider using a library like PdfPig or iTextSharp
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            
            // Try to extract text from PDF
            var pdfText = ExtractTextFromPdf(memoryStream.ToArray());
            
            if (!string.IsNullOrWhiteSpace(pdfText))
            {
                return pdfText;
            }
            
            // If text extraction fails, convert first page to image and use vision
            // For now, return a message asking user to use image or text file
            throw new NotSupportedException("Could not extract text from PDF. Please try uploading as an image or text file.");
        }

        private string ExtractTextFromPdf(byte[] pdfBytes)
        {
            // Simple PDF text extraction - looks for text streams
            // This is a basic implementation; for production use PdfPig or similar
            try
            {
                var content = Encoding.UTF8.GetString(pdfBytes);
                var textBuilder = new StringBuilder();
                
                // Look for text between BT and ET markers (PDF text objects)
                var matches = Regex.Matches(content, @"BT\s*(.*?)\s*ET", RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    var textContent = match.Groups[1].Value;
                    // Extract text from Tj and TJ operators
                    var tjMatches = Regex.Matches(textContent, @"\((.*?)\)\s*Tj", RegexOptions.Singleline);
                    foreach (Match tj in tjMatches)
                    {
                        textBuilder.AppendLine(tj.Groups[1].Value);
                    }
                }
                
                return textBuilder.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<string> ExtractFromDocxAsync(IFormFile file)
        {
            // Extract text from DOCX (which is a ZIP file with XML content)
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            try
            {
                using var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Read);
                var documentEntry = archive.GetEntry("word/document.xml");
                
                if (documentEntry == null)
                    throw new InvalidOperationException("Invalid DOCX file structure");
                
                using var entryStream = documentEntry.Open();
                using var reader = new StreamReader(entryStream);
                var xmlContent = await reader.ReadToEndAsync();
                
                // Extract text from XML, removing tags
                var text = Regex.Replace(xmlContent, @"<[^>]+>", " ");
                text = Regex.Replace(text, @"\s+", " ");
                return text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from DOCX");
                throw new InvalidOperationException("Could not read DOCX file. Please ensure it's a valid Word document.");
            }
        }

        private async Task<string> CallGeminiVisionAsync(string base64Image, string mimeType)
        {
            var httpClient = _httpClientFactory.CreateClient("GeminiAI");
            
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                text = @"Extract all activity/event information from this image. 
                                        Look for: title, description, location, date, time, number of participants.
                                        Return the extracted text in a structured format."
                            },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 2048
                }
            };

            var url = $"{_options.ApiBaseUrl}/models/{_options.ModelName}:generateContent?key={_options.ApiKey}";
            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini Vision API error: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"Failed to process image: {response.StatusCode}");
            }

            return ParseGeminiResponse(responseContent);
        }

        private string ParseGeminiResponse(string jsonResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var text))
                        {
                            return text.GetString() ?? string.Empty;
                        }
                    }
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini response");
                return string.Empty;
            }
        }

        private async Task<ExtractedActivityDto> ParseToActivityDtoAsync(string extractedText)
        {
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return new ExtractedActivityDto { RawExtractedText = extractedText };
            }

            // Use Gemini to parse the extracted text into structured data
            var prompt = $@"Analyze the following text and extract activity/event information.
Return ONLY a valid JSON object with these fields (use null for missing values):
{{
    ""title"": ""activity title"",
    ""description"": ""activity description"",
    ""location"": ""location/venue"",
    ""startTime"": ""YYYY-MM-DDTHH:mm:ss or null"",
    ""endTime"": ""YYYY-MM-DDTHH:mm:ss or null"",
    ""maxParticipants"": number or null,
    ""suggestedType"": ""one of: ClubMeeting, ClubTraining, ClubWorkshop, LargeEvent, MediumEvent, SmallEvent, SchoolCompetition, ProvincialCompetition, NationalCompetition, ClubCollaboration, SchoolCollaboration"",
    ""schedules"": [
        {{
            ""startTime"": ""HH:mm"",
            ""endTime"": ""HH:mm"",
            ""title"": ""schedule item title"",
            ""description"": ""optional description""
        }}
    ]
}}

Rules for suggestedType:
- LargeEvent: 100-200 participants
- MediumEvent: 50-100 participants  
- SmallEvent: less than 50 participants
- Competition types: if it mentions competition/contest
- ClubMeeting/Training/Workshop: internal club activities

Rules for schedules:
- Extract timeline/agenda/schedule items if present
- startTime and endTime should be in HH:mm format (24-hour)
- If no schedule/timeline found, set schedules to null or empty array
- Look for patterns like ""08:00 - 09:00: Registration"" or ""9h-10h: Opening""

Text to analyze:
{extractedText}

Return ONLY the JSON object, no markdown, no explanation.";

            var httpClient = _httpClientFactory.CreateClient("GeminiAI");
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 4096 // Increased to handle responses with many schedule items
                }
            };

            var url = $"{_options.ApiBaseUrl}/models/{_options.ModelName}:generateContent?key={_options.ApiKey}";
            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error during parsing: {Response}", responseContent);
                    return new ExtractedActivityDto { RawExtractedText = extractedText };
                }

                var geminiText = ParseGeminiResponse(responseContent);
                _logger.LogInformation("Gemini raw response: {Response}", geminiText);
                
                // Clean up the response - remove markdown code blocks if present
                geminiText = CleanJsonResponse(geminiText);
                _logger.LogInformation("Cleaned JSON: {Json}", geminiText);

                if (string.IsNullOrWhiteSpace(geminiText))
                {
                    _logger.LogWarning("Empty response from Gemini after cleaning");
                    return new ExtractedActivityDto { RawExtractedText = extractedText };
                }

                var result = JsonSerializer.Deserialize<ExtractedActivityDto>(geminiText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    result.RawExtractedText = extractedText;
                    return result;
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing failed. Will try manual extraction.");
                // Try manual extraction as fallback
                return ManualExtract(extractedText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse extracted text to DTO");
            }

            return new ExtractedActivityDto { RawExtractedText = extractedText };
        }

        private ExtractedActivityDto ManualExtract(string text)
        {
            // Simple manual extraction as fallback
            var result = new ExtractedActivityDto { RawExtractedText = text };
            
            try
            {
                var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
                var schedules = new List<ExtractedScheduleDto>();
                
                foreach (var line in lines)
                {
                    var lower = line.ToLower();
                    
                    // Try to extract title
                    if (lower.StartsWith("tên:") || lower.StartsWith("title:") || lower.StartsWith("tên hoạt động:"))
                    {
                        result.Title = line.Substring(line.IndexOf(':') + 1).Trim();
                    }
                    // Try to extract description
                    else if (lower.StartsWith("mô tả:") || lower.StartsWith("description:"))
                    {
                        result.Description = line.Substring(line.IndexOf(':') + 1).Trim();
                    }
                    // Try to extract location
                    else if (lower.StartsWith("địa điểm:") || lower.StartsWith("location:") || lower.StartsWith("nơi:"))
                    {
                        result.Location = line.Substring(line.IndexOf(':') + 1).Trim();
                    }
                    // Try to extract max participants - improved patterns
                    else if (lower.StartsWith("số lượng:") || lower.StartsWith("số người:") || 
                             lower.Contains("participants") || lower.Contains("tham gia:") ||
                             lower.Contains("số lượng tham gia") || lower.Contains("người tham gia"))
                    {
                        var numMatch = Regex.Match(line, @"\d+");
                        if (numMatch.Success && int.TryParse(numMatch.Value, out var num))
                        {
                            result.MaxParticipants = num;
                        }
                    }
                    // Try to extract schedule items - pattern: "HH:mm - HH:mm: Title" or "HH:mm-HH:mm: Title"
                    else
                    {
                        var scheduleMatch = Regex.Match(line, @"(\d{1,2}[:\.]?\d{2})\s*[-–]\s*(\d{1,2}[:\.]?\d{2})\s*[:\-–]?\s*(.+)");
                        if (scheduleMatch.Success)
                        {
                            var startTime = NormalizeTime(scheduleMatch.Groups[1].Value);
                            var endTime = NormalizeTime(scheduleMatch.Groups[2].Value);
                            var title = scheduleMatch.Groups[3].Value.Trim();
                            
                            if (!string.IsNullOrEmpty(title))
                            {
                                schedules.Add(new ExtractedScheduleDto
                                {
                                    StartTime = startTime,
                                    EndTime = endTime,
                                    Title = title
                                });
                            }
                        }
                    }
                }
                
                // If no title found, use first non-empty line
                if (string.IsNullOrEmpty(result.Title) && lines.Count > 0)
                {
                    result.Title = lines[0];
                }
                
                // Add schedules if found
                if (schedules.Count > 0)
                {
                    result.Schedules = schedules;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual extraction failed");
            }
            
            return result;
        }
        
        private string NormalizeTime(string time)
        {
            // Convert various time formats to HH:mm
            // e.g., "8:00", "08.00", "0800" -> "08:00"
            time = time.Replace(".", ":").Replace("h", ":");
            
            if (!time.Contains(":"))
            {
                // Handle format like "0800" -> "08:00"
                if (time.Length == 4)
                {
                    time = time.Substring(0, 2) + ":" + time.Substring(2);
                }
                else if (time.Length == 3)
                {
                    time = "0" + time.Substring(0, 1) + ":" + time.Substring(1);
                }
            }
            
            var parts = time.Split(':');
            if (parts.Length == 2)
            {
                var hour = parts[0].PadLeft(2, '0');
                var minute = parts[1].PadLeft(2, '0');
                return $"{hour}:{minute}";
            }
            
            return time;
        }

        private string CleanJsonResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Trim();
            
            // Remove markdown code blocks
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(7);
            else if (text.StartsWith("```"))
                text = text.Substring(3);
            
            if (text.EndsWith("```"))
                text = text.Substring(0, text.Length - 3);
            
            text = text.Trim();
            
            // Try to find JSON object in the text
            var startIndex = text.IndexOf('{');
            var endIndex = text.LastIndexOf('}');
            
            if (startIndex >= 0 && endIndex > startIndex)
            {
                text = text.Substring(startIndex, endIndex - startIndex + 1);
            }
            
            return text;
        }
    }
}
