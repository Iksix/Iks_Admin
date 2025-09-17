using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Commands;

public static class CmdAdminManage
{
    private static AdminApi _api = Main.AdminApi!;
    private static IStringLocalizer _localizer = _api.Localizer;
    public static void Add(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var steamId = args[0];
        var name = args[1];
        var time = args[2];
        if (!int.TryParse(time, out var timeInt))
        {
            throw new ArgumentException("Time must be a number");
        }
        int? serverId = null;
        
        if (args[3] != "all")
            serverId = args[3] == "this" ? _api.ThisServer.Id : int.Parse(args[3]);

        switch (args.Count)
        {
            case 5:
                AdminManageFunctions.Add(caller, info, steamId, name, timeInt, serverId, groupName: args[4]);
                break;
            case 6:
                AdminManageFunctions.Add(caller, info, steamId, name, timeInt, serverId, flags: args[4], immunity: int.Parse(args[5]));
                break;
            default:
                throw new ArgumentException("Wrong command usage...");
        }
    }

    public static void AddFlag(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var admin = AdminUtils.ServerAdmin(args[0]);
        if (admin == null)
        {
            return;
        }
        var flags = args[1];
        AdminManageFunctions.AddFlag(caller, info, admin, flags);
    }
    public static void AddFlagOrAdmin(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // am_addflag_or_admin <steamId> <name> <time/0> <server_id/this> <flags> <immunity>
        var admin = AdminUtils.ServerAdmin(args[0]);
        if (admin == null)
        {
            Add(caller, args, info);
            return;
        }
        var flags = args[1];
        AdminManageFunctions.AddFlag(caller, info, admin, flags);
        Task.Run(async () =>
        {
            await _api.ReloadDataFromDb();
        });
    }

    public static void AddServerId(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_am_add_server_id <SteamID> <server_id/this>
        if (!int.TryParse(args[0], out var adminId))
        {
            throw new ArgumentException("adminId must be a number");
        }
        var admin = AdminUtils.Admin(adminId);
        if (admin == null)
        {
            Helper.Reply(info, "Admin not found ✖");
            return;
        }
        int? serverId = args[1] == "this" ? null : int.Parse(args[1]);
        if (serverId != null)
        {
            if (!_api.AllServers.All(x => x.Id == serverId))
            {
                caller.Print(_localizer["ActionError.ServerNotFounded"]);
                return;
            }
        }
        AdminManageFunctions.AddServerId(caller, info, admin, serverId);
    }

    public static void Warn(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_am_warn <SteamID> <time> <reason>
        var admin = caller.Admin()!;
        var targetAdmin = AdminUtils.ServerAdmin(args[0]);
        if (targetAdmin == null)
        {
            info.Reply(_localizer["ActionError.TargetNotFound"]);
            return;
        }
        if (!_api.CanDoActionWithPlayer(admin.SteamId, targetAdmin.SteamId))
        {
            info.Reply(_localizer["ActionError.NotEnoughPermissionsForAction"]);
            return;
        }

        var time = args[1];
        if (!int.TryParse(time, out var timeInt))
        {
            throw new ArgumentException("Time must be a number");
        }
        var reason = string.Join(" ", args.Skip(2));
        var warn = new Warn(admin.Id, targetAdmin.Id, timeInt, reason);
        Task.Run(async () =>
        {
            await _api.CreateWarn(warn);
        });
    }

    public static void RemoveAdmin(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        if (!int.TryParse(args[0], out var adminId)) {
            throw new ArgumentException("id must be a number");
        }
        var admin = AdminUtils.Admin(adminId);
        if (!_api.CanDoActionWithPlayer(caller.GetSteamId(), admin!.SteamId))
        {
            caller.Print(_localizer["ActionError.NotEnoughPermissionsForAction"]);
            return;
        }
        var callerAdmin = caller.Admin();
        Task.Run(async () => {
            await _api.DeleteAdmin(callerAdmin!, admin);
            Server.NextWorldUpdate(() => {
                caller.Print(_localizer["ActionSuccess.AdminDeleted"]);
            });
        });
    }

    public static void Warns(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        if (!int.TryParse(args[0], out var adminId))
        {
            throw new ArgumentException("Admin id must be a number");
        }
        var admin = AdminUtils.ServerAdmin(adminId);
        if (admin == null)
        {
            info.Reply(_localizer["ActionError.TargetNotFound"]);
            return;
        }
        MsgOther.PrintWarns(caller, admin);
    }

    public static void WarnRemove(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        if (!int.TryParse(args[0], out var warnId))
        {
            throw new ArgumentException("Admin id must be a number");
        }
        var warn = _api.Warns.FirstOrDefault(warn => warn.Id == warnId);
        if (warn == null)
        {
            info.Reply(_localizer["ActionError.WarnNotFound"]);
            return;
        }

        var admin = caller.Admin()!;
        Task.Run(async () =>
        {
            var result = await _api.DeleteWarn(admin, warn);
        });
    }

    public static void List(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        List<Admin> admins;
        if (caller != null)
        {
            caller.Print("Check info in console...");
        }
        if (args.Count > 0 && args[0] == "all")
        {
            admins = _api.AllAdmins;
            caller.Print("All admins:", toConsole: true);
        }
        else
        {
            admins = _api.ServerAdmins.Values.ToList();
            caller.Print("Server admins:", toConsole: true);
        }
        caller.Print("id | name | steamId | flags | immunity | group | serverIds | discord | vk | isDisabled", toConsole: true);
        caller.Print("Admins:" + admins.Count, toConsole: true);
        foreach (var admin in admins)
        {
            caller.Print($"{admin.Id} | {admin.Name} | {admin.SteamId} | {admin.CurrentFlags} | {admin.CurrentImmunity} | {admin.Group?.Name ?? "NONE"} | {string.Join(";", admin.Servers)} | {admin.Discord ?? "NONE"} | {admin.Vk ?? "NONE"} | {admin.IsDisabled}",
                toConsole: true);
        }
    }
}