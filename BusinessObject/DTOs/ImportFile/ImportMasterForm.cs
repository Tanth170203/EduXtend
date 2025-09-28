using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.ImportFile
{
    public class ImportMasterForm { public IFormFile File { get; set; } = null!; }
}
