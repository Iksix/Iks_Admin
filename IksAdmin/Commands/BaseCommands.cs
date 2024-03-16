using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdmin.Menus;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Commands;

public class BaseCommands
{
    private static IIksAdminApi? _api = IksAdmin.Api;
    private static IStringLocalizer _localizer = _api!.Localizer;
    public static PluginConfig? Config;
    

    public static void ReplyToCommand(CommandInfo info, string reply, string? replyToConsole = null)
    {
        Server.NextFrame(() =>
        {
            var player = info.CallingPlayer;
            string message = player != null ? reply : replyToConsole == null ? reply : replyToConsole; 
            foreach (var str in message.Split("\n"))
            {
                if (player == null)
                {
                    info.ReplyToCommand($" [IksAdmin] {str}");
                }
                else
                {
                    if (reply.Trim() == "") return;
                    player.PrintToChat($" {_localizer["PluginTag"]} {str}");
                }
            }
        });
    }

    public static bool IsSteamId(string arg)
    {
        if (!UInt64.TryParse(arg, out _) || arg.Length != 17)
        {
            return false;
        }
        return true;
    }
    public static void AdminAdd(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_adminadd <sid> <name> <flags/-> <immunity> <group_id> <time> <server_id/ - (ALL SERVERS)>
        string sid = args[0];
        if (!IsSteamId(sid))
        {
            ReplyToCommand(info, "Incorrect SteamId");
            return;
        }
        string name = args[1];
        string flags = args[2] == "-" ? "" : args[2];
        int immunity = int.Parse(args[3]);
        int groupId = int.Parse(args[4]);
        int time = int.Parse(args[5]);
        string serverId = args.Count < 6 ? Config!.ServerId : args[6] == "-" ? "" : args[6];
        var newAdmin = new Admin(
            name,
            sid,
            flags,
            immunity,
            time == 0 ? 0 : (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60),
            groupId,
            serverId);

        Task.Run(async () =>
        {
            ReplyToCommand(info, "add admin..");
            await _api!.AddAdmin(newAdmin);
            ReplyToCommand(info, "New admin added");
        });
    }
    
    public static void AdminDel(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_admindel <sid>
        string sid = args[0];
        Task.Run(async () =>
        {
            if (await _api!.DelAdmin(sid))
            {
                ReplyToCommand(info, "Admin deleted");
            } else ReplyToCommand(info, "Admin not exists");
        });
    }
    public static void ReloadAdmins(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_reload_admins
        Task.Run(async () =>
        {
            await _api!.ReloadAdmins();
            ReplyToCommand(info, "Admins reloaded");
        });
    }
    public static void Admin(CCSPlayerController caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_admin
        PluginMenuManager.OpenAdminMenu(caller);
    }
    public static void ReloadInfractions(CCSPlayerController caller, Admin? admin, List<string> args, CommandInfo info)
    {
        if (!IsSteamId(args[0]))
        {
            info.ReplyToCommand("ERROR: Incorrect SteamId64!");
            return;
        }

        Task.Run(async () =>
        {
            await _api!.ReloadInfractions(args[0]);
            Server.NextFrame(() =>
            {
                info.ReplyToCommand("Infractions reloaded");
            });
        });
    }

