using BusinessObject.DTOs.CVExport;

namespace Services.CVExport
{
    public interface IExcelGeneratorService
    {
        byte[] GenerateExcel(List<ExtractedCVDataDto> data, string clubName);
    }
}
