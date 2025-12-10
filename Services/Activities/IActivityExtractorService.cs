using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Http;

namespace Services.Activities
{
    public interface IActivityExtractorService
    {
        Task<ExtractedActivityDto> ExtractActivityFromFileAsync(IFormFile file);
    }
}
