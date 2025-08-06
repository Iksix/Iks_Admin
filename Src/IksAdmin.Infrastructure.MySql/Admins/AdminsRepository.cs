using IksAdmin.Api.Application.Admins;
using IksAdmin.Api.Entities.Admins;
using Microsoft.EntityFrameworkCore;
using XUtils;

namespace IksAdmin.Infrastructure.MySql.Admins;

public class AdminsRepository : IAdminsRepository
{
    private readonly AppDbContext _dbContext;

    public AdminsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Admin>> GetAllAsync(bool includeDeleted = false)
    {
        var result =  _dbContext.Admins.AsNoTracking();

        if (!includeDeleted)
            result = result.Where(e => e.DeletedAt == null);
        
        return await result.ToListAsync();
    }

    public async Task<IEnumerable<Admin>> GetByServerIdAsync(int? serverId, bool includeDeleted = false)
    {
        var result =  _dbContext.Admins.AsNoTracking().Where(e => e.ServerIds.Contains(serverId));

        if (!includeDeleted)
            result = result.Where(e => e.DeletedAt == null);
        
        return await result.ToListAsync();
    }

    public async Task<IEnumerable<Admin>> GetBySteamIdAsync(ulong steamId, bool includeDeleted = false)
    {
        var result =  _dbContext.Admins.AsNoTracking().Where(e => e.SteamId == steamId);

        if (!includeDeleted)
            result = result.Where(e => e.DeletedAt == null);
        
        return await result.ToListAsync();
    }

    public async Task<Admin?> GetByIdAsync(int id)
    {
        return await _dbContext.Admins.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Admin> GetConsoleAsync()
    {
        return await _dbContext.Admins.AsNoTracking().FirstAsync(e => e.SteamId == null);
    }

    public async Task<Admin> CreateAsync(Admin newAdmin)
    {
        newAdmin.CreatedAt = DateUtils.GetCurrentTimestamp();
        
        await _dbContext.Admins.AddAsync(newAdmin);
        
        return newAdmin;
    }

    public async Task UpdateAsync(Admin admin)
    {
        admin.UpdatedAt = DateUtils.GetCurrentTimestamp();
        
        await _dbContext.Admins.ExecuteUpdateAsync(a => a
            .SetProperty(x => x.SteamId, admin.SteamId)
            .SetProperty(x => x.Name, admin.Name)
            .SetProperty(x => x.Flags, admin.Flags)
            .SetProperty(x => x.Immunity, admin.Immunity)
            .SetProperty(x => x.GroupId, admin.GroupId)
            .SetProperty(x => x.Discord, admin.Discord)
            .SetProperty(x => x.Vk, admin.Vk)
            .SetProperty(x => x.IsDisabled, admin.IsDisabled)
            .SetProperty(x => x.EndAt, admin.EndAt)
            .SetProperty(x => x.CreatedAt, admin.CreatedAt)
            .SetProperty(x => x.UpdatedAt, admin.UpdatedAt)
            .SetProperty(x => x.DeletedAt, admin.DeletedAt)
        );
    }
}