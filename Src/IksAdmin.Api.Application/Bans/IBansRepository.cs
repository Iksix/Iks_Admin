using IksAdmin.Api.Entities.Bans;

namespace IksAdmin.Api.Application.Bans;

public interface IBansRepository
{
    Task<IEnumerable<Ban>> GetAllAsync();

    Task<IEnumerable<Ban>> GetLastAsync(int maxCount = 1);
    
    Task<Ban?> GetByIdAsync(int id);
    
    Task<IEnumerable<Ban>> GetByAdminIdAsync(int adminId);
    
    Task<IEnumerable<Ban>> GetBySteamIdAsync(ulong steamId);
    
    Task<IEnumerable<Ban>> GetByIpAsync(string ip);

    Task<IEnumerable<Ban>> GetLastByAdminIdAsync(int adminId, int maxCount = 1);
    
    Task<IEnumerable<Ban>> GetLastBySteamIdAsync(ulong steamId, int maxCount = 1);
    
    Task<IEnumerable<Ban>> GetLastByIpAsync(string ip, int maxCount = 1);
    
    Task<int> AddAsync(Ban ban);
    
    Task UpdateAsync(Ban ban);
}