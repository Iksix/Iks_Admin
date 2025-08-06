using IksAdmin.Api.Entities.Admins;

namespace IksAdmin.Api.Application.Admins;

public interface IAdminsRepository
{
    Task<IEnumerable<Admin>> GetAllAsync(bool includeDeleted = false);
    
    Task<IEnumerable<Admin>> GetByServerIdAsync(int? serverId, bool includeDeleted = false);
    
    Task<IEnumerable<Admin>> GetBySteamIdAsync(ulong steamId, bool includeDeleted = false);
    
    /// <summary>
    /// Gets <see cref="Admin"/> by <see cref="Admin.Id"/> <br/>
    /// <c>!</c> Can return deleted Admin
    /// </summary>
    Task<Admin?> GetByIdAsync(int id);
    
    Task<Admin> GetConsoleAsync();

    Task<Admin> CreateAsync(Admin newAdmin);
    
    Task UpdateAsync(Admin admin);
}