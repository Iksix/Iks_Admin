using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace XUtils;

public static class ServerUtils
{
    public static List<CCSPlayerController> GetOnlinePlayers(
        bool includeBots = false,
        bool skipNotValid = true
        )
    {
        var players = new List<CCSPlayerController>();

        foreach (var player in Utilities.GetPlayers())
        {
            if (!skipNotValid && !player.IsValid)
            {
                players.Add(player);
                continue;
            }
            
            if (!includeBots && player.IsBot)
                continue;
            
            players.Add(player);
        }
        
        return players;
    }

    public static CCSPlayerController? GetPlayer(ulong steamId64)
    {
        for (int i = 0; i < 72; i++)
        {
            var player = Utilities.GetPlayerFromSlot(i);

            if (player == null || !player.IsValid) continue;

            if (player.AuthorizedSteamID == null) continue;
            
            if (player.AuthorizedSteamID.SteamId64 == steamId64)
                return player;
        }
        
        return null;
    }
}