using System.Text;
using ClosedXML.Excel;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly EduXtendContext _context;

    public ReportsController(EduXtendContext context)
    {
        _context = context;
    }

    [HttpGet("club-monthly")]
    public async Task<IActionResult> ExportClubMonthly([FromQuery] int semesterId)
    {
        // Build months list from semester
        var sem = await _context.Semesters.FindAsync(semesterId);
        if (sem == null) return NotFound("Semester not found");

        var months = new List<(int y, int m)>();
        var cursor = new DateTime(sem.StartDate.Year, sem.StartDate.Month, 1);
        var endCap = new DateTime(sem.EndDate.Year, sem.EndDate.Month, 1);
        while (cursor <= endCap)
        {
            months.Add((cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(1);
        }

        // Prepare workbook
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BÁO CÁO");

        // Header (simplified to match provided sheet)
        ws.Cell(1, 1).Value = "STT";
        ws.Cell(1, 2).Value = "Tên CLB";
        ws.Cell(1, 3).Value = "SINH HOẠT NỘI BỘ\n5 điểm/ tuần";
        ws.Cell(1, 4).Value = "TỔ CHỨC SỰ KIỆN\nLớn 20 / Nhỏ 15 / Nội bộ 5";
        ws.Cell(1, 5).Value = "PHỐI HỢP\nBTC 1-10 / Tham dự 1-3";
        ws.Cell(1, 6).Value = "GIẢI THI ĐẤU\nTỉnh/TP 20 / Quốc gia 30 / Khác 5-10";
        ws.Cell(1, 7).Value = "KẾ HOẠCH\nHoàn thành đúng hạn 10";
        ws.Cell(1, 8).Value = "Tổng";
        ws.Row(1).Style.Alignment.WrapText = true;

        int currentRow = 3; // leave a spacer line

        // For each month: write a subtable
        foreach (var (y, m) in months)
        {
            ws.Cell(currentRow, 1).Value = $"THÁNG {m}";
            ws.Range(currentRow, 1, currentRow, 8).Merge().Style.Font.SetBold();
            currentRow++;

            var clubs = await _context.Clubs
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            int idx = 1;
            foreach (var club in clubs)
            {
                // Aggregate scores from details within this month & semester
                var record = await _context.ClubMovementRecords
                    .Include(r => r.Details).ThenInclude(d => d.Criterion).ThenInclude(c => c.Group)
                    .FirstOrDefaultAsync(r => r.ClubId == club.Id && r.SemesterId == semesterId && r.Month == m);

                double sh = 0, sk = 0, ph = 0, thi = 0, kh = 0, total = 0;
                if (record != null)
                {
                    sh = record.Details.Where(d => d.Criterion != null && d.Criterion.Title.Contains("Sinh hoạt CLB")).Sum(d => d.Score);
                    sk = record.Details.Where(d => d.Criterion != null && d.Criterion.Title.Contains("Sự kiện")).Sum(d => d.Score);
                    ph = record.Details.Where(d => d.Criterion != null && d.Criterion.Title.Contains("Phối hợp")).Sum(d => d.Score);
                    thi = record.Details.Where(d => d.Criterion != null && (d.Criterion.Title.Contains("Tỉnh/TP") || d.Criterion.Title.Contains("Quốc gia") || d.Criterion.Title.Contains("cuộc thi"))).Sum(d => d.Score);
                    kh = record.Details.Where(d => d.Criterion != null && d.Criterion.Title.Contains("Kế hoạch")).Sum(d => d.Score);
                    total = sh + sk + ph + thi + kh;
                }

                ws.Cell(currentRow, 1).Value = idx++;
                ws.Cell(currentRow, 2).Value = club.Name;
                ws.Cell(currentRow, 3).Value = sh;
                ws.Cell(currentRow, 4).Value = sk;
                ws.Cell(currentRow, 5).Value = ph;
                ws.Cell(currentRow, 6).Value = thi;
                ws.Cell(currentRow, 7).Value = kh;
                ws.Cell(currentRow, 8).Value = total;
                currentRow++;
            }

            currentRow++; // spacer between months
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();
        var fileName = $"BaoCao_CLB_{sem.Name}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    //[HttpGet("student-semester")]
    //public async Task<IActionResult> ExportStudentSemester([FromQuery] int semesterId)
    //{
    //    var sem = await _context.Semesters.FindAsync(semesterId);
    //    if (sem == null) return NotFound("Semester not found");

    //    var records = await _context.MovementRecords
    //        .Include(r => r.Student)
    //        .Include(r => r.Details)
    //            .ThenInclude(d => d.Criterion)
    //                .ThenInclude(c => c.Group)
    //        .Where(r => r.SemesterId == semesterId)
    //        .OrderByDescending(r => r.TotalScore)
    //        .ToListAsync();

    //    using var wb = new XLWorkbook();
    //    var ws = wb.Worksheets.Add($"SV_{sem.Name}");

    //    // Header
    //    ws.Cell(1, 1).Value = "STT";
    //    ws.Cell(1, 2).Value = "Mã SV";
    //    ws.Cell(1, 3).Value = "Họ tên";
    //    ws.Cell(1, 4).Value = "Tổng điểm";
    //    ws.Cell(1, 5).Value = "Nhóm";
    //    ws.Cell(1, 6).Value = "Tiêu chí";
    //    ws.Cell(1, 7).Value = "Max/tiêu chí";
    //    ws.Cell(1, 8).Value = "Điểm";
    //    ws.Cell(1, 9).Value = "Nguồn";
    //    ws.Cell(1, 10).Value = "Ghi chú";
    //    ws.Cell(1, 11).Value = "ActivityId";
    //    ws.Cell(1, 12).Value = "Ngày cộng";
    //    ws.Row(1).Style.Font.SetBold();

    //    int row = 2;
    //    int idx = 1;
    //    foreach (var r in records)
    //    {
    //        if (r.Details == null || r.Details.Count == 0)
    //        {
    //            ws.Cell(row, 1).Value = idx++;
    //            ws.Cell(row, 2).Value = r.Student?.StudentCode;
    //            ws.Cell(row, 3).Value = r.Student?.FullName;
    //            ws.Cell(row, 4).Value = r.TotalScore;
    //            row++;
    //            continue;
    //        }

    //        foreach (var d in r.Details.OrderByDescending(d => d.AwardedAt))
    //        {
    //            ws.Cell(row, 1).Value = idx;
    //            ws.Cell(row, 2).Value = r.Student?.StudentCode;
    //            ws.Cell(row, 3).Value = r.Student?.FullName;
    //            ws.Cell(row, 4).Value = r.TotalScore;
    //            ws.Cell(row, 5).Value = d.Criterion?.Group?.Name;
    //            ws.Cell(row, 6).Value = d.Criterion?.Title;
    //            ws.Cell(row, 7).Value = d.Criterion?.MaxScore ?? 0;
    //            ws.Cell(row, 8).Value = d.Score;
    //            ws.Cell(row, 9).Value = string.IsNullOrWhiteSpace(d.ScoreType) ? "Auto" : d.ScoreType;
    //            ws.Cell(row, 10).Value = d.Note;
    //            ws.Cell(row, 11).Value = d.ActivityId;
    //            ws.Cell(row, 12).Value = d.AwardedAt;
    //            row++;
    //        }
    //        idx++;
    //    }

    //    // Autosize
    //    ws.Columns().AdjustToContents();

    //    using var ms = new MemoryStream();
    //    wb.SaveAs(ms);
    //    var bytes = ms.ToArray();
    //    var fileName = $"BaoCao_SinhVien_{sem.Name}.xlsx";
    //    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    //}
    [HttpGet("student-semester")]
    public async Task<IActionResult> ExportStudentSemester([FromQuery] int semesterId)
    {
        var sem = await _context.Semesters.FindAsync(semesterId);
        if (sem == null) return NotFound("Semester not found");

        // 1. Cập nhật Truy vấn: Chỉ cần include Student, không cần Details
        var records = await _context.MovementRecords
            .Include(r => r.Student)
            .Where(r => r.SemesterId == semesterId)
            .OrderByDescending(r => r.TotalScore)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"SV_{sem.Name}");

        // 2. Cập nhật Header: Chỉ giữ thông tin tổng hợp
        ws.Cell(1, 1).Value = "STT";
        ws.Cell(1, 2).Value = "Mã SV";
        ws.Cell(1, 3).Value = "Họ tên";
        ws.Cell(1, 4).Value = "Tổng điểm";
        ws.Row(1).Style.Font.SetBold();

        int row = 2;
        int idx = 1;

        // 3. Cập nhật Logic Ghi Dữ Liệu: Chỉ lặp qua MovementRecord (r)
        foreach (var r in records)
        {
            // Ghi dữ liệu của MovementRecord vào một dòng duy nhất
            ws.Cell(row, 1).Value = idx;
            ws.Cell(row, 2).Value = r.Student?.StudentCode;
            ws.Cell(row, 3).Value = r.Student?.FullName;
            ws.Cell(row, 4).Value = r.TotalScore;

            row++;
            idx++;
        }

        // Autosize
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();
        var fileName = $"BaoCao_SinhVien_{sem.Name}.xlsx";

        // Trả về file
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}


