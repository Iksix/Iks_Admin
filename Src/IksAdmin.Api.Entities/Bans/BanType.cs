namespace IksAdmin.Api.Entities.Bans;

public enum BanType
{
    /// <summary>
    /// Ban only by SteamId
    /// </summary>
    SteamId,
    
    /// <summary>
    /// Ban only by Ip
    /// </summary>
    Ip,
    
    /// <summary>
    /// Ban for both SteamId and Ip
    /// </summary>
    Both
}