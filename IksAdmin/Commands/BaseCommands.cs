using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using IksAdmin.Menus;
using IksAdminApi;
using MenuManager;
using Microsoft.Extensions.Localization;
using MySqlConnector;

namespace IksAdmin.Commands;

public class BaseCommands
{
    private static IIksAdminApi? _api = IksAdmin.Api;
    private static IStringLocalizer _localizer = _api!.Localizer;
    public static PluginConfig? Config;
    public static List<CCSPlayerController> HidenPlayers = new();
     

    private static void ActionWithCt(Action<CCSPlayerController> action)
    {
        var players = Utilities.GetPlayers().Where(x => x is { TeamNum: 3, IsValid: true, IsBot: false });
        foreach (var p in players)
        {
            action.Invoke(p);
        }
    }
    private static void ActionWithT(Action<CCSPlayerController> action)
    {
        var players = Utilities.GetPlayers().Where(x => x is { TeamNum: 2, IsValid: true, IsBot: false });
        foreach (var p in players)
        {
            action.Invoke(p);
        }
    }
    private static void ActionWithSpec(Action<CCSPlayerController> action)
    {
        var players = Utilities.GetPlayers().Where(x => x is { TeamNum: 1, IsValid: true, IsBot: false });
        foreach (var p in players)
        {
            action.Invoke(p);
        }
    }
    private static void ActionWithAll(Action<CCSPlayerController> action)
    {
        var players = Utilities.GetPlayers().Where(x => x is { IsValid: true, IsBot: false });
        foreach (var p in players)
        {
            action.Invoke(p);
        }
    }

