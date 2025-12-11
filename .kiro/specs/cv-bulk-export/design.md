# Design Document - CV Bulk Export with Parsing

## Overview

Tính năng này cho phép Club Manager tự động trích xuất thông tin từ các file CV (PDF/Word) của các đơn đăng ký chưa có lịch phỏng vấn và xuất ra file Excel tổng hợp. Hệ thống sẽ:

1. Lọc các đơn đăng ký có status "Pending" và chưa có Interview
2. Tải xuống các file CV từ URL đã lưu
3. Phân tích nội dung CV để trích xuất thông tin có cấu trúc
4. Tổng hợp tất cả thông tin vào file Excel với định dạng rõ ràng
5. Cung cấp preview và cho phép tải xuống

## Architecture

### High-Level Architecture

```
┌─────────────────┐
│   Web UI        │
│  (Razor Pages)  │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────┐
│  CVExportService            │
│  - GetUnscheduledRequests   │
│  - ExportCVsToExcel         │
│  - PreviewExtractedData     │
└────────┬────────────────────┘
         │
         ├──────────────┬──────────────┬────────────────┐
         ▼              ▼              ▼                ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│CVDownloader  │ │  CVParser    │ │ExcelGenerator│ │Authorization │
│Service       │ │  Service     │ │Service       │ │Service       │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
         │              │              │
         ▼              ▼              ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│HTTP Client   │ │PDF/DOCX      │ │EPPlus        │
│(Download)    │ │Libraries     │ │Library       │
└──────────────┘ └──────────────┘ └──────────────┘
```

### Component Responsibilities

1. **CVExportService**: Orchestrates the entire export process
2. **CVDownloaderService**: Downloads CV files from URLs
3. **CVParserService**: Parses PDF/DOCX files and extracts structured data
4. **ExcelGeneratorService**: Creates formatted Excel files
5. **AuthorizationService**: Validates club manager permissions

## Components and Interfaces

### 1. Data Transfer Objects (DTOs)

```csharp
namespace BusinessObject.DTOs.CVExport
{
    public class ExtractedCVDataDto
    {
        public int JoinRequestId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Profile { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public string Skills { get; set; }
        public string CvUrl { get; set; }
        public DateTime SubmittedDate { get; set; }
        public bool ParseSuccess { get; set; }
        public string ParseError { get; set; }
    }

    public class CVExportRequestDto
    {
        public int ClubId { get; set; }
        public int RequestedByUserId { get; set; }
    }

    public class CVExportResultDto
    {
        public int TotalRequests { get; set; }
        public int SuccessfullyParsed { get; set; }
        public int FailedToParse { get; set; }
        public List<ExtractedCVDataDto> ExtractedData { get; set; }
        public List<string> Errors { get; set; }
    }
}
```

### 2. Service Interfaces

```csharp
namespace Services.CVExport
{
    public interface ICVExportService
    {
        Task<CVExportResultDto> ExtractCVDataAsync(CVExportRequestDto request);
        Task<byte[]> GenerateExcelAsync(CVExportResultDto data, string clubName);
    }

    public interface ICVDownloaderService
    {
        Task<(byte[] fileData, string extension)?> DownloadCVAsync(string cvUrl);
        bool IsSupportedFormat(string url);
    }

    public interface ICVParserService
    {
        Task<ExtractedCVDataDto> ParseCVAsync(byte[] fileData, string extension, int joinRequestId);
        string ExtractText(byte[] fileData, string extension);
        ExtractedCVDataDto ExtractStructuredData(string text, int joinRequestId);
    }

    public interface IExcelGeneratorService
    {
        byte[] GenerateExcel(List<ExtractedCVDataDto> data, string clubName);
    }
}
```

## Data Models

### Existing Models Used

- **JoinRequest**: Contains CV URL, user info, club info, status
- **Interview**: Used to check if interview exists for a join request
- **User**: Contains student information
- **Student**: Contains detailed student information
- **Club**: Contains club name for file naming

### New Models

No new database models required. All data is transient and exported to Excel.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Unscheduled request filtering
*For any* set of join requests, the system should only include requests with status "Pending" AND no associated Interview record when filtering for unscheduled requests.
**Validates: Requirements 1.1**

### Property 2: File format validation
*For any* CV URL, the system should accept it for download if and only if it has a .pdf or .docx extension (case-insensitive).
**Validates: Requirements 1.2**

### Property 3: Error resilience in download
*For any* list of CV URLs where some are invalid, the system should continue processing valid URLs and log errors for invalid ones without stopping the entire process.
**Validates: Requirements 1.3**

### Property 4: Personal information extraction
*For any* CV file containing name, email, and phone number fields, the parser should extract at least one of these fields when they are present in standard formats.
**Validates: Requirements 2.1**

### Property 5: Missing data handling
*For any* CV file that cannot be parsed or is missing certain fields, the corresponding Excel cells should be empty (not null or error text).
**Validates: Requirements 2.6, 3.3**

### Property 6: One row per applicant
*For any* set of extracted CV data, the generated Excel file should contain exactly one row per applicant (excluding the header row).
**Validates: Requirements 3.1**

### Property 7: Filename convention
*For any* generated Excel file, the filename should match the pattern "CV_Extracted_[ClubName]_[Date].xlsx" where date is in yyyyMMdd format.
**Validates: Requirements 3.4**

