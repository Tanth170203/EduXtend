using BusinessObject.DTOs.ImportFile;
using Microsoft.AspNetCore.Http;

namespace Services.UserImport
{
    public interface IUserImportService
    {
        Task<ImportUsersResponse> ImportUsersFromExcelAsync(IFormFile file);
    }
}

