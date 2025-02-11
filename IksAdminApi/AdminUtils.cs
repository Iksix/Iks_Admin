using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Localization;

namespace IksAdminApi;


public static class AdminUtils
{
    public static BasePlugin CoreInstance;
    public delegate Admin? AdminFinderByController(CCSPlayerController player);
    public static AdminFinderByController FindAdminByControllerMethod = null!;
    public delegate Admin? AdminFinderById(int id);
    public static AdminFinderById FindAdminByIdMethod = null!;
    public static IIksAdminApi CoreApi = null!;
    public delegate Dictionary<string, Dictionary<string, string>> RightsGetter();
    public static RightsGetter GetPremissions = null!;
    public delegate CoreConfig ConfigGetter();
    public static ConfigGetter GetConfigMethod = null!;
    public delegate Group? GetGroupFromIdMethod(int id);
    public static GetGroupFromIdMethod GetGroupFromIdFunc = null!;
    public static string ConfigsDir {get => CoreApi.Plugin.ModuleDirectory + "/../../configs/plugins";}
    public static string[] BlockedIdentifiers(string key) 
    {
        return CoreApi.Config.BlockedIdentifiers.FirstOrDefault(x => x.Key == key).Value;
    }
    public static void LogDebug(string message)
    {
        if (CoreApi != null && CoreApi.Config != null && !CoreApi.Config.DebugMode)
        {
            return;
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[Admin Debug]: " + message);
        Console.ResetColor();
    }
    public static void LogError(string message)
    {
        if (CoreApi != null)
        {
            Server.NextFrame(() => {
                var data = new EventData("error");
                data.Insert("text", message);
                data.Invoke();
            });
        }
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Admin Error]: " + message);
        Console.ResetColor();
    }
    public static Group? GetGroup(int? id)
    {
        if (id == null) return null;
        return GetGroupFromIdFunc((int)id);
    }
    public static int CurrentTimestamp()
    {
        return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public static List<PlayerComm> GetComms(this CCSPlayerController player)
    {
        return CoreApi.Comms.Where(x => x.SteamId == player.GetSteamId()).ToList();
    }
    public static string GetDurationString(int seconds)
    {
        if (seconds == 0)
        {
            return CoreApi.Localizer["Other.Never"];
        }
        // ищем seconds во всех конфигах с Times =)
        if (BansConfig.Config.Times.TryGetValue(seconds/60, out var time)) return time;
        if (MutesConfig.Config.Times.TryGetValue(seconds/60, out time)) return time;
        if (GagsConfig.Config.Times.TryGetValue(seconds/60, out time)) return time;
        if (SilenceConfig.Config.Times.TryGetValue(seconds/60, out time)) return time;
        if (seconds % 86400 == 0)
        {
            return $"{seconds / 86400}{CoreApi.Localizer["Other.Days"]}";
        }
        if (seconds % 3600 == 0)
        {
            return $"{seconds / 3600}{CoreApi.Localizer["Other.Hours"]}";
        }
        if (seconds % 60 == 0)
        {
            return $"{seconds / 60}{CoreApi.Localizer["Other.Minutes"]}";
        }
        return $"{seconds}{CoreApi.Localizer["Other.Seconds"]}";
    }
    public static bool CanUnban(Admin admin, PlayerBan existingBan)
    {
        var bannedBy = existingBan.Admin;
        if (bannedBy == null) return true;
        if (bannedBy.SteamId == admin.SteamId) return true;
        if (bannedBy.SteamId != "CONSOLE")
        {
            if (admin.HasPermissions("blocks_manage.remove_all")) return true;
        } else {
            if (admin.HasPermissions("blocks_manage.remove_console")) return true;
            return false;
        }
        if (admin.HasPermissions("blocks_manage.remove_immunity") && bannedBy.CurrentImmunity < admin.CurrentImmunity) return true;
        if (admin.HasPermissions("other.equals_immunity_action") && admin.HasPermissions("blocks_manage.remove_immunity") && bannedBy.CurrentImmunity <= admin.CurrentImmunity) return true;
        return false;
    }
    public static bool CanUnComm(Admin admin, PlayerComm comm)
    {
        var bannedBy = comm.Admin;
        if (bannedBy == null) return true;
        if (bannedBy.SteamId == admin.SteamId) return true;
        if (bannedBy.SteamId != "CONSOLE")
        {
            if (admin.HasPermissions("blocks_manage.remove_all")) return true;
        } else {
            if (admin.HasPermissions("blocks_manage.remove_console")) return true;
            return false;
        }
        if (admin.HasPermissions("blocks_manage.remove_immunity") && bannedBy.CurrentImmunity < admin.CurrentImmunity) return true;
        if (admin.HasPermissions("other.equals_immunity_action") && admin.HasPermissions("blocks_manage.remove_immunity") && bannedBy.CurrentImmunity <= admin.CurrentImmunity) return true;
        return false;
    }

    public static bool HasMute(this List<PlayerComm> comms)
    {
        return comms.Any(x => x.MuteType is 0);
    }
    public static bool HasGag(this List<PlayerComm> comms)
    {
        return comms.Any(x => x.MuteType is 1);
    }
    public static bool HasSilence(this List<PlayerComm> comms)
    {
        return comms.Any(x => x.MuteType is 2);
    }
    public static PlayerComm? GetGag(this List<PlayerComm> comms)
    {
        return comms.FirstOrDefault(x => x.MuteType is 1);
    }
    public static PlayerComm? GetMute(this List<PlayerComm> comms)
    {
        return comms.FirstOrDefault(x => x.MuteType is 0);
    }
    public static PlayerComm? GetSilence(this List<PlayerComm> comms)
    {
        return comms.FirstOrDefault(x => x.MuteType is 2);
    }
    public static Admin? Admin(this CCSPlayerController? player)
    {
        if (player == null) return CoreApi.ConsoleAdmin;
        return FindAdminByControllerMethod(player);
    }
    
    public static Admin? ServerAdmin(this PlayerInfo player)
    {
        return CoreApi.ServerAdmins.FirstOrDefault(x => x.SteamId == player.SteamId);
    }
    public static Admin? ServerAdmin(int id)
    {
        return CoreApi.ServerAdmins.FirstOrDefault(x => x.Id == id);
    }
    public static Admin? Admin(int id)
    {
        return FindAdminByIdMethod(id);
    }
    public static Admin? Admin(string steamId)
    {
        if (steamId.ToLower() == "console")
            return CoreApi.ConsoleAdmin;
        return CoreApi.AllAdmins.FirstOrDefault(x => x.SteamId == steamId);
    }
    public static Admin? ServerAdmin(string steamId)
    {
        if (steamId.ToLower() == "console")
            return CoreApi.ConsoleAdmin;
        return CoreApi.ServerAdmins.FirstOrDefault(x => x.SteamId == steamId);
    }
    public static bool IsAdmin(this CCSPlayerController player)
    {
        return FindAdminByControllerMethod(player) != null;
    }
    public static CoreConfig Config()
    {
        return GetConfigMethod();
    }
    public static string AReplace(this LocalizedString localizer, string[] keys, object[] values)
    {
        var input = localizer.ToString()!;
        for (int i = 0; i < keys.Length; i++)
        {
            input = input.Replace("{" + keys[i] + "}", values[i].ToString());
        }
        return input;
    }
    public static string AReplace(this string input, string[] keys, object[] values)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            input = input.Replace("{" + keys[i] + "}", values[i].ToString());
        }
        return input;
    }
    public static void Print(this CCSPlayerController? player, string message, string? tag = null, bool toConsole = false)
    {
        if (message.Trim() == "") return;
        var eventData = new EventData("print_to_player");
        eventData.Insert("player", player);
        eventData.Insert("message", message);
        eventData.Insert("tag", tag);
        if (eventData.Invoke() != HookResult.Continue)
        {
            LogDebug("Print(...) stopped by event PRE ");
            return;
        }

        player = eventData.Get<CCSPlayerController?>("player");
        message = eventData.Get<string>("message");
        tag = eventData.Get<string>("tag");
        
        Server.NextFrame(() =>
        {
            if (player == null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
                return;
            }
            foreach (var str in message.Split("\n"))
            {
                if (toConsole) player.PrintToConsole($" {tag ?? CoreApi.Localizer["Tag"]} {str}");
                else player.PrintToChat($" {tag ?? CoreApi.Localizer["Tag"]} {str}");
            }

            eventData.Invoke("print_to_player_post");
        });
        
    }
    public static string? GetIp(this CCSPlayerController player)
    {
        var ip = player.IpAddress;
        if (ip == null) return null;
        if (CoreApi.Config.MirrorsIp.Contains(ip.Split(":")[0])) return null;
        return ip.Split(":")[0];
    }

    public static string GetSteamId(this CCSPlayerController? player)
    {
        if (player == null) return "CONSOLE";
        if (player.IsBot)
        {
            throw new Exception("Trying to get bot steam id");
        }
        var steamId = player.AuthorizedSteamID;
        return steamId!.SteamId64.ToString();
    }

    public static bool IsConsoleId(string steamId)
    {
        return steamId.ToLower() == "console";
    }

    public static void PrintToServer(string message, string? tag = null)
    {
        tag = tag ?? CoreApi.Localizer["Tag"];
        if (message.Trim() == "") return;
        foreach (var str in message.Split("\n"))
        {
            Server.PrintToChatAll($" {tag} {str}");
        }
    }
    public static void Reply(this CommandInfo info, string message, string? tag = null)
    {
        if (message.Trim() == "") return;
        foreach (var str in message.Split("\n"))
        {
            info.ReplyToCommand($" {tag ?? CoreApi.Localizer["Tag"]} {str}");
        }
    }
    /// <returns>Возвращает строку из текущих флагов по праву(ex: "admin_manage.add") (учитывая замену в кфг)</returns>
    public static string GetCurrentPermissionFlags(string key)
    {
        LogDebug("GetCurrentPermissionFlags for key: `" + key + "`");
        if (key == ">*") {
            return GetAllPermissionFlags();
        }
        var permissions = GetPremissions();
        var firstKey = key.Split(".")[0];
        var lastKey = string.Join(".", key.Split(".").Skip(1));
        if (!permissions.TryGetValue(firstKey, out var permission))
        {
            throw new Exception("Trying to get permissions group that doesn't registred (HasPermissions method) | " + key);
        }
        if (!permission.TryGetValue(lastKey, out var flags))
        {
            throw new Exception("Trying to get permissions that doesn't registred (HasPermissions method) | " + key);
        }
        if (Config().PermissionReplacement.ContainsKey(key))
        {
            LogDebug($"Replace permission flags from config...");
            flags = Config().PermissionReplacement[key];
            LogDebug($"Permission flags replacement ✔ | flags: {flags}");
        }
        return flags;
    }
    /// <returns>Возвращает строку из всех флагов которые используются в группе прав (учитывая замену в кфг)</returns>
    public static string GetAllPermissionGroupFlags(string key) // ex: admin_manage
    {
        var registredPermissions = GetPremissions();
        if (!registredPermissions.TryGetValue(key, out var permissions))
        {
            throw new Exception("Trying to get permissions group that doesn't registred (HasPermissions method)");
        }
        var flags = "";
        foreach (var permission in permissions)
        {
            flags += GetCurrentPermissionFlags($"{key}.{permission.Key}");
        }
        return flags;
    }
    /// <returns>Возвращает строку из всех флагов которые используются(учитывая замену в кфг)</returns>
    public static string GetAllPermissionFlags() // ex: admin_manage
    {
        var registredPermissions = GetPremissions();
        var flags = "";
        foreach (var permission in registredPermissions)
        {
            var group = permission.Key;
            foreach (var right in permission.Value)
            {
                flags += GetCurrentPermissionFlags($"{group}.{right.Key}");
            }
        }
        return flags;
    }
    /// <summary>
    /// Проверяет есть ли у админа доступ к любому из прав группы(ex: "admin_manage")
    /// </summary>
    public static bool HasAnyGroupPermission(this CCSPlayerController player, string key)
    {
        return HasAnyGroupPermission(player.Admin(), key);
    }
    public static bool HasAnyGroupPermission(this Admin? admin, string key)
    {
        var allGroupFlags = GetAllPermissionGroupFlags(key);
        if (allGroupFlags.Contains("*")) return true;
        if (admin == null) return false;
        if (admin.IsDisabled) return false;
        if (admin.CurrentFlags.ToCharArray().Any(allGroupFlags.Contains)) return true;
        return false;
    }
    public static bool HasPermissions(this CCSPlayerController? player, string key)
    {
        if (player == null)
        {
            return true;
        }
        var admin = player.Admin();
        return HasPermissions(admin, key);
    }
    public static bool HasPermissions(this Admin? admin, string key)
    {
        var flags = GetCurrentPermissionFlags(key);
        if (flags == "*")
        {
            LogDebug($"Has Access ✔");
            return true;
        }
        if (admin == null) {
            LogDebug($"Admin is null | No Access ✖");
            return false;
        }
        if (admin!.Warns.Count >= CoreApi.Config.MaxWarns)
        {
            return false;
        }
        LogDebug($"Checking permission: {admin.Name} | {key}" );
        LogDebug("AdminDisabled: " + admin!.IsDisabled);
        if (admin.IsDisabled) {
            LogDebug($"Admin is disabled | No Access ✖");
            return false;
        }
        //bool a = flags.ToCharArray().All(x => admin.CurrentFlags.Contains(x));
        if (flags.ToCharArray().All(x => admin.CurrentFlags.Contains(x)) || admin.CurrentFlags.Contains("z") || admin.SteamId == "CONSOLE"
            || (key == ">*" && admin.CurrentFlags.ToCharArray().Any(x => flags.Contains(x)))
            )
        {
            LogDebug($"Admin has access ✔");
            return true;
        } else {
            LogDebug($"Admin hasn't access ✖");
            return false;
        }
    }
    public static List<string> GetArgsFromCommandLine(string commandLine)
    {
        List<string> args = new List<string>();
        var regex = new Regex(@"(""((\\"")|([^""]))*"")|('((\\')|([^']))*')|(\S+)");
        var matches = regex.Matches(commandLine);
        foreach (Match match in matches)
        {
            var arg = match.Value;
            if (arg.StartsWith('"'))
            {
                arg = arg.Remove(0, 1);
                arg = arg.TrimEnd('"');
            }
            args.Add(arg);
        }
        args.RemoveAt(0);
        return args;
    }
}
