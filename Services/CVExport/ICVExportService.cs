using BusinessObject.DTOs.CVExport;

namespace Services.CVExport
{
    public interface ICVExportService
    {
        Task<CVExportResultDto> ExtractCVDataAsync(CVExportRequestDto request);
        Task<byte[]> GenerateExcelAsync(CVExportResultDto data, string clubName);
    }
}
