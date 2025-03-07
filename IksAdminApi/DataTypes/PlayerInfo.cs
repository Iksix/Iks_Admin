using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace IksAdminApi;

public class PlayerInfo
{
    public int UserId {get; set;}
    public int Slot {get; set;}
    public string? Ip {get; set;}
    public string? SteamId {get; set;}
    public string PlayerName {get; set;}
    [JsonConstructor]
    public PlayerInfo(int userId, int slot, string? ip, string? steamId, string playerName)
    {
        UserId = userId;
        Slot = slot;
        Ip = ip;
        SteamId = steamId;
        PlayerName = playerName;
    }
    [JsonIgnore]
    public CCSPlayerController? Controller {get {
        if (SteamId == null) return null;
        return PlayersUtils.GetControllerBySteamId(SteamId);
    }}
    [JsonIgnore]
    public bool IsOnline {get {
        if (SteamId == null) return false;
        return PlayersUtils.GetControllerBySteamId(SteamId) != null;
    }}
    public PlayerInfo(CCSPlayerController player)
    {
        UserId = (int)player.UserId!;
        Slot = player.Slot;
        Ip = player.GetIp();
        if (!player.IsBot)
        {
            SteamId = player.AuthorizedSteamID!.SteamId64.ToString();
        }
        PlayerName = player.PlayerName;
    }
    public PlayerInfo()
    {
        UserId = 0;
        Slot = 0;
        Ip = "";
        SteamId = "";
        PlayerName = "";
    }

}