using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class CmdBansCmdRCommands
{
    public static void RBan(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_rban <steamId(admin)> <steamId(target)> <ip/-> <time> <type(0/1/2)> <reason>
        var adminId = args[0];
        var admin = AdminUtils.Admin(adminId);
        var steamId = args[1];
        if (!AdminUtils.CoreApi.CanDoActionWithPlayer(adminId, steamId))
        {
            info.Reply("Player do not have access to use this command for this target.");
            return;
        }
        if (admin == null)
        {
            info.Reply("Player do not have access to use this command.");
            return;
        }
        if (!admin.HasPermissions("blocks_manage.ban"))
        {
            info.Reply("Player do not have access to use this command.");
            return;
        }
        var ip = args[2] == "-" ? null : args[2];
        var time = int.Parse(args[3]);
        var type = int.Parse(args[4]);
        var reason = args[5];
        if (!BansConfig.HasReason(reason) && !admin.HasPermissions("blocks_manage.own_ban_reason"))
        {
            info.Reply("Player do not have access to use this command with this reason.");
            return;
        }
        if (!BansConfig.HasTime(time) && !admin.HasPermissions("blocks_manage.own_ban_time"))
        {
            info.Reply("Player do not have access to use this command with this time.");
            return;
        }
        var announce = args[6] == "true";
        
        var targetController = PlayersUtils.GetControllerBySteamId(steamId);
        string name = targetController?.PlayerName ?? "";
        var ban = new PlayerBan(
            steamId,
            ip,
            name,
            reason,
            time,
            serverId: Main.AdminApi.ThisServer.Id,
            banType: (sbyte)type
        );
        if (BansConfig.Config.BanOnAllServers) {
            ban.ServerId = null;
        }
        ban.AdminId = admin.Id;
        Task.Run(async () => {
            if (name == "")
            {
                var summ = await AdminUtils.CoreApi.GetPlayerSummaries(ulong.Parse(steamId));
                if (summ != null)
                {
                    name = summ.PersonaName;
                }
            }
            await AdminUtils.CoreApi.AddBan(ban, announce);
        });
    }
    public static void RComm(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_rcomm <steamId(admin)> <steamId(target)> <ip/-> <time> <type(0/1/2)> <reason>
        var adminId = args[0];
        var admin = AdminUtils.Admin(adminId);
        var steamId = args[1];
        if (!AdminUtils.CoreApi.CanDoActionWithPlayer(adminId, steamId))
        {
            info.Reply("Player do not have access to use this command for this target.");
            return;
        }
        if (admin == null)
        {
            info.Reply("Player do not have access to use this command.");
            return;
        }
        
        var ip = args[2] == "-" ? null : args[2];
        var time = int.Parse(args[3]);
        var type = int.Parse(args[4]);
        var reason = args[5];
        var permission = type switch
        {
            0 => "mute",
            1 => "gag",
            2 => "silence",
            _ => throw new ArgumentException("Invalid type")
        };
        bool hasReason = type switch
        {
            0 => MutesConfig.HasReason(reason),
            1 => GagsConfig.HasReason(reason),
            2 => SilenceConfig.HasReason(reason),
            _ => throw new ArgumentException("Invalid type")
        };
        bool hasTime = type switch
        {
            0 => MutesConfig.HasTime(time),
            1 => GagsConfig.HasTime(time),
            2 => SilenceConfig.HasTime(time),
            _ => throw new ArgumentException("Invalid type")
        };
        if (!admin.HasPermissions($"comms_manage.{permission}"))
        {
            info.Reply("Player do not have access to use this command.");
            return;
        }
        if (!hasReason && !admin.HasPermissions($"comms_manage.own_{permission}_reason"))
        {
            info.Reply("Player do not have access to use this command with this reason.");
            return;
        }
        if (!hasTime && !admin.HasPermissions($"comms_manage.own_{permission}_time"))
        {
            info.Reply("Player do not have access to use this command with this time.");
            return;
        }
        var announce = args[6] == "true";
        
        var targetController = PlayersUtils.GetControllerBySteamId(steamId);
        string name = targetController?.PlayerName ?? "";
        var comm = new PlayerComm(
            steamId,
            ip,
            name,
            (PlayerComm.MuteTypes)type,
            reason,
            time,
            serverId: Main.AdminApi.ThisServer.Id
        );
        if (type == 0 && MutesConfig.Config.BanOnAllServers) {
            comm.ServerId = null;
        } else if (type == 1 && GagsConfig.Config.BanOnAllServers) {
            comm.ServerId = null;
        } else if (type == 2 && SilenceConfig.Config.BanOnAllServers) {
            comm.ServerId = null;
        }
        comm.AdminId = admin.Id;
        Task.Run(async () => {
            if (name == "")
            {
                var summ = await AdminUtils.CoreApi.GetPlayerSummaries(ulong.Parse(steamId));
                if (summ != null)
                {
                    name = summ.PersonaName;
                }
            }
            await AdminUtils.CoreApi.AddComm(comm, announce);
        });
    }
}