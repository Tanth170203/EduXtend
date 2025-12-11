using BusinessObject.DTOs.CVExport;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Services.CVExport
{
    public class ExcelGeneratorService : IExcelGeneratorService
    {
        private readonly ILogger<ExcelGeneratorService> _logger;

        public ExcelGeneratorService(ILogger<ExcelGeneratorService> logger)
        {
            _logger = logger;
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public byte[] GenerateExcel(List<ExtractedCVDataDto> data, string clubName)
        {
            try
            {
                _logger.LogInformation("Generating Excel file for {Count} CVs, Club: {Club}", data.Count, clubName);

                // Sort by submitted date (newest first)
                var sortedData = data.OrderByDescending(d => d.SubmittedDate).ToList();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Danh sách CV");

                // Create header row
                CreateHeaderRow(worksheet);

                // Add data rows
                int row = 2;
                int stt = 1;
                foreach (var item in sortedData)
                {
                    AddDataRow(worksheet, row, stt, item);
                    row++;
                    stt++;
                }

                // Format worksheet
                FormatWorksheet(worksheet, sortedData.Count);

                _logger.LogInformation("Excel file generated successfully");
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel file");
                throw;
            }
        }

        private void CreateHeaderRow(ExcelWorksheet worksheet)
        {
            var headers = new[]
            {
                "STT",
                "Mã sinh viên",
                "Họ tên",
                "Email",
                "Số điện thoại",
                "Học vấn",
                "Kinh nghiệm",
                "Kỹ năng",
                "Các thông tin khác",
                "Link CV",
                "Ngày nộp đơn"
            };

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cells[1, col];
                cell.Value = headers[col - 1];
                
                // Format header
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196)); // Blue
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
        }

        private void AddDataRow(ExcelWorksheet worksheet, int row, int stt, ExtractedCVDataDto data)
        {
            worksheet.Cells[row, 1].Value = stt;
            worksheet.Cells[row, 2].Value = data.StudentCode;
            worksheet.Cells[row, 3].Value = data.FullName;
            worksheet.Cells[row, 4].Value = data.Email;
            worksheet.Cells[row, 5].Value = data.PhoneNumber;
            worksheet.Cells[row, 6].Value = data.Education;
            worksheet.Cells[row, 7].Value = data.Experience;
            worksheet.Cells[row, 8].Value = data.Skills;
            worksheet.Cells[row, 9].Value = data.OtherInformation;
            
            // Add hyperlink for CV URL
            if (!string.IsNullOrWhiteSpace(data.CvUrl))
            {
                var cell = worksheet.Cells[row, 10];
                cell.Hyperlink = new Uri(data.CvUrl, UriKind.Absolute);
                cell.Value = "Xem CV";
                cell.Style.Font.UnderLine = true;
                cell.Style.Font.Color.SetColor(Color.Blue);
            }
            
            // Format date
            worksheet.Cells[row, 11].Value = data.SubmittedDate;
            worksheet.Cells[row, 11].Style.Numberformat.Format = "dd/MM/yyyy";

            // Enable text wrapping for long text columns
            worksheet.Cells[row, 6].Style.WrapText = true;  // Education
            worksheet.Cells[row, 7].Style.WrapText = true;  // Experience
            worksheet.Cells[row, 8].Style.WrapText = true;  // Skills
            worksheet.Cells[row, 9].Style.WrapText = true;  // Other Information
        }

        private void FormatWorksheet(ExcelWorksheet worksheet, int dataRowCount)
        {
            // Auto-fit columns
            for (int col = 1; col <= 11; col++)
            {
                worksheet.Column(col).AutoFit();
                
                // Set minimum and maximum widths
                if (worksheet.Column(col).Width < 10)
                    worksheet.Column(col).Width = 10;
                
                if (worksheet.Column(col).Width > 50)
                    worksheet.Column(col).Width = 50;
            }

            // Set specific column widths for better readability
            worksheet.Column(1).Width = 8;   // STT
            worksheet.Column(2).Width = 15;  // Mã sinh viên
            worksheet.Column(3).Width = 25;  // Họ tên
            worksheet.Column(4).Width = 30;  // Email
            worksheet.Column(5).Width = 15;  // Số điện thoại
            worksheet.Column(6).Width = 40;  // Học vấn
            worksheet.Column(7).Width = 40;  // Kinh nghiệm
            worksheet.Column(8).Width = 30;  // Kỹ năng
            worksheet.Column(9).Width = 40;  // Các thông tin khác
            worksheet.Column(10).Width = 15; // Link CV
            worksheet.Column(11).Width = 15; // Ngày nộp đơn

            // Add borders to all cells
            var dataRange = worksheet.Cells[1, 1, dataRowCount + 1, 11];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // Center align STT and date columns
            worksheet.Cells[2, 1, dataRowCount + 1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[2, 11, dataRowCount + 1, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Freeze header row
            worksheet.View.FreezePanes(2, 1);
        }
    }
}
