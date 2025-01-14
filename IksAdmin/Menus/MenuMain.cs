using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class MenuMain
{
    private static IIksAdminApi _api = Main.AdminApi;
    private static IStringLocalizer _localizer = _api.Localizer;
   
    public static void OpenAdminMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null) {
        _api.RemoveNextPlayerMessageHook(caller);
        var menu = _api.CreateMenu(
            id: Main.MenuId("main"),
            title: _localizer["MenuTitle.AdminMain"],
            backMenu: backMenu
        );
        menu.AddMenuOption(
            id: "sm",
            title: _localizer["MenuOption.SM"],
            (p, _) => {
                MenuAM.OpenServersManageMenu(caller, menu);
            },
            viewFlags: AdminUtils.GetAllPermissionGroupFlags("admins_manage") 
                       + AdminUtils.GetAllPermissionGroupFlags("groups_manage") 
                       + AdminUtils.GetAllPermissionGroupFlags("servers_manage")
        );
        menu.AddMenuOption(
            id: "pm",
            title: _localizer["MenuOption.PM"],
            (p, _) => {
                MenuPM.OpenMain(caller, menu);
            },
            viewFlags: AdminUtils.GetAllPermissionGroupFlags("players_manage")
        );
        menu.AddMenuOption(
            id: "bm",
            title: _localizer["MenuOption.BM"],
            (p, _) => {
                OpenBlocksManageMenu(caller, menu);
            },
            viewFlags: AdminUtils.GetAllPermissionGroupFlags("blocks_manage") + AdminUtils.GetAllPermissionGroupFlags("comms_manage")
        );
        menu.Open(caller);
    }

    public static void OpenBlocksManageMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(
            id: Main.MenuId("bm"),
            title: _localizer["MenuTitle.BM"],
            backMenu: backMenu
        );
        menu.AddMenuOption(
            id: Main.GenerateOptionId("bans"),
            title: _localizer["MenuOption.BansManage"],
            (p, _) => {
                MenuBansManage.OpenBansMenu(caller, menu);
            }
        );
        menu.AddMenuOption(
            id: "cm",
            title: _localizer["MenuOption.CM"],
            (p, _) => {
                MenuCommsManage.OpenCommsMenu(caller, menu);
            },
            viewFlags: AdminUtils.GetAllPermissionGroupFlags("comms_manage")
        );
        menu.Open(caller);
    }
}