### Property 8: Authorization filtering
*For any* club manager, the system should only return join requests for clubs where that user has manager permissions.
**Validates: Requirements 4.1, 4.3**

### Property 9: Date sorting
*For any* set of extracted CV data, the Excel rows should be sorted by submission date in descending order (newest first).
**Validates: Requirements 5.4**

### Property 10: Error reporting completeness
*For any* CV that fails to parse, the error should be included in the result's error list with the join request ID.
**Validates: Requirements 6.4**

### Property 11: Vietnamese encoding preservation
*For any* text containing Vietnamese characters with diacritics, the Excel output should preserve these characters correctly.
**Validates: Requirements 7.5**

## Error Handling

### Error Categories

1. **Authorization Errors**
   - User not a club manager
   - User doesn't manage the specified club
   - Return 403 Forbidden

2. **Download Errors**
   - CV URL is invalid or inaccessible
   - Network timeout
   - Log error, continue with other CVs

3. **Parsing Errors**
   - Corrupted file
   - Unsupported format
   - Encrypted PDF
   - Log error, mark as failed, continue

4. **Excel Generation Errors**
   - Out of memory (too many records)
   - Disk space issues
   - Return 500 with error message

### Error Logging

All errors should be logged with:
- Timestamp
- Join Request ID
- CV URL
- Error type and message
- Stack trace (for unexpected errors)

## Testing Strategy

### Unit Testing

Unit tests will cover:
- URL validation logic
- File extension detection
- Text extraction from sample files
- Excel column structure
- Filename generation
- Authorization checks

### Property-Based Testing

We will use **Bogus** library for data generation and standard xUnit for property-based testing patterns in C#.

Property-based tests will:
- Generate random sets of join requests with various statuses and interview states
- Generate random CV URLs with different extensions
- Generate random text content with Vietnamese characters
- Verify properties hold across all generated inputs
- Run minimum 100 iterations per property test

Each property-based test will be tagged with a comment referencing the design document:
```csharp
// Feature: cv-bulk-export, Property 1: Unscheduled request filtering
[Fact]
public async Task Property_UnscheduledRequestFiltering()
{
    // Test implementation
}
```

### Integration Testing

Integration tests will:
- Test the full pipeline from request to Excel generation
- Use sample PDF and DOCX files
- Verify Excel file structure and content
- Test with real database context (in-memory)

### Manual Testing

Manual testing will verify:
- UI responsiveness and loading indicators
- Preview functionality
- File download in browser
- Excel file opens correctly in Microsoft Excel/LibreOffice

## Implementation Notes

### CV Parsing Strategy

Since CV parsing is complex and CVs have varied formats, we will use a **pattern-based extraction** approach:

1. **Text Extraction**: Use libraries to extract raw text
   - PDF: iTextSharp or PdfPig
   - DOCX: DocumentFormat.OpenXml or NPOI

2. **Pattern Matching**: Use regex patterns to identify sections
   - Email: Standard email regex
   - Phone: Vietnamese phone number patterns
   - Sections: Keywords like "Học vấn", "Education", "Kinh nghiệm", "Experience", etc.

3. **Heuristics**: Use position and formatting clues
   - Name usually at top
   - Contact info near name
   - Section headers in bold or larger font

### Excel Generation

Use **EPPlus** library (already in project dependencies):
- Create workbook and worksheet
- Set header row with bold font and background color
- Auto-fit columns
- Add hyperlinks for CV URLs
- Enable text wrapping for long text columns
- Set date format to dd/MM/yyyy

### Performance Considerations

- Process CVs in parallel (use Task.WhenAll with degree of parallelism limit)
- Stream large Excel files instead of loading entirely in memory
- Implement timeout for CV downloads (30 seconds per file)
- Cache downloaded CVs temporarily to avoid re-downloading on retry

### Security Considerations

- Validate CV URLs to prevent SSRF attacks
- Limit file size for downloads (max 10MB per CV)
- Sanitize extracted text before inserting into Excel
- Verify user permissions before every operation
- Use HTTPS only for CV downloads

## UI/UX Flow

1. Club Manager navigates to "Đơn đăng ký" page
2. Sees button "Xuất CV chưa phỏng vấn" with count badge
3. Clicks button → Loading modal appears
4. Progress bar shows "Đang xử lý 5/10 CV..."
5. When complete → Preview table shows extracted data
6. Manager reviews data
7. Clicks "Tải xuống Excel" → File downloads
8. If errors → Warning message shows failed CVs

## Dependencies

### NuGet Packages Required

- **iTextSharp** or **PdfPig**: PDF text extraction
- **DocumentFormat.OpenXml**: DOCX text extraction
- **EPPlus**: Excel generation (already installed)
- **Bogus**: Test data generation for property-based tests

### Existing Services Used

- JoinRequestRepository
- InterviewRepository
- ClubRepository
- UserRepository
- ILogger

## Future Enhancements

1. **AI-Powered Parsing**: Use Azure Form Recognizer or OpenAI for better extraction
2. **Template Matching**: Learn from successfully parsed CVs
3. **Batch Processing**: Queue system for large numbers of CVs
4. **Export Formats**: Support CSV, JSON in addition to Excel
5. **Field Mapping**: Allow managers to customize which fields to extract
