# Implementation Plan - CV Bulk Export with Parsing

- [x] 1. Set up project structure and install dependencies



  - Add NuGet packages: iTextSharp (or PdfPig), DocumentFormat.OpenXml, Bogus (for testing)
  - Create folder structure: Services/CVExport, BusinessObject/DTOs/CVExport
  - _Requirements: All_

- [ ] 2. Create DTOs and interfaces
  - [x] 2.1 Create ExtractedCVDataDto, CVExportRequestDto, CVExportResultDto

    - Define all properties for extracted CV data
    - Include parse success/error tracking fields
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.2_
  
  - [x] 2.2 Create service interfaces

    - Define ICVExportService, ICVDownloaderService, ICVParserService, IExcelGeneratorService
    - Document method signatures and return types
    - _Requirements: 1.1, 1.2, 2.1, 3.1_

- [ ] 3. Implement CVDownloaderService
  - [x] 3.1 Implement file download logic


    - Use HttpClient to download files from URLs
    - Support PDF and DOCX formats
    - Implement timeout (30 seconds)
    - Validate file size (max 10MB)
    - _Requirements: 1.1, 1.2_
  

  - [ ] 3.2 Implement format validation
    - Check file extensions (.pdf, .docx)
    - Validate URLs to prevent SSRF
    - _Requirements: 1.2_

  
  - [ ] 3.3 Implement error handling
    - Log download failures
    - Continue processing on individual failures
    - _Requirements: 1.3_
  
  - [ ]* 3.4 Write property test for error resilience
    - **Property 3: Error resilience in download**
    - **Validates: Requirements 1.3**
  
  - [ ]* 3.5 Write property test for file format validation
    - **Property 2: File format validation**
    - **Validates: Requirements 1.2**



- [ ] 4. Implement CVParserService
  - [ ] 4.1 Implement PDF text extraction
    - Use iTextSharp or PdfPig to extract text from PDF

    - Handle multi-page PDFs
    - Handle encrypted/corrupted PDFs gracefully
    - _Requirements: 2.1, 7.1, 7.3_
  

  - [ ] 4.2 Implement DOCX text extraction
    - Use DocumentFormat.OpenXml to extract text from Word documents
    - Handle tables and complex layouts
    - _Requirements: 2.1, 7.2_
  

  - [ ] 4.3 Implement personal information extraction
    - Extract name using patterns (first lines, capitalization)
    - Extract email using regex pattern
    - Extract phone number using Vietnamese phone patterns

    - _Requirements: 2.1_
  
  - [ ] 4.4 Implement profile/objective extraction
    - Identify profile sections using keywords

    - Extract text from profile section
    - _Requirements: 2.2_
  
  - [x] 4.5 Implement education extraction

    - Identify education sections using keywords ("Học vấn", "Education")
    - Extract school, major, dates
    - _Requirements: 2.3_
  

  - [ ] 4.6 Implement experience extraction
    - Identify experience sections using keywords ("Kinh nghiệm", "Experience")
    - Extract position, organization, dates
    - _Requirements: 2.4_
  
  - [ ] 4.7 Implement skills extraction
    - Identify skills sections using keywords ("Kỹ năng", "Skills")
    - Extract skill list
    - _Requirements: 2.5_
  
  - [ ] 4.8 Implement error handling for unparseable CVs
    - Return empty fields for missing data
    - Log parsing errors with details
    - Mark parse success/failure
    - _Requirements: 2.6_
  
  - [ ]* 4.9 Write property test for personal information extraction
    - **Property 4: Personal information extraction**
    - **Validates: Requirements 2.1**


  
  - [ ]* 4.10 Write property test for missing data handling
    - **Property 5: Missing data handling**
    - **Validates: Requirements 2.6, 3.3**

  
  - [ ]* 4.11 Write unit tests for Vietnamese encoding
    - Test with sample text containing Vietnamese diacritics
    - Verify correct extraction and preservation
    - _Requirements: 7.5_

- [x] 5. Implement ExcelGeneratorService

  - [ ] 5.1 Implement Excel file generation
    - Use EPPlus to create workbook
    - Create worksheet with proper structure


    - Add all required columns: STT, Mã sinh viên, Họ tên, Email, SĐT, Profile, Học vấn, Kinh nghiệm, Kỹ năng, Link CV, Ngày nộp đơn
    - _Requirements: 3.1, 3.2_
  
  - [ ] 5.2 Implement Excel formatting
    - Format header row (bold, background color)
    - Auto-fit column widths
    - Enable text wrapping for long text columns
    - Create hyperlinks for CV URLs
    - Format dates as dd/MM/yyyy
    - _Requirements: 5.1, 5.2, 5.3, 5.5_
  
  - [ ] 5.3 Implement data sorting
    - Sort rows by submission date (newest first)
    - _Requirements: 5.4_
  
  - [ ] 5.4 Implement filename generation
    - Generate filename: "CV_Extracted_[ClubName]_[Date].xlsx"
    - Sanitize club name for filename
    - Use yyyyMMdd format for date
    - _Requirements: 3.4_
  
  - [ ]* 5.5 Write property test for one row per applicant
    - **Property 6: One row per applicant**
    - **Validates: Requirements 3.1**
  
  - [ ]* 5.6 Write property test for filename convention
    - **Property 7: Filename convention**
    - **Validates: Requirements 3.4**

  
  - [ ]* 5.7 Write property test for date sorting
    - **Property 9: Date sorting**
    - **Validates: Requirements 5.4**
  
  - [ ]* 5.8 Write property test for Vietnamese encoding preservation
    - **Property 11: Vietnamese encoding preservation**
    - **Validates: Requirements 7.5**
  
  - [ ]* 5.9 Write unit tests for Excel structure
    - Verify all required columns are present
    - Verify header formatting
    - Verify hyperlink creation
    - _Requirements: 3.2, 5.1, 5.2, 5.3_

