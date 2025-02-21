using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace IksAdmin.Functions;

public static class AdminManageFunctions
{
    public static void Add(CCSPlayerController? caller, CommandInfo info, string steamId, string name, int time, int? serverId, string? groupName = null, string? flags = null, int? immunity = null, string? discord = null, string? vk = null)
    {
        int? groupId = null;
        if (groupName != null) {
            var group = Main.AdminApi.Groups.FirstOrDefault(x => x.Name == groupName);
            if (group == null) {
                Helper.Reply(info, "Group not founded ✖");
                return;
            }
            groupId = group!.Id;
        }
        Helper.Reply(info, "Adding admin...");
        var admin = new Admin(
            steamId,
            name,
            flags,
            immunity,
            groupId,
            discord,
            vk,
            endAt: time == 0 ? null : AdminUtils.CurrentTimestamp() + time*60
        );
        Task.Run(async () =>
        {
            var newAdmin = await Main.AdminApi.CreateAdmin(caller.Admin()!, admin, serverId);
            switch (newAdmin.QueryStatus)
            {
                case 0: 
                    Helper.Reply(info, "Admin added \u2714");
                    break;
                case 1: 
                    Helper.Reply(info, "Admin !UPDATED!");
                    break;
                default:
                    Helper.Reply(info, "Unexpected query status! Please check server console!");
                    AdminUtils.LogError(newAdmin.QueryMessage);
                    break;
            }
        });
    }

    public static void AddFlag(CCSPlayerController? caller, CommandInfo info, Admin admin, string flags)
    {
        if (admin.Flags == null)
        {
            admin.Flags = flags;
        } else{
            admin.Flags += flags;
        }
        Helper.Reply(info, "Flags setted to admin ✔");
        Task.Run(async () => {
            await DBAdmins.UpdateAdminInBase(admin);
            await Main.AdminApi!.SendRconToAllServers("css_am_reload_admins");
        });
    }

    public static void AddServerId(CCSPlayerController? caller, CommandInfo info, Admin admin, int? serverId)
    {
        Task.Run(async () => {
            if (serverId != null)
            {
                await DBAdmins.AddServerIdToAdmin(admin.Id, (int)serverId);
            } else {
                await DBAdmins.AddServerIdToAdmin(admin.Id, Main.AdminApi.ThisServer.Id);
            }
            await AdminUtils.CoreApi.ReloadDataFromDb();
            Server.NextFrame(() => {
                Helper.Reply(info, "Server Id added to admin ✔");
            });
        });
    }
}