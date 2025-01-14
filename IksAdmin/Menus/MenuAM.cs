using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class MenuAM
{
    static IIksAdminApi _api = Main.AdminApi;
    static IStringLocalizer _localizer = _api.Localizer;
    public static Dictionary<Admin, Admin> AddAdminBuffer = new();
    public static Dictionary<Admin, Admin> EditAdminBuffer = new();
    public static Dictionary<Admin, List<int>> EditAdminServerIdBuffer = new();

    public static void OpenServersManageMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        
        var menu = _api.CreateMenu(
            Main.MenuId("ac"),
            _localizer["MenuTitle.SM"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        menu.AddMenuOption("am", _localizer["MenuOption.AM"], (_, _) => { 
            OpenAdminManageMenuSection(caller, menu);
        }, 
        viewFlags: AdminUtils.GetAllPermissionGroupFlags("admins_manage"));

        menu.AddMenuOption("warns", _localizer["MenuOption.Warns"], (_, _) => { 
            MenuWarns.OpenMain(caller, menu);
        }, 
        viewFlags:  AdminUtils.GetCurrentPermissionFlags("admins_manage.warn_add") +
                    AdminUtils.GetCurrentPermissionFlags("admins_manage.warn_list") +
                    AdminUtils.GetCurrentPermissionFlags("admins_manage.warn_delete"));

        menu.AddMenuOption("gm", _localizer["MenuOption.GM"], (_, _) => {
            if (MenuGM.AddGroupBuffer.ContainsKey(caller.Admin()!))
            {
                MenuGM.AddGroupBuffer[caller.Admin()!] = new Group("ExampleGroup", "abc", 0);
            } else {
                MenuGM.AddGroupBuffer.Add(caller.Admin()!, new Group("ExampleGroup", "abc", 0));
            }
            MenuGM.OpenGroupsManageMenu(caller, menu);
        }, 
        viewFlags: AdminUtils.GetAllPermissionGroupFlags("groups_manage"));
        menu.AddMenuOption("reload", _localizer["MenuOption.SM.ReloadData"], (_, _) =>
        {
            Task.Run(async () =>
            {
                await _api.ReloadDataFromDBOnAllServers();
                caller.Print(_localizer["Message.SM.DataReloaded"]);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("servers_manage.reload_data"));
        menu.AddMenuOption("rcon", _localizer["MenuOption.SM.Rcon"], (_, _) =>
        {
            MenuSM.OpenRconMenu(caller, menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("servers_manage.rcon"));
        menu.Open(caller);
    }

    public static void OpenAdminManageMenuSection(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.MenuId("am"),
            _localizer["MenuTitle." + "AM"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );

        menu.AddMenuOption("add", _localizer["MenuOption.AM.Add"], (_, _) =>
        {
            MenuUtils.OpenSelectPlayer(caller, "am_add", (t, m) =>
            {
                OpenAdminAddMenu(caller, t, m);
            }, backMenu: menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("admins_manage.add"));
        menu.AddMenuOption("edit_this_server", _localizer["MenuOption.AM.Edit.ThisServer"], (_, _) =>
        {
            MenuUtils.SelectItem<Admin?>(caller, "am_edit", "Name", _api.ServerAdmins!, (t, m) =>
            {
                var newAdmin = new Admin(t!.Id, t.SteamId, t.Name, t.Flags, t.Immunity, t.GroupId, t.Discord, t.Vk, t.Disabled, t.EndAt, t.CreatedAt, t.UpdatedAt, t.DeletedAt);
                EditAdminBuffer[caller.Admin()!] = newAdmin;
                EditAdminServerIdBuffer[caller.Admin()!] = newAdmin.Servers.ToList();
                OpenAdminEditMenu(caller, newAdmin, m);
            }, backMenu: menu, nullOption: false);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("admins_manage.edit"));
        menu.AddMenuOption("edit_all", _localizer["MenuOption.AM.Edit.All"], (_, _) =>
        {
            MenuUtils.SelectItem<Admin?>(caller, "am_edit", "Name", _api.AllAdmins!, (t, m) =>
            {
                var newAdmin = new Admin(t!.Id, t.SteamId, t.Name, t.Flags, t.Immunity, t.GroupId, t.Discord, t.Vk, t.Disabled, t.EndAt, t.CreatedAt, t.UpdatedAt, t.DeletedAt);
                EditAdminBuffer[caller.Admin()!] = newAdmin;
                EditAdminServerIdBuffer[caller.Admin()!] = newAdmin.Servers.ToList();
                OpenAdminEditMenu(caller, newAdmin, m);
            }, backMenu: menu, nullOption: false);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("admins_manage.edit"));
        menu.AddMenuOption("delete", _localizer["MenuOption.AM.Delete"], (_, _) =>
        {
            MenuUtils.SelectItem<Admin?>(caller, "am_delete", "Name", _api.ServerAdmins!, (t, m) =>
            {
                var cAdmin = caller.Admin();
                Task.Run(async () =>
                {
                    await _api.DeleteAdmin(cAdmin!, t!);
                    Server.NextFrame(() =>
                    {
                        OpenAdminManageMenuSection(caller, menu);
                    });
                });
            }, nullOption: false, backMenu: menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("admins_manage.delete"));
        
        menu.Open(caller);
    }

    private static void OpenAdminEditMenu(CCSPlayerController caller, Admin? admin, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.MenuId("am_edit"),
            _localizer["MenuTitle." + "AM_edit"].AReplace(["name"], [admin!.Name]),
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        var serverIds = EditAdminServerIdBuffer[caller.Admin()!];
        menu.AddMenuOption("name", _localizer["MenuOption.AM.Name"].AReplace(["value"], [admin.Name]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption("steam_id", _localizer["MenuOption.AM.SteamId"].AReplace(["value"], [admin.SteamId]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption("server_id", _localizer["MenuOption.AM.ServerId"].AReplace(["value"], [string.Join(";", serverIds)]), (_, _) =>
        {
            OpenServerIdEditMenu(caller, admin, backMenu);
        });
        menu.AddMenuOption("flags", _localizer["MenuOption.AM.Flags"].AReplace(["value"], [admin.CurrentFlags]), (_, _) =>
        {
            caller.Print(_localizer["Message.CH.FlagsSet"]);
            _api.HookNextPlayerMessage(caller, flags =>
            {
                admin.Flags = flags;
                OpenAdminEditMenu(caller, admin, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption("immunity", _localizer["MenuOption.AM.Immunity"].AReplace(["value"], [admin.CurrentImmunity]), (_, _) =>
        {
            caller.Print(_localizer["Message.CH.ImmunitySet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (!int.TryParse(str, out var immunity))
                {
                    caller.Print(_localizer["Error.MustBeANumber"]);
                    OpenAdminEditMenu(caller, admin, backMenu);
                    return;
                }
                admin.Immunity = immunity;
                OpenAdminEditMenu(caller, admin, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption("group", _localizer["MenuOption.AM.Group"].AReplace(["value"], [admin.Group?.Name ?? ""]), (_, _) =>
        {
            var groups = _api.Groups;
            MenuUtils.SelectItem<Group?>(caller, "am_add", "Name", groups!, (g, m) =>
            {
                admin.GroupId = g?.Id ?? null;
                admin.Immunity = null;
                admin.Flags = null;
                OpenAdminEditMenu(caller, admin, backMenu);
            }, backMenu: menu);
        });
        menu.AddMenuOption("vk", _localizer["MenuOption.AM.Vk"].AReplace(["value"], [admin.Vk ?? ""]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.VkSet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (str != "-")
                    admin.Vk = str;
                else admin.Vk = null;
                OpenAdminEditMenu(caller, admin, backMenu);
            });
        });
        menu.AddMenuOption("discord", _localizer["MenuOption.AM.Discord"].AReplace(["value"], [admin.Discord ?? ""]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.DiscordSet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (str != "-")
                    admin.Discord = str;
                else admin.Discord = null;
                OpenAdminEditMenu(caller, admin, backMenu);
            });
        });
        
        menu.AddMenuOption("save", _localizer["MenuOption.AM.Save"], (_, _) =>
        {
            caller.Print(_localizer["Message.AM.AdminSave"]);
            var serverIds = EditAdminServerIdBuffer[caller.Admin()!];
            var cAdmin = caller.Admin()!;
            Task.Run(async () =>
            {
                await _api.RemoveServerIdsFromAdmin(admin.Id);
                foreach (var serverId in serverIds)
                {
                    await _api.AddServerIdToAdmin(admin.Id, serverId);
                }
                var result = await _api.UpdateAdmin(cAdmin, admin);
                if (result.QueryStatus < 0)
                {
                    caller.Print(_localizer["ActionError.Other"]);
                    AdminUtils.LogError(result.QueryMessage);
                    return;
                }
                caller.Print(_localizer["Message.AM.AdminSaved"]);
            });
        });
        menu.Open(caller);
    }

    private static void OpenServerIdEditMenu(CCSPlayerController caller, Admin admin, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.MenuId("am_edit_server_id"),
            _localizer["MenuTitle." + "AM_edit_server_id"],
            titleColor: MenuColors.Gold
        );
        var serverIds = EditAdminServerIdBuffer[caller.Admin()!];
        menu.BackAction = (_) =>
        {
            OpenAdminEditMenu(caller, admin, backMenu);
        };

        foreach (var serverId in _api.AllServers.Select(x => x.Id))
        {
            bool adminHas = serverIds.Contains(serverId);
            var server = _api.GetServerById(serverId);
            if (server == null) continue;
            menu.AddMenuOption(serverId.ToString(), $"{server.Name} {(adminHas ? "+" : "-")}",
                (p, m) =>
                {
                    if (adminHas)
                    {
                        serverIds.Remove(serverId);
                    }
                    else
                    {
                        serverIds.Add(serverId);
                    }
                    OpenServerIdEditMenu(caller, admin, backMenu);
                });
        }
        
        menu.Open(caller);
    }


    public static void OpenAdminAddMenu(CCSPlayerController caller, PlayerInfo target, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.MenuId("am_add"),
            _localizer["MenuTitle." + "AM_add"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        if (!AddAdminBuffer.ContainsKey(caller.Admin()!))
        {
            AddAdminBuffer[caller.Admin()!] = new Admin(
                target.SteamId!, 
                target.PlayerName
                );
        }

        var admin = AddAdminBuffer[caller.Admin()!];
        
        menu.AddMenuOption("name", _localizer["MenuOption.AM.Name"].AReplace(["value"], [target.PlayerName]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption("steam_id", _localizer["MenuOption.AM.SteamId"].AReplace(["value"], [target.SteamId!]), (_, _) =>
        {}, disabled: true);
        menu.AddMenuOption("flags", _localizer["MenuOption.AM.Flags"].AReplace(["value"], [admin.CurrentFlags]), (_, _) =>
        {
            caller.Print(_localizer["Message.CH.FlagsSet"]);
            _api.HookNextPlayerMessage(caller, flags =>
            {
                admin.Flags = flags;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption("immunity", _localizer["MenuOption.AM.Immunity"].AReplace(["value"], [admin.CurrentImmunity]), (_, _) =>
        {
            caller.Print(_localizer["Message.CH.ImmunitySet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (!int.TryParse(str, out var immunity))
                {
                    caller.Print(_localizer["Error.MustBeANumber"]);
                    OpenAdminAddMenu(caller, target, backMenu);
                    return;
                }
                admin.Immunity = immunity;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        }, disabled: admin.GroupId != null);
        menu.AddMenuOption("group", _localizer["MenuOption.AM.Group"].AReplace(["value"], [admin.Group?.Name ?? ""]), (_, _) =>
        {
            var groups = _api.Groups;
            MenuUtils.SelectItem<Group?>(caller, "am_add", "Name", groups!, (g, m) =>
            {
                admin.GroupId = g?.Id ?? null;
                admin.Immunity = null;
                admin.Flags = null;
                OpenAdminAddMenu(caller, target, m);
            }, backMenu: menu);
        });
        menu.AddMenuOption("vk", _localizer["MenuOption.AM.Vk"].AReplace(["value"], [admin.Vk ?? ""]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.VkSet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (str != "-")
                    admin.Vk = str;
                else admin.Vk = null;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        });
        menu.AddMenuOption("discord", _localizer["MenuOption.AM.Discord"].AReplace(["value"], [admin.Discord ?? ""]), (_, _) =>
        {
            caller.Print(_localizer["Message.AM.DiscordSet"]);
            _api.HookNextPlayerMessage(caller, str =>
            {
                if (str != "-")
                    admin.Discord = str;
                else admin.Discord = null;
                OpenAdminAddMenu(caller, target, backMenu);
            });
        });
        
        menu.AddMenuOption("save", _localizer["MenuOption.AM.Save"], (_, _) =>
        {
            caller.Print(_localizer["Message.AM.AdminSave"]);
            var cAdmin = caller.Admin()!;
            Task.Run(async () =>
            {
                var result = await _api.CreateAdmin(cAdmin, AddAdminBuffer[cAdmin!], _api.ThisServer.Id);
                if (result.QueryStatus < 0)
                {
                    caller.Print(_localizer["ActionError.Other"]);
                    AdminUtils.LogError(result.QueryMessage);
                    return;
                }
                caller.Print(_localizer["Message.AM.AdminSaved"]);
            });
        });

        menu.Open(caller);
    }

    

}