using ClosedXML.Excel;

namespace WebAPI.Utils;

public static class ExcelHelper
{
    public static byte[] CreateAccountTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Accounts");
        ws.Cell(1, 1).Value = "Email";
        ws.Cell(1, 2).Value = "FullName";
        ws.Cell(1, 3).Value = "RoleNames";       // e.g., "Student,FacultyStaff"
        ws.Cell(1, 4).Value = "StudentCode";
        ws.Cell(1, 5).Value = "StaffCode";
        ws.Cell(1, 6).Value = "DepartmentCode";
        ws.Cell(1, 7).Value = "FacultyCode";
        ws.Cell(1, 8).Value = "ClassName";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
