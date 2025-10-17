using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Clubs
{
    public interface IClubRepository
    {
        Task<List<Club>> GetAllAsync();
        Task<Club?> GetByIdAsync(int id);
    }
}