    private static void DoAction(string key, string arg, Action<CCSPlayerController> action)
    {
        if (Config!.BlockMassTargets.Contains(key))
        {
            throw new Exception("Mass targets blocked for this command!");
        }
        switch (arg)
        {
            case "@all":
                ActionWithAll(action);
            break;
            case "@ct":
                ActionWithCt(action);
            break;
            case "@t":
                ActionWithT(action);
            break;
            case "@spec":
                ActionWithSpec(action);
            break;
            default:
                throw new Exception("Mass targets identity's: @all, @ct, @t, @spec");
        }
    }
    

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
        BaseMenus.OpenAdminMenu(caller);
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
        int duration = int.Parse(args[1])*60;
        string reason = args[2];
        string adminSid = caller.GetSteamId();
        if (identity.StartsWith("@"))
        {
            DoAction("ban", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                {
                    return;
                }
                var name = target.PlayerName;
                var newBan = new PlayerBan(
                    target.PlayerName,
                    target.AuthorizedSteamID!.SteamId64.ToString(),
                    target.IpAddress!,
                    caller.GetSteamId(),
                    caller.GetName(),
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
            });
            return;
        }
        var target = XHelper.GetPlayerFromArg(identity);
        if (XHelper.GetIdentityType(identity) != "sid" && target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        string sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Replace("#", "");
        if (!_api!.HasMoreImmunity(adminSid, sid))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        string ip = target != null ? target.IpAddress! : "Undefined";
        string name = args.Count > 3 ? string.Join(" ", args.Skip(3)) :
            target == null ? "Undefined" : target.PlayerName;
        var newBan = new PlayerBan(
            name,
            sid,
            ip,
            adminSid,
            caller.GetName(),
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
        string adminSid = caller.GetSteamId();
        int duration = int.Parse(args[1])*60;
        string reason = args[2];
        if (identity.StartsWith("@"))
        {
            DoAction("gag", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                {
                    return;
                }
                var name = target.PlayerName;
                var newGag = new PlayerComm(
                    target.PlayerName,
                    target.AuthorizedSteamID!.SteamId64.ToString(),
                    caller.GetSteamId(),
                    caller.GetName(),
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
            });
            return;
        }
        var target = XHelper.GetPlayerFromArg(identity);
        if (XHelper.GetIdentityType(identity) != "sid" && target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        string sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Replace("#", "");
        if (!_api!.HasMoreImmunity(adminSid, sid))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        string name = args.Count > 3 ? string.Join(" ", args.Skip(3)) :
            target == null ? "Undefined" : target.PlayerName;
        var newGag = new PlayerComm(
            name,
            sid,
            adminSid,
            caller.GetName(),
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
        string adminSid = caller.GetSteamId();
        int duration = int.Parse(args[1])*60;
        string reason = args[2];
        if (identity.StartsWith("@"))
        {
            DoAction("mute", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                {
                    return;
                }
                var name = target.PlayerName;
                var newMute = new PlayerComm(
                    target.PlayerName,
                    target.AuthorizedSteamID!.SteamId64.ToString(),
                    caller.GetSteamId(),
                    caller.GetName(),
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    duration,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + duration,
                    reason,
                    Config!.ServerId
                );
                Task.Run(async () =>
                {
                    var banStatus = await _api.AddMute(adminSid, newMute);
                    if (banStatus)
                        ReplyToCommand(info, _localizer["NOTIFY_OnGag"].Value.Replace("{name}", name), $"Mute added to player!");
                    else ReplyToCommand(info, _localizer["NOTIFY_PlayerAlreadyPunished"], $"The player has already been punished!");
                });
            });
            return;
        }
        var target = XHelper.GetPlayerFromArg(identity);
        if (XHelper.GetIdentityType(identity) != "sid" && target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        string sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Replace("#", "");
        if (!_api!.HasMoreImmunity(adminSid, sid))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        string name = args.Count > 3 ? string.Join(" ", args.Skip(3)) :
            target == null ? "Undefined" : target.PlayerName;
        var newGag = new PlayerComm(
            name,
            sid,
            adminSid,
            caller.GetName(),
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
        var adminSid = caller.GetSteamId();
        if (identity.StartsWith("@"))
        {
            DoAction("unmute", identity, target =>
            {
                var sid = target.GetSteamId();
                var name = target.PlayerName;
                Task.Run(async () =>
                {
                    var oldPunishment = await _api!.UnMute(sid, adminSid);
                    if (oldPunishment == null)
                    {
                        return;
                    }
                    ReplyToCommand(info, _localizer["NOTIFY_OnUnMute"].Value.Replace("{name}", name), "Player unmuted!");
                });
            });
            return;
        }
        var target = XHelper.GetPlayerFromArg(identity);
        if (target == null && XHelper.GetIdentityType(identity) != "sid")
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }

        var sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Remove(0, 1);

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
        var adminSid = caller.GetSteamId();

        if (identity.StartsWith("@"))
        {
            DoAction("ungag", identity, target =>
            {
                var sid = target.GetSteamId();
                Task.Run(async () =>
                {
                    var oldPunishment = await _api!.UnGag(sid, adminSid);
                    if (oldPunishment == null)
                    {
                        return;
                    }
                    ReplyToCommand(info, _localizer["NOTIFY_OnUnGag"].Value.Replace("{name}", oldPunishment.Name), "Player ungagged!");
                });
            });
            return;
        }
        var target = XHelper.GetPlayerFromArg(identity);
        if (target == null && XHelper.GetIdentityType(identity) != "sid")
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        var sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Remove(0, 1);
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
        // css_unban <sid/ip>
        var sid = args[0];
        if (!IsSteamId(sid) && !sid.Contains("."))
        {
            ReplyToCommand(info, _localizer["NOTIFY_IncorrectSid"], "Incorrect SteamId64!");
            return;
        }
        var adminSid = caller.GetSteamId();

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
        string adminSid = caller.GetSteamId();
        var identity = args[0];
        var reason = string.Join(" ", args.Skip(1));
        if (reason.Trim() == "")
        {
            ReplyToCommand(info, _localizer["NOTIFY_ErrorReason"], "Reason can't be void");
            return;
        }
        if (identity.StartsWith("@"))
        {
            DoAction("kick", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                    return;
                var targetInfo = XHelper.CreateInfo(target);
                target.Kick();
                _api!.EKick(adminSid, targetInfo, reason);
            });
            return;
        }
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
        var targetInfo = XHelper.CreateInfo(target);
        target.Kick();
        
        _api.EKick(adminSid, targetInfo, reason);
    }
    public static void Slay(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        string adminSid = caller.GetSteamId();
        var identity = args[0];
        if (identity.StartsWith("@"))
        {
            DoAction("slay", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                    return;
                var targetInfo = XHelper.CreateInfo(target);
                target.CommitSuicide(true, true);
                _api.ESlay(adminSid, targetInfo);
                BaseMessages.SERVER_SLAY(adminSid, targetInfo);
            });
            return;
        }
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
        var targetInfo = XHelper.CreateInfo(target);
        target.CommitSuicide(true, true);
        _api.ESlay(adminSid, targetInfo);
        BaseMessages.SERVER_SLAY(adminSid, targetInfo);
    }

    public static void SwitchTeam(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        string adminSid = caller.GetSteamId();
        var identity = args[0];
        var team = args[1];
        if (identity.StartsWith("@"))
        {
            DoAction("switchteam", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                    return;
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
            });
            return;
        }
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
        string adminSid = caller.GetSteamId();
        var identity = args[0];
        var team = args[1];
        if (identity.StartsWith("@"))
        {
            DoAction("switchteam", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                    return;
                var oldTeam = XHelper.GetTeamFromNum(target.TeamNum);
                var newTeam = XHelper.GetTeamFromString(team);
                if (newTeam == CsTeam.None)
                {
                    ReplyToCommand(info, _localizer["NOTIFY_IncorrectTeam"], "You can use only <t/ct/spec> for team");
                    return;
                }
                target.ChangeTeam(newTeam);
                _api.EChangeTeam(adminSid, XHelper.CreateInfo(target), oldTeam, newTeam);
            });
            return;
        }
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
            _api!.GetAdmin(target);
        if (existingAdmin == null)
        {
            ReplyToCommand(info, $"Zero permissions! \n Name: {target.PlayerName} \n SteamId: {target.AuthorizedSteamID!.SteamId64}");
            return;
        }

