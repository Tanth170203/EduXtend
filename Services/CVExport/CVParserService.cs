using BusinessObject.DTOs.CVExport;
using Microsoft.Extensions.Logging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using System.Text.RegularExpressions;

namespace Services.CVExport
{
    public class CVParserService : ICVParserService
    {
        private readonly ILogger<CVParserService> _logger;

        public CVParserService(ILogger<CVParserService> logger)
        {
            _logger = logger;
        }

        public async Task<ExtractedCVDataDto> ParseCVAsync(byte[] fileData, string extension, int joinRequestId)
        {
            try
            {
                _logger.LogInformation("Parsing CV for JoinRequest {Id}, Extension: {Ext}", joinRequestId, extension);

                // Extract text from file
                var text = ExtractText(fileData, extension);

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("No text extracted from CV for JoinRequest {Id}", joinRequestId);
                    return new ExtractedCVDataDto
                    {
                        JoinRequestId = joinRequestId,
                        ParseSuccess = false,
                        ParseError = "Could not extract text from CV file"
                    };
                }

                // Extract structured data
                var result = ExtractStructuredData(text, joinRequestId);
                result.ParseSuccess = true;

                _logger.LogInformation("Successfully parsed CV for JoinRequest {Id}", joinRequestId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CV for JoinRequest {Id}", joinRequestId);
                return new ExtractedCVDataDto
                {
                    JoinRequestId = joinRequestId,
                    ParseSuccess = false,
                    ParseError = $"Error parsing CV: {ex.Message}"
                };
            }
        }

        public string ExtractText(byte[] fileData, string extension)
        {
            try
            {
                return extension.ToLowerInvariant() switch
                {
                    ".pdf" => ExtractTextFromPdf(fileData),
                    ".docx" => ExtractTextFromDocx(fileData),
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from {Extension} file", extension);
                return string.Empty;
            }
        }

        private string ExtractTextFromPdf(byte[] fileData)
        {
            try
            {
                using var memoryStream = new MemoryStream(fileData);
                using var pdfReader = new PdfReader(memoryStream);
                using var pdfDocument = new PdfDocument(pdfReader);

                var text = new StringBuilder();
                var numberOfPages = pdfDocument.GetNumberOfPages();

                _logger.LogInformation("Extracting text from PDF with {Pages} pages", numberOfPages);

                for (int i = 1; i <= numberOfPages; i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    text.AppendLine(pageText);
                }

                var extractedText = text.ToString();
                _logger.LogInformation("Extracted {Length} characters from PDF", extractedText.Length);
                
                // Check if we actually got any text (not just whitespace)
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning("PDF appears to be image-based (no text extracted). May need OCR.");
                    throw new Exception("PDF contains no extractable text. It may be an image-based/scanned PDF that requires OCR.");
                }

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                throw;
            }
        }

