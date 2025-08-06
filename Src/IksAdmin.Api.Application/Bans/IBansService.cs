using IksAdmin.Api.Contracts.Bans;
using IksAdmin.Api.Entities.Bans;

namespace IksAdmin.Api.Application.Bans;

public interface IBansService
{
    /// <summary>
    /// Give ban to player
    /// </summary>
    /// <returns><see cref="Ban.Id"/> in DataBase</returns>
    Task<int> GiveAsync(GiveBanDto giveBanDto);
    
    /// <summary>
    /// Unban player
    /// </summary>
    /// <param name="banId">Ban Id</param>
    Task UnbanAsync(int banId);


    /// <summary>
    /// Execute ban for online player <br/><br/>
    /// Warning: This method doesn't add ban to database or something else <br/>
    /// For add ban use <see cref="GiveAsync"/>
    /// </summary>
    public void ExecuteBan(GiveBanDto giveBanDto);
}