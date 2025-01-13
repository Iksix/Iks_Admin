using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class MenuGM
{
    private static IIksAdminApi _api = Main.AdminApi;
    private static IStringLocalizer _localizer = _api.Localizer;
    public static Dictionary<Admin, Group> AddGroupBuffer = new();
    public static Dictionary<Admin, Group> EditGroupBuffer = new();
    public static void OpenGroupsManageMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.MenuId("gm"),
            _localizer["MenuTitle.GM"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        
        menu.AddMenuOption("add", _localizer["MenuOption.GM.Add"], (_, _) => {
            OpenGroupAddMenu(caller, menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("groups_manage.add"));
        menu.AddMenuOption("edit", _localizer["MenuOption.GM.Edit"], (_, _) => {
            MenuUtils.SelectItem<Group?>(caller, "group_edit", "Name", _api.Groups!, (g, m) =>
            {
                var newGroup = new Group(g.Id, g.Name, g.Flags, g.Immunity, g.Comment);
                OpenGroupEditMenu(caller, newGroup, m);
            }, nullOption: false, backMenu: menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("groups_manage.edit"));
        menu.AddMenuOption("delete", _localizer["MenuOption.GM.Delete"], (_, _) => {
            MenuUtils.SelectItem<Group?>(caller, "group_edit", "Name", _api.Groups!, (g, m) =>
            {
                caller.Print(_localizer["Message.CH.ConfirmDeleting"]);
                caller.Print(_localizer["Message.GM.AdminsWithGroup"].AReplace(["value"], [_api.AllAdmins.Count(x => x.GroupId == g!.Id)]) );
                _api.HookNextPlayerMessage(caller, msg =>
                {
                    if (msg != "delete")
                    {
                        caller.Print(_localizer["Message.GL.DeleteCanceled"]);
                        return;
                    }
                    caller.Print(_localizer["Message.GM.Delete"]);
                    Task.Run(async () =>
                    {
                        await _api.DeleteGroup(g);
                        caller.Print(_localizer["Message.GM.Deleted"]);
                    });
                });
            }, nullOption: false, backMenu: menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("groups_manage.delete"));

        menu.Open(caller);
    }

    private static void OpenGroupEditMenu(CCSPlayerController caller, Group group, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.MenuId("gm_edit"),
            _localizer["MenuTitle.GM.Editing"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        
        menu.AddMenuOption("name", _localizer["MenuOption.GM.Name"].AReplace(["value"], [group.Name]), (_, _) => {
            caller.Print(_localizer["Message.GM.NameSet"]);
            _api.HookNextPlayerMessage(caller, msg => {
                group.Name = msg;
                OpenGroupEditMenu(caller, group, backMenu);
            });
        });
        menu.AddMenuOption("flags", _localizer["MenuOption.GM.Flags"].AReplace(["value"], [group.Flags]), (_, _) => {
            caller.Print(_localizer["Message.CH.FlagsSet"]);
            _api.HookNextPlayerMessage(caller, msg => {
                group.Flags = msg;
                OpenGroupEditMenu(caller, group, backMenu);
            });
        });
        menu.AddMenuOption("immunity", _localizer["MenuOption.GM.Immunity"].AReplace(["value"], [group.Immunity]), (_, _) => {
            caller.Print(_localizer["Message.CH.ImmunitySet"]);
            _api.HookNextPlayerMessage(caller, msg => {
                if (int.TryParse(msg, out var immunity))
                {
                    group.Immunity = immunity;
                    OpenGroupEditMenu(caller, group, backMenu);
                } else {
                    caller.Print(_localizer["Error.MustBeANumber"]);
                    OpenGroupEditMenu(caller, group, backMenu);
                }
            });
        });
        menu.AddMenuOption("comment", _localizer["MenuOption.GM.Comment"].AReplace(["value"], [group.Comment ?? ""]), (_, _) => {
            caller.Print(_localizer["Message.GM.CommentSet"]);
            _api.HookNextPlayerMessage(caller, msg => {
                group.Comment = msg;
                OpenGroupEditMenu(caller, group, backMenu);
            });
        });
        menu.AddMenuOption("save", _localizer["MenuOption.GM.Save"], (_, _) => {
            caller.Print(_localizer["Message.GM.Save"]);
            backMenu.Open(caller);
            Task.Run(async () =>
            {
                await _api.UpdateGroup(group);
                Server.NextFrame(() => {
                    caller.Print(_localizer["Message.GM.Saved"]);
                    OpenGroupEditMenu(caller, group, backMenu);
                });
            });
        });
        
        menu.Open(caller);
    }

    private static void OpenGroupAddMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(
            Main.MenuId("gm_add"),
            _localizer["MenuTitle.GM.Add"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        var group = AddGroupBuffer[caller.Admin()!];
        menu.AddMenuOption("name", _localizer["MenuOption.GM.Name"].AReplace(["value"], [group.Name]), (_, _) => {
            caller.Print(_localizer["Message.GM.NameSet"]);
            _api.HookNextPlayerMessage(caller, msg => {
                group.Name = msg;
                OpenGroupAddMenu(caller, backMenu);
            });
        });
        menu.AddMenuOption("flags", _localizer["MenuOption.GM.Flags"].AReplace(["value"], [group.Flags]), (_, _) => {
            caller.Print(_localizer["Message.CH.FlagsSet"]);
            _api.HookNextPlayerMessage(caller, msg => {
                group.Flags = msg;
                OpenGroupAddMenu(caller, backMenu);
            });
        });
        menu.AddMenuOption("immunity", _localizer["MenuOption.GM.Immunity"].AReplace(["value"], [group.Immunity]), (_, _) => {
            caller.Print(_localizer["Message.CH.ImmunitySet"]);
            _api.HookNextPlayerMessage(caller, msg => {
                if (int.TryParse(msg, out var immunity))
                {
                    group.Immunity = immunity;
                    OpenGroupAddMenu(caller, backMenu);
                } else {
                    caller.Print(_localizer["Error.MustBeANumber"]);
                    OpenGroupAddMenu(caller, backMenu);
                }
            });
        });
        menu.AddMenuOption("comment", _localizer["MenuOption.GM.Comment"].AReplace(["value"], [group.Comment ?? ""]), (_, _) => {
            caller.Print(_localizer["Message.GM.CommentSet"]);
            _api.HookNextPlayerMessage(caller, msg => {
                group.Comment = msg;
                OpenGroupAddMenu(caller, backMenu);
            });
        });
        menu.AddMenuOption("save", _localizer["MenuOption.GM.Save"], (_, _) => {
            caller.Print(_localizer["Message.GM.Save"]);
            backMenu.Open(caller);
            Task.Run(async () =>
            {
                await _api.CreateGroup(group);
                Server.NextFrame(() => {
                    caller.Print(_localizer["Message.GM.Saved"]);
                    OpenGroupAddMenu(caller, backMenu);
                });
            });
        });

        menu.Open(caller);
    }
}