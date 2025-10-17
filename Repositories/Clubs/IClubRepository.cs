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
        Task<List<Club>> SearchClubsAsync(string? searchTerm, string? categoryName, bool? isActive);
        Task<Club?> GetByIdAsync(int id);
        Task<Club?> GetByIdWithDetailsAsync(int id);
        Task<int> GetMemberCountAsync(int clubId);
        Task<int> GetActivityCountAsync(int clubId);
        Task<List<ClubCategory>> GetAllCategoriesAsync();
    }
}
