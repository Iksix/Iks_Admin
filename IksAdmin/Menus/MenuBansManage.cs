using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class MenuBansManage
{
    static IIksAdminApi _api {get; set;} = Main.AdminApi;
    static IStringLocalizer _localizer {get; set;} = Main.AdminApi.Localizer;

    public static void OpenBansMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(Main.MenuId("bm.ban"), _localizer["MenuTitle.BansManage"], backMenu: backMenu);
        menu.AddMenuOption("add", _localizer["MenuOption.AddBan"], (_, _) => {
            OpenAddBanMenu(caller, menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.ban"));
        menu.AddMenuOption("add.offline", _localizer["MenuOption.AddOfflineBan"], (_, _) => {
            OpenAddOfflineBanMenu(caller, menu);
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.ban"));
        menu.AddMenuOption(Main.GenerateOptionId("bm.unban"), _localizer["MenuOption.Unban"], (_, _) => {
            Task.Run(async () => {
                var bans = await DBBans.GetLastBans(_api.Config.LastPunishmentTime);
                Server.NextFrame(() => {
                    OpenRemoveBansMenu(caller, bans, menu);
                });
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.unban") + AdminUtils.GetCurrentPermissionFlags("blocks_manage.unban_ip"));
        menu.Open(caller);
    }

    private static void OpenRemoveBansMenu(CCSPlayerController caller, List<PlayerBan> bans, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(Main.MenuId("bm_unban"), _localizer["MenuTitle.Unban"], backMenu: backMenu);
        var admin = caller.Admin()!;
        
        foreach (var ban in bans)
        {
            var disableByPerms = false;
            if (ban.BanType == 1 && !caller.HasPermissions("blocks_manage.unban_ip"))
            {
                disableByPerms = true;
            }
            if (ban.BanType == 0 && !caller.HasPermissions("blocks_manage.unban"))
            {
                disableByPerms = true;
            }
            string postfix = "";
            if (ban.IsUnbanned)
                postfix = _localizer["MenuOption.Postfix.Unbanned"];
            else if (ban.IsExpired)
                postfix = _localizer["MenuOption.Postfix.Expired"];
            menu.AddMenuOption("bm_unban_" + ban.SteamId, ban.NameString + postfix, (_, _) => {
                caller.Print(_localizer["Message.GL.ReasonSet"]);
                _api.HookNextPlayerMessage(caller, r => {
                    Task.Run(async () => {
                        if (ban.BanType == 0)
                            await _api.Unban(admin, ban.SteamId!, r);
                        else await _api.UnbanIp(admin, ban.Ip!, r);
                        var b = await DBBans.GetLastBans(_api.Config.LastPunishmentTime);
                        b.Reverse();
                        Server.NextFrame(() =>
                        {
                            OpenRemoveBansMenu(caller, b, menu);
                        });
                    });
                });
            }, disabled: !AdminUtils.CanUnban(admin, ban) || ban.IsExpired || ban.IsUnbanned || disableByPerms);
        }
        menu.Open(caller);
    }

    public static void OpenAddOfflineBanMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(Main.MenuId("bm_offline_ban_add"), _localizer["MenuTitle.AddOfflineBan"], backMenu: backMenu);
        var players = _api.DisconnectedPlayers;
        foreach (var player in players)
        {
            if (!_api.CanDoActionWithPlayer(caller.GetSteamId()!, player.SteamId!))
                continue;
            menu.AddMenuOption(Main.GenerateOptionId("bm_offline_ban_add_" + player.SteamId!), player.PlayerName, (_, _) => {
                OpenSelectReasonMenu(caller, player);
            });
        }
        menu.Open(caller);
    }
    public static void OpenAddBanMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(Main.MenuId("bm_ban_add"), _localizer["MenuTitle.AddBan"], backMenu: backMenu);
        var players = PlayersUtils.GetOnlinePlayers();
        foreach (var player in players)
        {
            if (!_api.CanDoActionWithPlayer(caller.GetSteamId()!, player.AuthorizedSteamID!.SteamId64.ToString()))
                continue;
            menu.AddMenuOption(Main.GenerateOptionId("bm_ban_add_" + player.GetSteamId()), player.PlayerName, (_, _) => {
                OpenSelectReasonMenu(caller, new PlayerInfo(player));
            });
        }
        menu.Open(caller);
    }

    private static void OpenSelectReasonMenu(CCSPlayerController caller, PlayerInfo target)
    {
        var menu = _api.CreateMenu(Main.MenuId("bm_ban_reason"), _localizer["MenuTitle.Other.SelectReason"]);
        var config = BansConfig.Config;
        var reasons = config.Reasons;
        var admin = caller.Admin()!;

        menu.AddMenuOption(Main.GenerateOptionId("own_ban_reason") ,_localizer["MenuOption.Other.OwnReason"], (_, _) => {
            Helper.Print(caller, _localizer["Message.PrintOwnReason"]);
            _api.HookNextPlayerMessage(caller, reason => {
                OpenTimeSelectMenu(caller, target, reason, menu);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.own_ban_reason"));

        foreach (var reason in reasons)
        {
            if (reason.HideFromMenu) continue;
            if (reason.Duration != null)
            {
                if (caller.Admin()!.MaxBanTime != 0)
                {
                    if (reason.Duration > caller.Admin()!.MaxBanTime)
                        continue;
                }
                if (caller.Admin()!.MinBanTime != 0)
                {
                    if (reason.Duration < caller.Admin()!.MinBanTime)
                        continue;
                }
            }

            menu.AddMenuOption(reason.Title, reason.Title, (_, _) => {
                if (reason.Duration == null)
                {
                    OpenTimeSelectMenu(caller, target, reason.Text, menu);
                }
            });
        }
        
        menu.Open(caller);
    }

    private static void OpenTimeSelectMenu(CCSPlayerController caller, PlayerInfo target, string reason, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(Main.MenuId("bm_ban_time"), _localizer["MenuTitle.Other.SelectTime"], backMenu: backMenu);
        var config = BansConfig.Config;
        var times = config.Times;
        var admin = caller.Admin()!;

        var ban = new PlayerBan(target, reason, 0, serverId: _api.ThisServer.Id);
        ban.AdminId = admin.Id;
        menu.AddMenuOption("own_ban_time" ,_localizer["MenuOption.Other.OwnTime"], (_, _) => {
            Helper.Print(caller, _localizer["Message.PrintOwnTime"]);
            _api.HookNextPlayerMessage(caller, time => {
                if (!int.TryParse(time, out var timeInt))
                {
                    Helper.Print(caller, _localizer["Error.MustBeANumber"]);
                    return;
                }
                ban.Duration = timeInt*60;
                Helper.Print(caller, _localizer["ActionSuccess.TimeSetted"]);
                OpenBanTypeSelectMenu(caller, ban);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.own_ban_time"));

        foreach (var time in times)
        {
            if (caller.Admin()!.MaxBanTime != 0)
            {
                if (time.Key > caller.Admin()!.MaxBanTime)
                    continue;
            }
            if (caller.Admin()!.MinBanTime != 0)
            {
                if (time.Key < caller.Admin()!.MinBanTime)
                    continue;
            }

            menu.AddMenuOption("ban_time_" + time.Key, time.Value, (_, _) => {
                ban.Duration = time.Key;
                OpenBanTypeSelectMenu(caller, ban);
            });
        }
        
        menu.Open(caller);
    }

    private static void OpenBanTypeSelectMenu(CCSPlayerController caller, PlayerBan ban)
    {
        var menu = _api.CreateMenu(Main.MenuId("bm_ban_type"), _localizer["MenuTitle.BanType"]);
        var admin = caller.Admin();
        menu.AddMenuOption(Main.GenerateOptionId("bm_ban_steam_id"), _localizer["MenuOption.BanSteamId"], (_, _) => {
            _api.CloseMenu(caller);
            Task.Run(async () => {
                await BansFunctions.Ban(ban);
            });
        });
            
        menu.AddMenuOption(Main.GenerateOptionId("bm_ban_ip"), _localizer["MenuOption.BanIp"], (_, _) => {
            _api.CloseMenu(caller);
            ban.BanType = 2;
            Task.Run(async () => {
                await BansFunctions.Ban(ban);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("blocks_manage.ban_ip"));
        
        menu.Open(caller);
    }
}