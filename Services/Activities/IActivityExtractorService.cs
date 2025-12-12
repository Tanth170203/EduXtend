using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Http;

namespace Services.Activities
{
    public interface IActivityExtractorService
    {
        Task<ExtractedActivityDto> ExtractActivityFromFileAsync(IFormFile file);
        Task<ExtractedActivityDto> ExtractActivityFromProposalAsync(
            string proposalTitle, 
            string? proposalDescription,
            int proposalId,
            int clubId);
    }
}
