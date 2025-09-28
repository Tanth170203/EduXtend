using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.ImportFile
{
    public class ImportAccountsForm
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