    public static void Ban(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_ban <#uid/#sid/name> <duration> <reason> <name if needed>
        string identity = args[0];
        var target = XHelper.GetPlayerFromArg(identity);
        if (XHelper.GetIdentityType(identity) != "sid" && target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        string sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Replace("#", "");
        string adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();
        if (!_api!.HasMoreImmunity(adminSid, sid))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        string ip = target != null ? target.IpAddress! : "Undefined";
        int duration = int.Parse(args[1])*60;
        string reason = args[2];
        string name = args.Count > 3 ? string.Join(" ", args.Skip(3)) :
            target == null ? "Undefined" : target.PlayerName;
        var newBan = new PlayerBan(
            name,
            sid,
            ip,
            adminSid,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            duration,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + duration,
            reason,
            Config!.ServerId
        );
        Task.Run(async () =>
        {
            var banStatus = await _api.AddBan(adminSid, newBan);
            if (banStatus)
                ReplyToCommand(info, _localizer["NOTIFY_OnBan"].Value.Replace("{name}", name), $"Ban added to player!");
            else ReplyToCommand(info, _localizer["NOTIFY_PlayerAlreadyPunished"], $"The player has already been punished!");
        });
    }
    
    public static void Gag(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_gag <#uid/#sid/name> <duration> <reason> <name if needed>
        string identity = args[0];
        var target = XHelper.GetPlayerFromArg(identity);
        if (XHelper.GetIdentityType(identity) != "sid" && target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        string sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Replace("#", "");
        string adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();
        if (!_api!.HasMoreImmunity(adminSid, sid))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        int duration = int.Parse(args[1])*60;
        string reason = args[2];
        string name = args.Count > 3 ? string.Join(" ", args.Skip(3)) :
            target == null ? "Undefined" : target.PlayerName;
        var newGag = new PlayerComm(
            name,
            sid,
            adminSid,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            duration,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + duration,
            reason,
            Config!.ServerId
        );
        Task.Run(async () =>
        {
            var banStatus = await _api.AddGag(adminSid, newGag);
            if (banStatus)
                ReplyToCommand(info, _localizer["NOTIFY_OnGag"].Value.Replace("{name}", name), $"Gag added to player!");
            else ReplyToCommand(info, _localizer["NOTIFY_PlayerAlreadyPunished"], $"The player has already been punished!");
        });
    }
    public static void Mute(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_gag <#uid/#sid/name> <duration> <reason> <name if needed>
        string identity = args[0];
        var target = XHelper.GetPlayerFromArg(identity);
        if (XHelper.GetIdentityType(identity) != "sid" && target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        string sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Replace("#", "");
        string adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();
        if (!_api!.HasMoreImmunity(adminSid, sid))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        int duration = int.Parse(args[1])*60;
        string reason = args[2];
        string name = args.Count > 3 ? string.Join(" ", args.Skip(3)) :
            target == null ? "Undefined" : target.PlayerName;
        var newGag = new PlayerComm(
            name,
            sid,
            adminSid,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            duration,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + duration,
            reason,
            Config!.ServerId
        );
        Task.Run(async () =>
        {
            var banStatus = await _api.AddMute(adminSid, newGag);
            if (banStatus)
                ReplyToCommand(info, _localizer["NOTIFY_OnMute"].Value.Replace("{name}", name), $"Mute added to player!");
            else ReplyToCommand(info, _localizer["NOTIFY_PlayerAlreadyPunished"], $"The player has already been punished!");
        });
    }

    public static void UnMute(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_unmute <#uid/#sid/name>
        var identity = args[0];
        var target = XHelper.GetPlayerFromArg(identity);
        if (target == null && XHelper.GetIdentityType(identity) != "sid")
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }

        var sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Remove(0, 1);
        var adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();

        Task.Run(async () =>
        {
            var oldPunishment = await _api!.UnMute(sid, adminSid);
            if (oldPunishment == null)
            {
                ReplyToCommand(info, _localizer["NOTIFY_PlayerNotPunished"], "Player not punished!");
                return;
            }
            ReplyToCommand(info, _localizer["NOTIFY_OnUnMute"].Value.Replace("{name}", oldPunishment.Name), "Player unmuted!");
        });
    }
    public static void UnGag(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_ungag <#uid/#sid/name>
        var identity = args[0];
        var target = XHelper.GetPlayerFromArg(identity);
        if (target == null && XHelper.GetIdentityType(identity) != "sid")
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }

        var sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Remove(0, 1);
        var adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();

        Task.Run(async () =>
        {
            var oldPunishment = await _api!.UnGag(sid, adminSid);
            if (oldPunishment == null)
            {
                ReplyToCommand(info, _localizer["NOTIFY_PlayerNotPunished"], "Player not punished!");
                return;
            }
            ReplyToCommand(info, _localizer["NOTIFY_OnUnGag"].Value.Replace("{name}", oldPunishment.Name), "Player ungagged!");
        });
    }
    public static void UnBan(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_unban <sid>
        var sid = args[0];
        if (!IsSteamId(sid))
        {
            ReplyToCommand(info, _localizer["NOTIFY_IncorrectSid"], "Incorrect SteamId64!");
            return;
        }
        var adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();

        Task.Run(async () =>
        {
            var oldPunishment = await _api!.UnBan(sid, adminSid);
            if (oldPunishment == null)
            {
                ReplyToCommand(info, _localizer["NOTIFY_PlayerNotPunished"], "Player not punished!");
                return;
            }
            ReplyToCommand(info, _localizer["NOTIFY_OnUnBan"].Value.Replace("{name}", oldPunishment.Name), "Player unban!");
        });
    }

    public static void Kick(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        string adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();
        var target = XHelper.GetPlayerFromArg(args[0]);
        if (target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        if (!_api!.HasMoreImmunity(adminSid, target.AuthorizedSteamID!.SteamId64.ToString()))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        var reason = string.Join(" ", args.Skip(1));
        if (reason.Trim() == "")
        {
            ReplyToCommand(info, _localizer["NOTIFY_ErrorReason"], "Reason can't be void");
            return;
        }
        var targetInfo = XHelper.CreateInfo(target);
        Server.ExecuteCommand("kickid " + target.UserId);
        _api.EKick(adminSid, targetInfo, reason);
    }
    public static void Slay(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        string adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();
        var target = XHelper.GetPlayerFromArg(args[0]);
        if (target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        if (!_api!.HasMoreImmunity(adminSid, target.AuthorizedSteamID!.SteamId64.ToString()))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        var reason = string.Join(" ", args.Skip(1));
        if (reason.Trim() == "")
        {
            ReplyToCommand(info, _localizer["NOTIFY_ErrorReason"], "Reason can't be void");
            return;
        }
        var targetInfo = XHelper.CreateInfo(target);
        target.CommitSuicide(true, true);
        _api.ESlay(adminSid, targetInfo);
    }

    public static void SwitchTeam(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        string adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();
        var target = XHelper.GetPlayerFromArg(args[0]);
        if (target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        if (!_api!.HasMoreImmunity(adminSid, target.AuthorizedSteamID!.SteamId64.ToString()))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        var team = args[1];
        var oldTeam = XHelper.GetTeamFromNum(target.TeamNum);
        var newTeam = XHelper.GetTeamFromString(team);
        if (newTeam == CsTeam.Spectator)
        {
            ReplyToCommand(info, _localizer["NOTIFY_OnlyInChangeTeam"], "You can change team to this only with css_changeteam");
            return;
        }
        if (newTeam == CsTeam.None)
        {
            ReplyToCommand(info, _localizer["NOTIFY_IncorrectTeam"], "You can use only <t/ct> for team");
            return;
        }
        target.SwitchTeam(newTeam);
        _api.ESwitchTeam(adminSid, XHelper.CreateInfo(target), oldTeam, newTeam);
    }
    public static void ChangeTeam(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        string adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();
        var target = XHelper.GetPlayerFromArg(args[0]);
        if (target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        if (!_api!.HasMoreImmunity(adminSid, target.AuthorizedSteamID!.SteamId64.ToString()))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        var team = args[1];
        var oldTeam = XHelper.GetTeamFromNum(target.TeamNum);
        var newTeam = XHelper.GetTeamFromString(team);
        if (newTeam == CsTeam.None)
        {
            ReplyToCommand(info, _localizer["NOTIFY_IncorrectTeam"], "You can use only <t/ct/spec> for team");
            return;
        }
        target.ChangeTeam(newTeam);
        _api.EChangeTeam(adminSid, XHelper.CreateInfo(target), oldTeam, newTeam);
    }


    public static void Who(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        var target = XHelper.GetPlayerFromArg(args[0]);
        if (target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        var existingAdmin =
            _api!.ThisServerAdmins.FirstOrDefault(x => x.SteamId == target.AuthorizedSteamID!.SteamId64.ToString());
        if (existingAdmin == null)
        {
            ReplyToCommand(info, $"Zero permissions! \n Name: {target.PlayerName} \n SteamId: {target.AuthorizedSteamID!.SteamId64}");
            return;
        }

        var groupInfo = existingAdmin.GroupId != -1 ? $"\n Group Name: {existingAdmin.GroupName} \n GroupId: {existingAdmin.GroupId}" : "";
        ReplyToCommand(info, $"Name: {existingAdmin.Name} \n Flags: {existingAdmin.Flags} {groupInfo} \n Immunity: {existingAdmin.Immunity} \n SteamId: {existingAdmin.SteamId} \n ServerId: {existingAdmin.ServerId}");
    }

    public static void RBan(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_rban <sid> <ip/-(Auto)> <adminSid/CONSOLE> <duration> <reason> <BanType (0 - default / 1 - ip> <name/-(Auto)>
        var sid = args[0];
        if (!IsSteamId(sid))
        {
            ReplyToCommand(info, "Incorrect SteamId");
            return;
        }
        var target = XHelper.GetPlayerFromArg($"#{sid}");
        var ip = args[1] != "-" ? args[1] : target == null ? "Undefined" : target.IpAddress!;
        var adminSid = args[2];
        if (!_api!.HasAccess(adminSid, CommandUsage.CLIENT_AND_SERVER, "ban", "b"))
        {
            ReplyToCommand(info, "This admin haven't access to this command!");
            return;
        }
        if (!_api.HasMoreImmunity(adminSid, sid)) return;
        var time = int.Parse(args[3])*60;
        var reason = args[4];
        var banType = int.Parse(args[5]);
        var name = args[6] != "-" ? args[6] : target == null ? "Undefined" : target.PlayerName;
        var serverId = Config!.ServerId;
        var newBan = new PlayerBan(
        name,
        sid,
        ip,
        adminSid,
        (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        time,
        (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
        reason,
        serverId,
        banType
        );
        Task.Run(async () =>
        {
            await _api.AddBan(adminSid, newBan);
        });
    }
    public static void RGag(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_rgag <sid> <adminSid/CONSOLE> <duration> <reason> <name/-(Auto)>
        var sid = args[0];
        if (!IsSteamId(sid))
        {
            ReplyToCommand(info, "Incorrect SteamId");
            return;
        }
        var target = XHelper.GetPlayerFromArg($"#{sid}");
        var adminSid = args[1];
        if (!_api!.HasAccess(adminSid, CommandUsage.CLIENT_AND_SERVER, "gag", "g"))
        {
            ReplyToCommand(info, "This admin haven't access to this command!");
            return;
        }
        if (!_api.HasMoreImmunity(adminSid, sid)) return;
        var time = int.Parse(args[2])*60;
        var reason = args[3];
        var name = args[4] != "-" ? args[4] : target == null ? "Undefined" : target.PlayerName;
        var serverId = Config!.ServerId;
        var newGag = new PlayerComm(
            name,
            sid,
            adminSid,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            time,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
            reason,
            serverId
        );
        Task.Run(async () =>
        {
            await _api.AddGag(adminSid, newGag);
        });
    }
    public static void RMute(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_rmute <sid> <adminSid/CONSOLE> <duration> <reason> <name/-(Auto)>
        var sid = args[0];
        if (!IsSteamId(sid))
        {
            ReplyToCommand(info, "Incorrect SteamId");
            return;
        }
        var target = XHelper.GetPlayerFromArg($"#{sid}");
        var adminSid = args[1];
        if (!_api!.HasAccess(adminSid, CommandUsage.CLIENT_AND_SERVER, "mute", "m"))
        {
            ReplyToCommand(info, "This admin haven't access to this command!");
            return;
        }
        if (!_api.HasMoreImmunity(adminSid, sid)) return;
        var time = int.Parse(args[2])*60;
        var reason = args[3];
        var name = args[4] != "-" ? args[4] : target == null ? "Undefined" : target.PlayerName;
        var serverId = Config!.ServerId;
        var newMute = new PlayerComm(
            name,
            sid,
            adminSid,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            time,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
            reason,
            serverId
        );
        Task.Run(async () =>
        {
            await _api.AddMute(adminSid, newMute);
        });
    }
    
    public static void RUnMute(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_runmute <sid> <adminSid/CONSOLE>
        var sid = args[0];
        if (!IsSteamId(sid))
        {
            ReplyToCommand(info, "Incorrect SteamId");
            return;
        }
        var adminSid = args[1];
        if (!_api!.HasAccess(adminSid, CommandUsage.CLIENT_AND_SERVER, "unmute", "m"))
        {
            ReplyToCommand(info, "This admin haven't access to this command!");
            return;
        }
        Task.Run(async () =>
        {
            await _api.UnMute(adminSid, sid);
        });
    }
    public static void RUnGag(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_rungag <sid> <adminSid/CONSOLE>
        var sid = args[0];
        if (!IsSteamId(sid))
        {
            ReplyToCommand(info, "Incorrect SteamId");
            return;
        }
        var adminSid = args[1];
        if (!_api!.HasAccess(adminSid, CommandUsage.CLIENT_AND_SERVER, "ungag", "g"))
        {
            ReplyToCommand(info, "This admin haven't access to this command!");
            return;
        }
        Task.Run(async () =>
        {
            await _api.UnGag(adminSid, sid);
        });
    }
    public static void RUnBan(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_runban <sid> <adminSid/CONSOLE>
        var sid = args[0];
        if (!IsSteamId(sid))
        {
            ReplyToCommand(info, "Incorrect SteamId");
            return;
        }
        var adminSid = args[1];
        if (!_api!.HasPermisions(adminSid, "unban", "g"))
        {
            ReplyToCommand(info, "This admin haven't access to this command!");
            return;
        }
        Task.Run(async () =>
        {
            await _api.UnBan(adminSid, sid);
        });
    }

    public static void Rename(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        var identity = args[0];
        var target = XHelper.GetPlayerFromArg(identity);
        if (target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        var adminSid = caller == null ? "CONSOLE" : caller.AuthorizedSteamID!.SteamId64.ToString();
        if (!_api!.HasMoreImmunity(adminSid, target.AuthorizedSteamID!.SteamId64.ToString()))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        var newName = string.Join(" ", args.Skip(1));
        var oldName = target.PlayerName;
        if (newName.Trim() == "") return;
        target.PlayerName = newName;
        Utilities.SetStateChanged(target, "CBasePlayerController", "m_iszPlayerName");
        _api.ERename(adminSid, XHelper.CreateInfo(target), oldName, newName);
    }
}