        var adminData = AdminManager.GetPlayerAdminData(new SteamID(ulong.Parse(existingAdmin.SteamId)));


        var groupInfo = existingAdmin.GroupId != -1 ? $"\n Group Name: {existingAdmin.GroupName} \n GroupId: {existingAdmin.GroupId} \n CssGroups: {string.Join(", ", adminData!.Groups)}" : "";
        ReplyToCommand(info, $"Name: {existingAdmin.Name}\n Flags: {existingAdmin.Flags} {groupInfo} \n Immunity: {existingAdmin.Immunity}\nCS# immunity: {AdminManager.GetPlayerImmunity(new SteamID(ulong.Parse(existingAdmin.SteamId)))} \n SteamId: {existingAdmin.SteamId} \n ServerId: {existingAdmin.ServerId} ");
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
        caller.GetName(),
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
            caller.GetName(),
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
            caller.GetName(),
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
        var newName = string.Join(" ", args.Skip(1));
        if (newName.Trim() == "") return;
        var adminSid = caller.GetSteamId();
        if (identity.StartsWith("@"))
        {
            DoAction("rename", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                    return;
                var oldName = target.PlayerName;
                target.PlayerName = newName;
                Utilities.SetStateChanged(target, "CBasePlayerController", "m_iszPlayerName");
                _api.ERename(adminSid, XHelper.CreateInfo(target), oldName, newName);
            });
            return;
        }
        var target = XHelper.GetPlayerFromArg(identity);
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
        var oldName = target.PlayerName;
        target.PlayerName = newName;
        Utilities.SetStateChanged(target, "CBasePlayerController", "m_iszPlayerName");
        _api.ERename(adminSid, XHelper.CreateInfo(target), oldName, newName);
    }

    public static void Hide(CCSPlayerController caller, Admin? admin, List<string> args, CommandInfo info)
    {
        if (HidenPlayers.Contains(caller))
        {
            HidenPlayers.Remove(caller);
            ReplyToCommand(info, _localizer["NOTIFY_OffHide"] ,"Now you are not hidden!");
            caller.ChangeTeam(CsTeam.Spectator);
            return;
        }
        HidenPlayers.Add(caller);
        Server.ExecuteCommand("sv_disable_teamselect_menu 1");
        if (caller.PlayerPawn.Value != null && caller.PawnIsAlive)
            caller.PlayerPawn.Value.CommitSuicide(true, false);
        _api!.Plugin.AddTimer(1.0f, () => { Server.NextFrame(() => caller.ChangeTeam(CsTeam.Spectator)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        _api.Plugin.AddTimer(1.4f, () => { Server.NextFrame(() => caller.ChangeTeam(CsTeam.None)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        ReplyToCommand(info, _localizer["NOTIFY_OnHide"] ,"Now you are hidden!");
        _api.Plugin.AddTimer(2.0f, () => { Server.NextFrame(() => Server.ExecuteCommand("sv_disable_teamselect_menu 0")); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

    }

    public static void Rcon(CCSPlayerController caller, Admin? admin, List<string> args, CommandInfo info)
    {
        var command = string.Join(" ", args);
        Server.ExecuteCommand(command);
        ReplyToCommand(info, "Rcon command executed!");
    }

    public static void Map(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        var adminSid = caller.GetSteamId();
        var map = args[0];
        var isWorkshop = args.Count > 1 ? args[1] : "false";
        if (isWorkshop == "true")
        {
            Server.ExecuteCommand($"host_workshop_map {map}");
            var newMap = new Map(map, map, true);
            _api!.EChangeMap(adminSid, newMap);
        }
        else
        {
            Server.ExecuteCommand($"map {map}");
            var newMap = new Map(map, map, false);
            _api!.EChangeMap(adminSid, newMap);
        }
    }

    public static void DbUpdate(CCSPlayerController arg1, Admin? arg2, List<string> arg3, CommandInfo info)
    {
        Task.Run(async () =>
        {
            try
            {
                await using var conn = new MySqlConnection(_api!.DbConnectionString);
                await conn.OpenAsync();
                await conn.QueryAsync(@"
                ALTER TABLE `iks_bans` ADD `adminName` VARCHAR(64) NOT NULL AFTER `adminsid`;
                ALTER TABLE `iks_bans` CHANGE `server_id` `server_id` VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT ''; 
                ALTER TABLE `iks_gags` ADD `adminName` VARCHAR(64) NOT NULL AFTER `adminsid`;
                ALTER TABLE `iks_gags` CHANGE `server_id` `server_id` VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT ''; 
                ALTER TABLE `iks_mutes` ADD `adminName` VARCHAR(64) NOT NULL AFTER `adminsid`;
                ALTER TABLE `iks_mutes` CHANGE `server_id` `server_id` VARCHAR(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT ''; 
                ALTER TABLE `iks_admins` CHANGE `server_id` `server_id` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '1'; 
                ");
                foreach (var admin in _api.AllAdmins)
                {
                    await conn.QueryAsync("""
                    update iks_bans set adminName = @name where adminsid = @sid;
                    update iks_gags set adminName = @name where adminsid = @sid;
                    update iks_mutes set adminName = @name where adminsid = @sid;
                    """, new {name = admin.Name, sid = admin.SteamId});
                }
                await conn.QueryAsync("""
                                      update iks_bans set adminName = @name where adminsid = @sid;
                                      update iks_gags set adminName = @name where adminsid = @sid;
                                      update iks_mutes set adminName = @name where adminsid = @sid;
                                      """, new {name = "CONSOLE", sid = "CONSOLE"});
                ReplyToCommand(info, "DB UPDATED TO VER 2.1.1");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    public static void GroupAdd(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        var name = args[0];
        var flags = args[1];
        var immunity = args[2];
        var group = new Group(name, flags, int.Parse(immunity));
        Task.Run(async () =>
        {
            await _api!.AddGroup(group);
            var newGroup = await _api.GetGroup(group.Name);
            ReplyToCommand(info, $"New group added! \nName: {newGroup.Name}\nFlags: {newGroup.Flags}\nImmunity: {newGroup.Immunity}\nId: {newGroup.Id}");
        });
    }
    public static void GroupDel(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        var name = args[0];
        Task.Run(async () =>
        {
            await _api!.DeleteGroup(name);
            ReplyToCommand(info, $"Group {name} deleted");
        });
    }
    public static void GroupList(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        Task.Run(async () =>
        {
            var groups = await _api!.GetAllGroups();
            ReplyToCommand(info, $"Id | Name | Flags | Immunity");
            foreach (var group in groups.ToList())
            {
                ReplyToCommand(info, $"{group.Id} | {group.Name} | {group.Flags} | {group.Immunity}");
            }
        });
    }

    public static void BanIp(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_banip <$ip/#uid/#sid/name> <duration> <reason> <name if needed>
        var identity = args[0];
        var duration = int.Parse(args[1]);
        var reason = args[2];
        var name = args.Count() < 4 ? "Undefined" : string.Join(" ", args.Skip(3));
        var target = XHelper.GetPlayerFromArg(identity);
        var ip = "0.0.0.0";
        var sid = "00000000000000000";
        if (identity.StartsWith("$"))
        {
            ip = identity.Remove(0, 1);
        }
        else
        {
            if (target == null)
            {
                ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
                return;
            }
            ip = target.GetIp();
            sid = target.GetSteamId();
        }

        if (name == "Undefined" && target != null)
        {
            name = target.PlayerName;
        }
            
        var ban = new PlayerBan(
            name,
            sid,
            ip,
            caller.GetSteamId(),
            caller.GetName(),
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            duration*60,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + duration*60,
            reason,
            _api!.Config.ServerId,
            1
        );
        Task.Run(async () =>
        {
            var banStatus = await _api.AddBan(ban.AdminSid, ban);
            if (banStatus)
                ReplyToCommand(info, _localizer["NOTIFY_OnBan"].Value.Replace("{name}", name), $"Ban added to player!");
            else ReplyToCommand(info, _localizer["NOTIFY_PlayerAlreadyPunished"], $"The player has already been punished!");
        });
    }

    public static void Silence(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_silence <#uid/#sid/name> <duration> <reason> <name if needed>
        string identity = args[0];
        string adminSid = caller.GetSteamId();
        int duration = int.Parse(args[1])*60;
        string reason = args[2];
        if (identity.StartsWith("@"))
        {
            DoAction("silence", identity, target =>
            {
                if (!_api!.HasMoreImmunity(caller.GetSteamId(), target.GetSteamId()))
                {
                    return;
                }
                var name = target.PlayerName;
                var newGag = new PlayerComm(
                    target.PlayerName,
                    target.AuthorizedSteamID!.SteamId64.ToString(),
                    caller.GetSteamId(),
                    caller.GetName(),
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    duration,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + duration,
                    reason,
                    Config!.ServerId
                );
                Task.Run(async () =>
                {
                    ReplyToCommand(info, _localizer["NOTIFY_OnSilence"].Value.Replace("{name}", name), $"Silence added to player");
                    await _api.AddGag(adminSid, newGag);
                    await _api.AddMute(adminSid, newGag);
                });
            });
            return;
        }
        var target = XHelper.GetPlayerFromArg(identity);
        if (XHelper.GetIdentityType(identity) != "sid" && target == null)
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        string sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Replace("#", "");
        if (!_api!.HasMoreImmunity(adminSid, sid))
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerHaveBiggerImmunity"], "Target have >= immunity then yours");
            return;
        }
        string name = args.Count > 3 ? string.Join(" ", args.Skip(3)) :
            target == null ? "Undefined" : target.PlayerName;
        var newGag = new PlayerComm(
            name,
            sid,
            adminSid,
            caller.GetName(),
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            duration,
            (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + duration,
            reason,
            Config!.ServerId
        );
        Task.Run(async () =>
        {
            ReplyToCommand(info, _localizer["NOTIFY_OnSilence"].Value.Replace("{name}", name), $"Silence added to player");
            if (_api.HasPermisions(adminSid, "gag", "g")) await _api.AddGag(adminSid, newGag);
            if (_api.HasPermisions(adminSid, "mute", "m")) await _api.AddMute(adminSid, newGag);
        });
    }
    public static void UnSilence(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        // css_unsilence <#uid/#sid/name>
        var identity = args[0];
        var adminSid = caller.GetSteamId();

        if (identity.StartsWith("@"))
        {
            DoAction("unsilence", identity, target =>
            {
                var sid = target.GetSteamId();
                Task.Run(async () =>
                {
                    if (_api.HasPermisions(adminSid, "ungag", "g")) await _api!.UnGag(sid, adminSid);
                    if (_api.HasPermisions(adminSid, "unmute", "m")) await _api!.UnMute(sid, adminSid);
               
                    ReplyToCommand(info, _localizer["NOTIFY_OnUnSilence"], "Player unsilenced!");
                });
            });
            return;
        }
        var target = XHelper.GetPlayerFromArg(identity);
        if (target == null && XHelper.GetIdentityType(identity) != "sid")
        {
            ReplyToCommand(info, _localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        var sid = target != null ? target.AuthorizedSteamID!.SteamId64.ToString() : identity.Remove(0, 1);
        Task.Run(async () =>
        {
            if (_api!.HasPermisions(adminSid, "ungag", "g")) await _api!.UnGag(sid, adminSid);
            if (_api.HasPermisions(adminSid, "unmute", "m")) await _api!.UnMute(sid, adminSid);
               
            ReplyToCommand(info, _localizer["NOTIFY_OnUnSilence"], "Player unsilenced!");
        });
    }
}