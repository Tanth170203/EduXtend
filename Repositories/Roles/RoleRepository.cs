using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Roles;

public class RoleRepository : IRoleRepository
{
    private readonly EduXtendContext _db;

    public RoleRepository(EduXtendContext db)
    {
        _db = db;
    }

    public async Task<List<Role>> GetAllAsync()
        => await _db.Roles.OrderBy(r => r.Id).ToListAsync();

    public async Task<Role?> GetByIdAsync(int id)
        => await _db.Roles.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Role?> GetByNameAsync(string roleName)
        => await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
}

