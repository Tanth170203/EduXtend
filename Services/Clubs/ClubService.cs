using BusinessObject.DTOs.Club;
using Repositories.Clubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Clubs
{
    public class ClubService : IClubService
    {
        private readonly IClubRepository _repo;
        public ClubService(IClubRepository repo) => _repo = repo;

        public async Task<List<ClubListItemDto>> GetAllClubsAsync()
        {
            var clubs = await _repo.GetAllAsync();
            return clubs.Select(c => new ClubListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                SubName = c.SubName,
                LogoUrl = c.LogoUrl,
                CategoryName = c.Category.Name,
                IsActive = c.IsActive
            }).ToList();
        }

        public async Task<ClubDetailDto?> GetClubByIdAsync(int id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c == null) return null;

            return new ClubDetailDto
            {
                Id = c.Id,
                Name = c.Name,
                SubName = c.SubName,
                Description = c.Description,
                LogoUrl = c.LogoUrl,
                BannerUrl = c.BannerUrl,
                IsActive = c.IsActive,
                FoundedDate = c.FoundedDate,
                CategoryName = c.Category.Name
            };
        }
    }
}
