using IksAdmin.Api.Entities.Bans;

namespace IksAdmin.Api.Contracts.Bans;

public class GiveBanDto
{
    public int AdminId { get; init; }
    
    public required string Reason { get; init; }
    
    public int Duration { get; init; }
    
    public BanType BanType { get; init; }
    
    public ulong? SteamId { get; init; }
    
    public string? Ip { get; init; }
    
    public bool Announce { get; init; }

    /// <summary>
    /// Use this ctor for give ban with all params
    /// </summary>
    /// <param name="adminId">admin_id who ban the player</param>
    /// <param name="reason">ban reason</param>
    /// <param name="duration">ban duration in seconds</param>
    /// <param name="steamId">Target SteamId64</param>
    /// <param name="ip">Target Ip</param>
    /// <param name="banType">Type of ban</param>
    /// <param name="announce">Write announce about ban in game chat</param>
    public GiveBanDto(int adminId, string reason, int duration, ulong steamId, string ip, BanType banType, bool announce = true)
    {
        AdminId = adminId;
        Reason = reason;
        Duration = duration;
        BanType = banType;
        Announce = announce;
        SteamId = steamId;
        Ip = ip;
    }
    
    /// <summary>
    /// Use this ctor for give ban by SteamId
    /// </summary>
    /// <param name="adminId">admin_id who ban the player</param>
    /// <param name="reason">ban reason</param>
    /// <param name="duration">ban duration in seconds</param>
    /// <param name="steamId">Target SteamId64</param>
    /// <param name="announce">Write announce about ban in game chat</param>
    public GiveBanDto(int adminId, string reason, int duration, ulong steamId, bool announce = true)
    {
        AdminId = adminId;
        Reason = reason;
        Duration = duration;
        BanType = BanType.SteamId;
        SteamId = steamId;
        Announce = announce;
    }
    
    /// <summary>
    /// Use this ctor for give ban by Ip
    /// </summary>
    /// <param name="adminId">admin_id who ban the player</param>
    /// <param name="reason">ban reason</param>
    /// <param name="duration">ban duration in seconds</param>
    /// <param name="ip">Target Ip</param>
    /// <param name="announce">Write announce about ban in game chat</param>
    public GiveBanDto(int adminId, string reason, int duration, string ip, bool announce = true)
    {
        AdminId = adminId;
        Reason = reason;
        Duration = duration;
        BanType = BanType.Ip;
        Ip = ip;
        Announce = announce;
    }
}