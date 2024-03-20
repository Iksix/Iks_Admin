using System.Reflection;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdminApi;

namespace IksAdmin;

public class XHelper
{
    public static List<string> GetArgsFromCommandLine(string commandLine)
    {
        List<string> args = new List<string>();
        var regex = new Regex(@"(""((\\"")|([^""]))*"")|('((\\')|([^']))*')|(\S+)");
        var matches = regex.Matches(commandLine);
        foreach (Match match in matches)
        {
            args.Add(match.Value);
        }
        args.RemoveAt(0);
        return args;
    }

    public static List<CCSPlayerController> GetOnlinePlayers(bool getBots = false)
    {
        var players = Utilities.GetPlayers();

        List<CCSPlayerController> validPlayers = new List<CCSPlayerController>();

        foreach (var p in players)
        {
            if (!p.IsValid) continue;
            if (p.AuthorizedSteamID == null && !getBots) continue;
            if (p.IsBot && !getBots) continue;
            if (p.Connected != PlayerConnectedState.PlayerConnected) continue;
            validPlayers.Add(p);
        }

        return validPlayers;
    }

    /// <summary>
    /// Getting player by name, #uid, #sid64. For Uid and Sid You need add # at the start Like: #20
    /// </summary>
    public static CCSPlayerController? GetPlayerFromArg(string identity)
    {
        var players = XHelper.GetOnlinePlayers();
        CCSPlayerController? player;
        if (identity.StartsWith("#"))
        {
            player = players.FirstOrDefault(u => u.SteamID.ToString() == identity.Replace("#", ""));

            if (player != null) return player;

            player = players.FirstOrDefault(u => u.UserId.ToString() == identity.Replace("#", ""));

            if (player != null) return player;
        }
        if (!identity.StartsWith("#"))
            return GetOnlinePlayers().FirstOrDefault(u => u.PlayerName.Contains(identity));
        return null;
    }

    /// <summary>
    /// Getting identity type, can return "name", "uid", "sid" or null
    /// </summary>
    public static string? GetIdentityType(string identity)
    {
        if (!identity.StartsWith("#")) return "name";
        if (identity.StartsWith("#") && identity.Length < 17) return "uid";
        if (identity.StartsWith("#") && identity.Replace("#", "").Length == 17) return "sid";
        return null;
    }

    /// <summary>
    /// Replace Colors to CSSharp ChatColors
    /// </summary>
    public static string ReplaceColors(string str)
    {
        foreach (FieldInfo field in typeof(ChatColors).GetFields())
        {
            string pattern = $"{{{field.Name}}}";
            if (str.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                str = str.Replace(pattern, field.GetValue(null)!.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }
        return str;
    }

    public static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds((long)unixTimeStamp).DateTime.ToLocalTime();
    }

    public static string GetDateStringFromUtc(int unixTimeStamp)
    {
        return UnixTimeStampToDateTime((ulong)unixTimeStamp).ToString("dd.MM.yyyy HH:mm:ss");
    }

    public static bool IsControllerValid(CCSPlayerController? controller)
    {
        if (controller == null) return false;
        if (controller.AuthorizedSteamID == null) return false;
        if (!controller.IsValid) return false;
        if (controller.IsBot) return false;
        if (controller.Connected != PlayerConnectedState.PlayerConnected) return false;
        return true;
    }

    public static PlayerInfo CreateInfo(CCSPlayerController player)
    {
        return new PlayerInfo(player.PlayerName, player.AuthorizedSteamID!.SteamId64, player.IpAddress!);
    }
    public static CsTeam GetTeamFromString(string team)
    {
        return team.ToLower() switch
        {
            "ct" => CsTeam.CounterTerrorist,
            "t" => CsTeam.Terrorist,
            "spec" => CsTeam.Spectator,
            _ => CsTeam.None
        };
    }
    public static string GetStringFromTeam(CsTeam team)
    {
        return team switch
        {
            CsTeam.Spectator => "SPEC",
            CsTeam.Terrorist => "T",
            CsTeam.CounterTerrorist => "CT",
            _ => "NONE"
        };
    }
    public static CsTeam GetTeamFromNum(int teamNum)
    {
        return teamNum switch
        {
            3 => CsTeam.CounterTerrorist,
            2 => CsTeam.Terrorist,
            1 => CsTeam.Spectator,
            _ => CsTeam.None
        };
    }
    
}
