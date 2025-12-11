using BusinessObject.DTOs.CVExport;

namespace Services.CVExport
{
    public interface ICVParserService
    {
        Task<ExtractedCVDataDto> ParseCVAsync(byte[] fileData, string extension, int joinRequestId);
        string ExtractText(byte[] fileData, string extension);
        ExtractedCVDataDto ExtractStructuredData(string text, int joinRequestId);
    }
}
