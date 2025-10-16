using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.ImportFile
{
    public class ImportUsersRequest
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; } = null!;
    }
}

