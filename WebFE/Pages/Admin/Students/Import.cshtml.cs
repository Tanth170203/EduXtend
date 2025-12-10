using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessObject.DTOs.ImportFile;
using Services.UserImport;
using ClosedXML.Excel;

namespace WebFE.Pages.Admin.Students
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly IUserImportService _importService;
        private readonly ILogger<ImportModel> _logger;

        public ImportModel(
            IUserImportService importService,
            ILogger<ImportModel> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        public ImportUsersResponse? ImportResult { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload";
                return Page();
            }

            // Validate file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size exceeds 5MB limit";
                return Page();
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                TempData["ErrorMessage"] = "Only Excel files (.xlsx, .xls) are accepted";
                return Page();
            }

            try
            {
                ImportResult = await _importService.ImportUsersFromExcelAsync(file);

                if (ImportResult.FailureCount == 0)
                {
                    TempData["SuccessMessage"] = $"Successfully imported {ImportResult.SuccessCount} students!";
                    return RedirectToPage("/Admin/Students/Index");
                }
                else
                {
                    // Show results on same page if there are errors
                    _logger.LogWarning("Import completed with {SuccessCount} successes and {FailureCount} failures", 
                        ImportResult.SuccessCount, ImportResult.FailureCount);
                }

                return Page();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid import file");
                TempData["ErrorMessage"] = $"Invalid file: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing students");
                TempData["ErrorMessage"] = $"An error occurred while importing: {ex.Message}";
                return Page();
            }
        }

        public IActionResult OnGetDownloadTemplate()
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Students");

                // Headers
                worksheet.Cell(1, 1).Value = "Email";
                worksheet.Cell(1, 2).Value = "Full Name";
                worksheet.Cell(1, 3).Value = "Phone Number";
                worksheet.Cell(1, 4).Value = "Roles";
                worksheet.Cell(1, 5).Value = "Is Active";
                worksheet.Cell(1, 6).Value = "Student Code";
                worksheet.Cell(1, 7).Value = "Cohort";
                worksheet.Cell(1, 8).Value = "Date of Birth";
                worksheet.Cell(1, 9).Value = "Gender";
                worksheet.Cell(1, 10).Value = "Enrollment Date";
                worksheet.Cell(1, 11).Value = "Major Code";
                worksheet.Cell(1, 12).Value = "Student Status";

                // Style header
                var headerRange = worksheet.Range(1, 1, 1, 12);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Example data row 1
                worksheet.Cell(2, 1).Value = "student1@fpt.edu.vn";
                worksheet.Cell(2, 2).Value = "Nguyen Van A";
                worksheet.Cell(2, 3).Value = "0901234567";
                worksheet.Cell(2, 4).Value = "Student";
                worksheet.Cell(2, 5).Value = "true";
                worksheet.Cell(2, 6).Value = "SE123456";
                worksheet.Cell(2, 7).Value = "K16";
                worksheet.Cell(2, 8).Value = "2000-01-01";
                worksheet.Cell(2, 9).Value = "Male";
                worksheet.Cell(2, 10).Value = "2020-09-01";
                worksheet.Cell(2, 11).Value = "SE";
                worksheet.Cell(2, 12).Value = "Active";

                // Example data row 2
                worksheet.Cell(3, 1).Value = "student2@fpt.edu.vn";
                worksheet.Cell(3, 2).Value = "Tran Thi B";
                worksheet.Cell(3, 3).Value = "0907654321";
                worksheet.Cell(3, 4).Value = "Student";
                worksheet.Cell(3, 5).Value = "true";
                worksheet.Cell(3, 6).Value = "SE123457";
                worksheet.Cell(3, 7).Value = "K16";
                worksheet.Cell(3, 8).Value = "2000-05-15";
                worksheet.Cell(3, 9).Value = "Female";
                worksheet.Cell(3, 10).Value = "2020-09-01";
                worksheet.Cell(3, 11).Value = "SE";
                worksheet.Cell(3, 12).Value = "Active";

                // Example data row 3
                worksheet.Cell(4, 1).Value = "student3@fpt.edu.vn";
                worksheet.Cell(4, 2).Value = "Le Van C";
                worksheet.Cell(4, 3).Value = "0909876543";
                worksheet.Cell(4, 4).Value = "Student";
                worksheet.Cell(4, 5).Value = "true";
                worksheet.Cell(4, 6).Value = "IA123458";
                worksheet.Cell(4, 7).Value = "K17";
                worksheet.Cell(4, 8).Value = "2001-03-20";
                worksheet.Cell(4, 9).Value = "Male";
                worksheet.Cell(4, 10).Value = "2021-09-01";
                worksheet.Cell(4, 11).Value = "IA";
                worksheet.Cell(4, 12).Value = "Active";

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Add instructions sheet
                var instructionsSheet = workbook.Worksheets.Add("Instructions");
                instructionsSheet.Cell(1, 1).Value = "IMPORT INSTRUCTIONS";
                instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
                instructionsSheet.Cell(1, 1).Style.Font.FontSize = 14;

                instructionsSheet.Cell(3, 1).Value = "Required Fields:";
                instructionsSheet.Cell(3, 1).Style.Font.Bold = true;
                instructionsSheet.Cell(4, 1).Value = "- Email (must be unique)";
                instructionsSheet.Cell(5, 1).Value = "- Full Name";
                instructionsSheet.Cell(6, 1).Value = "- Student Code (must be unique)";
                instructionsSheet.Cell(7, 1).Value = "- Cohort (e.g., K16, K17)";
                instructionsSheet.Cell(8, 1).Value = "- Major Code (must exist in system)";

                instructionsSheet.Cell(10, 1).Value = "Optional Fields:";
                instructionsSheet.Cell(10, 1).Style.Font.Bold = true;
                instructionsSheet.Cell(11, 1).Value = "- Phone Number";
                instructionsSheet.Cell(12, 1).Value = "- Roles (default: Student)";
                instructionsSheet.Cell(13, 1).Value = "- Is Active (true/false, default: true)";
                instructionsSheet.Cell(14, 1).Value = "- Date of Birth (format: YYYY-MM-DD)";
                instructionsSheet.Cell(15, 1).Value = "- Gender (Male/Female/Other)";
                instructionsSheet.Cell(16, 1).Value = "- Enrollment Date (format: YYYY-MM-DD)";
                instructionsSheet.Cell(17, 1).Value = "- Student Status (Active/Inactive/Graduated/Suspended)";

                instructionsSheet.Cell(19, 1).Value = "Notes:";
                instructionsSheet.Cell(19, 1).Style.Font.Bold = true;
                instructionsSheet.Cell(20, 1).Value = "- Do not modify the header row";
                instructionsSheet.Cell(21, 1).Value = "- Email and Student Code must be unique";
                instructionsSheet.Cell(22, 1).Value = "- Major Code must exist in the system";
                instructionsSheet.Cell(23, 1).Value = "- Maximum file size: 5MB";
                instructionsSheet.Cell(24, 1).Value = "- Supported formats: .xlsx, .xls";

                instructionsSheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Student_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating template");
                TempData["ErrorMessage"] = "Error generating template file";
                return RedirectToPage();
            }
        }
    }
}