        private string ExtractTextFromDocx(byte[] fileData)
        {
            try
            {
                using var memoryStream = new MemoryStream(fileData);
                using var wordDocument = WordprocessingDocument.Open(memoryStream, false);

                var body = wordDocument.MainDocumentPart?.Document.Body;
                if (body == null)
                {
                    _logger.LogWarning("DOCX document has no body");
                    return string.Empty;
                }

                var text = new StringBuilder();
                
                // Extract text from paragraphs
                foreach (var paragraph in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }

                // Extract text from tables
                foreach (var table in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Table>())
                {
                    foreach (var row in table.Descendants<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
                    {
                        foreach (var cell in row.Descendants<DocumentFormat.OpenXml.Wordprocessing.TableCell>())
                        {
                            text.Append(cell.InnerText + " ");
                        }
                        text.AppendLine();
                    }
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from DOCX");
                throw;
            }
        }

        public ExtractedCVDataDto ExtractStructuredData(string text, int joinRequestId)
        {
            var result = new ExtractedCVDataDto
            {
                JoinRequestId = joinRequestId
            };

            // Extract personal information
            result.FullName = ExtractName(text);
            result.Email = ExtractEmail(text);
            result.PhoneNumber = ExtractPhoneNumber(text);

            // Extract sections
            result.Education = ExtractSection(text, new[] { "education", "học vấn", "trình độ học vấn", "quá trình học tập", "formation" });
            result.Experience = ExtractSection(text, new[] { "experience", "kinh nghiệm", "kinh nghiệm làm việc", "hoạt động", "experiences" });
            result.Skills = ExtractSection(text, new[] { "skills", "kỹ năng", "năng lực" });

            // Extract other information (everything not in the main sections)
            result.OtherInformation = ExtractOtherInformation(text, result);

            return result;
        }

        private string ExtractOtherInformation(string text, ExtractedCVDataDto extractedData)
        {
            try
            {
                // Remove already extracted information from the text
                var remainingText = text;

                // Remove personal info
                if (!string.IsNullOrEmpty(extractedData.FullName))
                    remainingText = remainingText.Replace(extractedData.FullName, "");
                if (!string.IsNullOrEmpty(extractedData.Email))
                    remainingText = remainingText.Replace(extractedData.Email, "");
                if (!string.IsNullOrEmpty(extractedData.PhoneNumber))
                    remainingText = remainingText.Replace(extractedData.PhoneNumber, "");

                // Remove extracted sections
                if (!string.IsNullOrEmpty(extractedData.Education))
                    remainingText = remainingText.Replace(extractedData.Education, "");
                if (!string.IsNullOrEmpty(extractedData.Experience))
                    remainingText = remainingText.Replace(extractedData.Experience, "");
                if (!string.IsNullOrEmpty(extractedData.Skills))
                    remainingText = remainingText.Replace(extractedData.Skills, "");

                // Remove common CV headers and empty lines
                var lines = remainingText.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Where(l => !l.ToLower().Contains("curriculum vitae"))
                    .Where(l => !l.ToLower().Contains("resume"))
                    .Where(l => l.Length > 2) // Remove very short lines
                    .ToList();

                // Join remaining lines
                var otherInfo = string.Join("\n", lines);

                // Limit length to avoid too much data
                if (otherInfo.Length > 1000)
                {
                    otherInfo = otherInfo.Substring(0, 1000) + "...";
                }

                return otherInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting other information");
                return string.Empty;
            }
        }

        private string ExtractName(string text)
        {
            try
            {
                // Name is usually in the first few lines and in uppercase or title case
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Take(10)
                    .ToList();

                foreach (var line in lines)
                {
                    // Skip common headers
                    if (line.ToLower().Contains("curriculum vitae") ||
                        line.ToLower().Contains("resume") ||
                        line.ToLower().Contains("cv") ||
                        line.Length < 3)
                        continue;

                    // Check if line looks like a name (2-5 words, mostly letters)
                    var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length >= 2 && words.Length <= 5)
                    {
                        var isName = words.All(w => w.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)));
                        if (isName && line.Length <= 50)
                        {
                            return line;
                        }
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting name");
                return string.Empty;
            }
        }

        private string ExtractEmail(string text)
        {
            try
            {
                // Email regex pattern
                var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
                var match = Regex.Match(text, emailPattern);
                return match.Success ? match.Value : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting email");
                return string.Empty;
            }
        }

        private string ExtractPhoneNumber(string text)
        {
            try
            {
                // Phone number patterns (Vietnamese and international)
                var patterns = new[]
                {
                    @"\b\+84\s?0?\d{9}\b",                  // +84 [0]xxxxxxxxx (VN with optional 0)
                    @"\b84\s?0?\d{9}\b",                    // 84 [0]xxxxxxxxx (VN with optional 0)
                    @"\b0\d{9}\b",                          // 0xxxxxxxxx (10 digits - VN)
                    @"\b0\d{2}[-.\s]?\d{3}[-.\s]?\d{4}\b", // 0xx-xxx-xxxx (VN with separators)
                    @"\b\d{3}[-.\s]?\d{3}[-.\s]?\d{4}\b",  // xxx-xxx-xxxx (US/International)
                    @"\b\d{10}\b",                          // xxxxxxxxxx (10 digits - any)
                    @"\+\d{1,3}\s?\d{6,14}\b"              // +x xxx... (international with country code)
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(text, pattern);
                    if (match.Success)
                    {
                        var phone = match.Value.Trim();
                        
                        // Normalize: remove space between country code and number
                        // 84 0832825424 -> 840832825424 or format it properly
                        if (phone.StartsWith("84 ") || phone.StartsWith("+84 "))
                        {
                            phone = phone.Replace(" ", "");
                        }
                        
                        return phone;
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting phone number");
                return string.Empty;
            }
        }

        private string ExtractSection(string text, string[] keywords)
        {
            try
            {
                var lines = text.Split('\n');
                var sectionLines = new List<string>();
                bool inSection = false;
                int emptyLineCount = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();

                    // Check if this line is a section header
                    if (!inSection && keywords.Any(k => line.ToLower().Contains(k)))
                    {
                        inSection = true;
                        continue; // Skip the header itself
                    }

                    if (inSection)
                    {
                        // Stop if we hit another section header
                        var otherSectionKeywords = new[]
                        {
                            "education", "học vấn", "experience", "kinh nghiệm",
                            "skills", "kỹ năng", "references", "tham khảo",
                            "certifications", "chứng chỉ", "awards", "giải thưởng",
                            "projects", "dự án", "contact", "liên hệ"
                        };

                        if (otherSectionKeywords.Any(k => line.ToLower().StartsWith(k)) &&
                            !keywords.Any(k => line.ToLower().Contains(k)))
                        {
                            break;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            emptyLineCount++;
                            // Stop after 2 consecutive empty lines
                            if (emptyLineCount >= 2)
                                break;
                        }
                        else
                        {
                            emptyLineCount = 0;
                            sectionLines.Add(line);
                        }
                    }
                }

                return sectionLines.Any() ? string.Join("\n", sectionLines) : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting section with keywords: {Keywords}", string.Join(", ", keywords));
                return string.Empty;
            }
        }
    }
}