- [ ] 6. Implement CVExportService (orchestrator)
  - [x] 6.1 Implement GetUnscheduledRequests method

    - Query join requests with status "Pending"
    - Filter out requests with existing interviews
    - Apply club authorization filter
    - Include related data (User, Student, Club)
    - _Requirements: 1.1, 4.1, 4.3_
  
  - [x] 6.2 Implement ExtractCVDataAsync method

    - Orchestrate download, parse, and data collection
    - Process CVs in parallel with degree of parallelism limit
    - Track progress and errors
    - Return CVExportResultDto with all extracted data
    - _Requirements: 1.4, 6.3, 6.4_
  
  - [x] 6.3 Implement GenerateExcelAsync method

    - Call ExcelGeneratorService
    - Return byte array for download
    - _Requirements: 3.1, 3.4_
  
  - [x] 6.4 Implement authorization checks


    - Verify user is a club manager
    - Verify user manages the specified club
    - Return appropriate error for unauthorized access
    - _Requirements: 4.1, 4.2, 4.3_
  
  - [ ]* 6.5 Write property test for unscheduled request filtering
    - **Property 1: Unscheduled request filtering**
    - **Validates: Requirements 1.1**
  
  - [ ]* 6.6 Write property test for authorization filtering
    - **Property 8: Authorization filtering**
    - **Validates: Requirements 4.1, 4.3**
  
  - [ ]* 6.7 Write property test for error reporting completeness
    - **Property 10: Error reporting completeness**
    - **Validates: Requirements 6.4**

- [x] 7. Register services in dependency injection



  - Register all services in Program.cs or Startup.cs
  - Configure HttpClient for CVDownloaderService
  - _Requirements: All_

- [ ] 8. Create Web UI (Razor Page)
  - [x] 8.1 Add export button to join requests page

    - Add "Xuất CV chưa phỏng vấn" button
    - Show count badge of unscheduled requests
    - _Requirements: 8.1_
  

  - [-] 8.2 Implement API endpoint for CV export

    - Create POST endpoint /api/cv-export/extract
    - Accept clubId in request body
    - Return CVExportResultDto
    - _Requirements: 1.1, 6.1_

  
  - [ ] 8.3 Implement API endpoint for Excel download
    - Create POST endpoint /api/cv-export/download
    - Accept extracted data in request body
    - Return Excel file as byte array
    - Set proper content-type and filename headers

    - _Requirements: 3.4_
  
  - [ ] 8.4 Implement loading indicator
    - Show modal with progress bar during processing

    - Update progress as CVs are processed
    - _Requirements: 6.1, 6.2_
  
  - [ ] 8.5 Implement preview table
    - Display extracted data in table format

    - Show empty cells for missing data
    - Highlight failed parses
    - _Requirements: 8.1, 8.2, 8.3_
  

  - [ ] 8.6 Implement download button
    - Add "Tải xuống Excel" button in preview
    - Trigger file download on click
    - _Requirements: 8.4_

  
  - [ ] 8.7 Implement error display
    - Show warning message for failed CVs
    - List failed CV URLs

    - _Requirements: 6.4, 6.5_
  
  - [ ] 8.8 Implement cancel/retry functionality
    - Add cancel button during processing
    - Add retry button on failure
    - _Requirements: 8.5_

- [ ] 9. Add authorization middleware
  - Verify user has ClubManager role

  - Verify user manages the specified club
  - Return 403 for unauthorized requests
  - _Requirements: 4.1, 4.2_


- [ ] 10. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Handle edge cases
  - [ ] 11.1 Handle empty request list
    - Show appropriate message when no unscheduled requests
    - Don't generate Excel file
    - _Requirements: 3.5_
  


  - [ ] 11.2 Handle multi-club managers
    - Add club selector dropdown if user manages multiple clubs
    - _Requirements: 4.4_
  
  - [ ]* 11.3 Write unit tests for edge cases
    - Test empty request list
    - Test unauthorized access
    - Test corrupted files
    - _Requirements: 3.5, 4.2_

- [ ] 12. Add logging and monitoring
  - Log all download attempts and results
  - Log all parsing attempts and results
  - Log Excel generation
  - Log authorization checks
  - Include timing information for performance monitoring
  - _Requirements: 1.3, 2.6, 6.5_

- [ ] 13. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
