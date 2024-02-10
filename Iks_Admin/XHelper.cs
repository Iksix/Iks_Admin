using System.Data;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace Iks_Admin;

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

    public static string RemoveDangerSimbols(string str)
    {
        str = str.Replace("'", "");
        str = str.Replace("\"", "");
        str = str.Replace(";", "");
        str = str.Replace("`", "");

        return str;
    }

    public static List<CCSPlayerController> GetOnlinePlayers()
    {
        var players = Utilities.GetPlayers();

        List<CCSPlayerController> validPlayers = new List<CCSPlayerController>();

        foreach (var p in players)
        {
            if (p == null) continue;
            if (!p.IsValid) continue;
            if (p.IsBot) continue;
            if (p.Connected != PlayerConnectedState.PlayerConnected) continue;
            validPlayers.Add(p);
        }

        return validPlayers;
    }

    public static List<string> SeparateString(string str)
    {
        List<string> sepStr = str.Split("\n").ToList();
        return sepStr;
    }

    /// <summary>
    /// Getting player by name, #uid, #sid64. For Uid and Sid You need add # at the start Like: #20
    /// </summary>
    public static CCSPlayerController? GetPlayerFromArg(string identity)
    {
        var players = XHelper.GetOnlinePlayers();
        CCSPlayerController? player = null;
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
    /// Checks if the string is an IP address
    /// </summary>
    private static bool IsIPAddress(string ipAddress)
    {
      bool isIPAddres = false;
 
      try
      {
        IPAddress address;
        // Определяем является ли строка ip-адресом
        isIPAddres = IPAddress.TryParse(ipAddress, out address);
      }
      catch (Exception e) { }
      return isIPAddres;
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

    public static string GetServerIp()
    {
        return ConVar.Find("ip").StringValue;
    }
    public static string GetServerPort()
    {
        return ConVar.Find("hostport").GetPrimitiveValue<int>().ToString();
    }

    public static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds((long)unixTimeStamp).DateTime.ToLocalTime();
    }

    public static string GetDateStringFromUTC(ulong unixTimeStamp)
    {
        return UnixTimeStampToDateTime(unixTimeStamp).ToString("dd.MM.yyyy HH:mm:ss");
    }

    public static bool IsControllerValid(CCSPlayerController? controller)
    {
        if (controller == null) return false;
        if (!controller.IsValid) return false;
        if (controller.IsBot) return false;
        if (controller.Connected != PlayerConnectedState.PlayerConnected) return false;
        return true;
    }

    public static ControllerParams GetControllerParams(CCSPlayerController controller)
    {
        var res = new ControllerParams(controller.PlayerName, controller.SteamID.ToString(), controller.IpAddress);
        return res;
    }

}
