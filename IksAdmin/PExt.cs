using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace IksAdmin;

public static class PExt
{
    public static string GetSteamId(this CCSPlayerController? controller)
    {
        return controller == null ? "CONSOLE" : controller.AuthorizedSteamID!.SteamId64.ToString();
    }
    public static string GetName(this CCSPlayerController? controller)
    {
        return controller == null ? "CONSOLE" : controller.PlayerName;
    }
    public static string GetIp(this CCSPlayerController? controller)
    {
        return controller == null ? "0.0.0.0" : controller.IpAddress!.Split(":")[0] ?? "0.0.0.0";
    }
    public static void Kick(this CCSPlayerController? controller)
    {
        if (controller == null) return;
        Server.ExecuteCommand("kickid " + controller.UserId);
    }
}