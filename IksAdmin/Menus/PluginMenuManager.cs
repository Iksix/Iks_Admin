using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class PluginMenuManager
{
    private static readonly IIksAdminApi? Api = IksAdmin.Api;
    private static readonly IStringLocalizer Localizer = Api!.Localizer;
    private static readonly PluginConfig Config = IksAdmin.ConfigNow;
    public static void OpenAdminMenu(CCSPlayerController caller)
    {
        var menu = new Menu(caller, ConstructAdminMenu);
        menu.Open(caller, Localizer["MENUTITLE_Main"]);
    }

    public static void ConstructAdminMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
        if (Api!.HasPermisions(adminSid, "blocks", "bmg"))
            menu.AddMenuOption(Localizer["MENUOPTION_Blocks"], (p, _) => { OpenBlocksMenu(p, menu); });
        if (Api.HasPermisions(adminSid, "players", "skt"))
            menu.AddMenuOption(Localizer["MENUOPTION_Players"], (p, _) => { OpenPlayersMenu(p, menu); });
        if (Api.HasPermisions(adminSid, "server", "z"))
            menu.AddMenuOption(Localizer["MENUOPTION_Server"], (p, _) => { OpenBlocksMenu(p, menu); });
        
        // Добавляем пункты из модулей
        var items = Api.ModulesOptions.Where(
            x => Api.HasPermisions(adminSid, x.FlagsAccess, x.FlagsDefault )
            && x.OptionLocation == "Main"
            );
        foreach (var item in items)
        {
            menu.AddMenuOption(item.Title, (p, _) =>
            {
                item.OnSelect!.Invoke(p, admin, menu);
            });
        }
    }

    public static void OpenBlocksMenu(CCSPlayerController caller, IMenu backMenu)
    {
        var menu = new Menu(caller, ConstructBlocksMenu);
        menu.Open(caller, Localizer["MENUTITLE_Blocks"], backMenu);
    }
    
    public static void OpenPlayersMenu(CCSPlayerController caller, IMenu backMenu)
    {
        var menu = new Menu(caller, ConstructPlayersMenu);
        menu.Open(caller, Localizer["MENUTITLE_Players"], backMenu);
    }

    private static void ConstructPlayersMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
        if (Api!.HasPermisions(adminSid, "slay", "s"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Slay"], (_, _) =>
            {   
                OpenSlayMenu(caller, menu);
            });
        }
        if (Api.HasPermisions(adminSid, "kick", "k"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Kick"], (_, _) =>
            {   
                OpenKickMenu(caller, menu);
            });
        }
        if (Api.HasPermisions(adminSid, "switchteam", "s"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_SwitchTeam"], (_, _) =>
            {   
                OpenSwitchTeamMenu(caller, menu);
            });
        }
        if (Api.HasPermisions(adminSid, "changeteam", "s"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_ChangeTeam"], (_, _) =>
            {   
                OpenChangeTeamMenu(caller, menu);
            });
        }
        if (Api.HasPermisions(adminSid, "changeteam", "s"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_ChangeTeam"], (_, _) =>
            {   
                OpenChangeTeamMenu(caller, menu);
            });
        }
        if (Api.HasPermisions(adminSid, "rename", "s"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Rename"], (_, _) =>
            {   
                OpenRenameMenu(caller, menu);
            });
        }
    }
    
    private static void OpenRenameMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, playersMenu) =>
        {
            MenuManager.CloseActiveMenu(caller);
            var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
            var player = XHelper.GetPlayerFromArg($"#{target.SteamId}");
            if (player == null) return;
            Api!.SendMessageToPlayer(caller, Localizer["NOTIFY_WriteName"]);
            Api.NextCommandAction.Add(caller, msg =>
            {
                if (!XHelper.IsControllerValid(player)) return;
                var oldName = player.PlayerName;
                player.PlayerName = msg;
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
                Api.ERename(adminSid, target, oldName, msg);
            });
        }, false, false, Localizer["MENUTITLE_Rename"]);
    }

    private static void OpenSwitchTeamMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, playersMenu) =>
        {
            OpenTeamsMenu(caller, target, playersMenu);
        }, false, false, Localizer["MENUTITLE_SwitchTeam"]);
    }
    private static void OpenChangeTeamMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, playersMenu) =>
        {
            OpenTeamsMenu(caller, target, playersMenu, true);
        }, false, false, Localizer["MENUTITLE_ChangeTeam"]);
    }

    private static void OpenTeamsMenu(CCSPlayerController caller, PlayerInfo target, IMenu backMenu, bool changeTeam = false)
    {
        Menu menu = new Menu(caller, (_, _, menu) =>
        {
            TeamsMenuConstructor(menu, caller, target, changeTeam);
        });
        menu.Open(caller, Localizer["MENUTITLE_SelectTeam"], backMenu);
    }

    private static void TeamsMenuConstructor(IMenu menu, CCSPlayerController caller, PlayerInfo target, bool changeTeam)
    {
        menu.PostSelectAction = PostSelectAction.Nothing;
        var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
        menu.AddMenuOption("To CT", (_, _) =>
        {
            var selectedPlayer = XHelper.GetPlayerFromArg($"#{target.SteamId.SteamId64}");
            if (selectedPlayer == null) return;
            var oldTeam = XHelper.GetTeamFromNum(selectedPlayer.TeamNum);
            var newTeam = CsTeam.CounterTerrorist;
            if (changeTeam)
            {
                selectedPlayer.ChangeTeam(newTeam);
                Api!.EChangeTeam(adminSid, target, oldTeam, newTeam);
            }
            else
            {
                selectedPlayer.SwitchTeam(newTeam);
                Api!.ESwitchTeam(adminSid, target, oldTeam, newTeam);
            }
        });
        menu.AddMenuOption("To T", (_, _) =>
        {
            var selectedPlayer = XHelper.GetPlayerFromArg($"#{target.SteamId.SteamId64}");
            if (selectedPlayer == null) return;
            var oldTeam = XHelper.GetTeamFromNum(selectedPlayer.TeamNum);
            var newTeam = CsTeam.Terrorist;
            if (changeTeam)
            {
                selectedPlayer.ChangeTeam(newTeam);
                Api!.EChangeTeam(adminSid, target, oldTeam, newTeam);
            }
            else
            {
                selectedPlayer.SwitchTeam(newTeam);
                Api!.ESwitchTeam(adminSid, target, oldTeam, newTeam);
            }
        });
        if (changeTeam)
        {
            menu.AddMenuOption("To SPEC", (_, _) =>
            {
                var selectedPlayer = XHelper.GetPlayerFromArg($"#{target.SteamId.SteamId64}");
                if (selectedPlayer == null) return;
                var newTeam = CsTeam.Spectator;
                var oldTeam = XHelper.GetTeamFromNum(selectedPlayer.TeamNum);
                selectedPlayer.ChangeTeam(newTeam);
                Api!.EChangeTeam(adminSid, target, oldTeam, newTeam);
            });
        }
    }

    private static void OpenSlayMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, playersMenu) =>
        {
            playersMenu.PostSelectAction = PostSelectAction.Nothing;
            var selectedPlayer = XHelper.GetPlayerFromArg($"#{target.SteamId.SteamId64}");
            if (selectedPlayer == null) return;
            selectedPlayer.CommitSuicide(true,true);
            Api!.ESlay(caller.AuthorizedSteamID!.SteamId64.ToString(), target);
        }, false, false, Localizer["MENUTITLE_Slay"], true);
    }
    
    private static void OpenKickMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReason(caller, menu, Config.KickReasons, s =>
            {
                MenuManager.CloseActiveMenu(caller);
                var selectedPlayer = XHelper.GetPlayerFromArg($"#{target.SteamId.SteamId64}");
                if (selectedPlayer == null) return;
                Server.ExecuteCommand("kickid " + selectedPlayer.UserId);
                Api!.EKick(caller.AuthorizedSteamID!.SteamId64.ToString(), target, s);
            });
        }, true, false, Localizer["MENUTITLE_Kick"]);
    }

    public static void ConstructBlocksMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
        if (Api!.HasPermisions(adminSid, "ban", "b"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Ban"], (_, _) =>
            {   
                OpenBanMenu(caller, menu);
            });
            menu.AddMenuOption(Localizer["MENUOPTION_OfflineBan"], (_, _) =>
            {
                OpenOfflineBanMenu(caller, menu);
            });
        }
        if (Api.HasPermisions(adminSid, "gag", "g"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Gag"], (_, _) =>
            {
                OpenGagMenu(caller, menu);
            });
        }
        if (Api.HasPermisions(adminSid, "mute", "m"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Mute"], (_, _) =>
            {
                OpenMuteMenu(caller, menu);
            });
        }
        if (Api.HasPermisions(adminSid, "ungag", "g"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_UnGag"], (_, _) =>
            {
                var gagMenu = new Menu(caller, ConstructUnGagMenu);
                gagMenu.Open(caller, Localizer["MENUTITLE_UnGag"], menu);
            });
        }
        if (Api.HasPermisions(adminSid, "unmute", "m"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_UnMute"], (_, _) =>
            {
                var gagMenu = new Menu(caller, ConstructUnMuteMenu);
                gagMenu.Open(caller, Localizer["MENUTITLE_UnMute"], menu);
            });
        }
        
        // Добавляем пункты из модулей
        var items = Api.ModulesOptions.Where(
            x => Api.HasPermisions(adminSid, x.FlagsAccess, x.FlagsDefault )
                 && x.OptionLocation == "ManageBlocks"
        );
        foreach (var item in items)
        {
            menu.AddMenuOption(item.Title, (p, _) =>
            {
                item.OnSelect!.Invoke(p, admin, menu);
            });
        }
    }

    private static void OpenBanMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, Config.BanReasons, (reason, time) =>
            {
                time = time * 60;
                var newBan = new PlayerBan(
                    target.PlayerName,
                    target.SteamId.SteamId64.ToString(),
                    target.IpAddress,
                    caller.AuthorizedSteamID!.SteamId64.ToString(),
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    time,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
                    reason,
                    Config.ServerId
                );
                Task.Run(async () =>
                {
                    await Api!.AddBan(newBan.AdminSid, newBan);
                });
            });
        }, true, false, Localizer["MENUTITLE_Ban"]);
    }
    
    private static void OpenOfflineBanMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, Config.BanReasons, (reason, time) =>
            {
                time = time * 60;
                var newBan = new PlayerBan(
                    target.PlayerName,
                    target.SteamId.SteamId64.ToString(),
                    target.IpAddress,
                    caller.AuthorizedSteamID!.SteamId64.ToString(),
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    time,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
                    reason,
                    Config.ServerId
                );
                Task.Run(async () =>
                {
                    await Api!.AddBan(newBan.AdminSid, newBan);
                });
            });
        }, true, true, Localizer["MENUTITLE_OfflineBan"]);
    }
    
    private static void OpenGagMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, Config.GagReasons, (reason, time) =>
            {
                time = time * 60;
                var newBan = new PlayerComm(
                    target.PlayerName,
                    target.SteamId.SteamId64.ToString(),
                    caller.AuthorizedSteamID!.SteamId64.ToString(),
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    time,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
                    reason,
                    Config.ServerId
                );
                Task.Run(async () =>
                {
                    await Api!.AddGag(newBan.AdminSid, newBan);
                });
            });
        }, true, false, Localizer["MENUTITLE_Gag"]);
    }
    private static void ConstructUnGagMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var players = XHelper.GetOnlinePlayers();
        foreach (var player in players)
        {
            var playerSid = player.AuthorizedSteamID!.SteamId64.ToString();
            var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
            if (Api!.OnlineGaggedPlayers.Any(x => x.Sid == playerSid))
            {
                menu.AddMenuOption(player.PlayerName, (_, _) =>
                {
                    Task.Run(async () =>
                    {
                        await Api.UnGag(playerSid, adminSid);
                    });
                });
            }
        }
    }
    
    private static void ConstructUnMuteMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var players = XHelper.GetOnlinePlayers();
        foreach (var player in players)
        {
            var playerSid = player.AuthorizedSteamID!.SteamId64.ToString();
            var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
            if (Api!.OnlineGaggedPlayers.Any(x => x.Sid == playerSid))
            {
                menu.AddMenuOption(player.PlayerName, (_, _) =>
                {
                    Task.Run(async () =>
                    {
                        await Api.UnGag(playerSid, adminSid);
                    });
                });
            }
        }
    }
    
    private static void OpenMuteMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, Config.MuteReasons, (reason, time) =>
            {
                time = time * 60;
                var newBan = new PlayerComm(
                    target.PlayerName,
                    target.SteamId.SteamId64.ToString(),
                    caller.AuthorizedSteamID!.SteamId64.ToString(),
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    time,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
                    reason,
                    Config.ServerId
                );
                Task.Run(async () =>
                {
                    await Api!.AddMute(newBan.AdminSid, newBan);
                });
            });
        }, true, false, Localizer["MENUTITLE_Mute"]);
    }
    
    private static void SelectReasonAndTime(CCSPlayerController caller, IMenu backMenu ,List<Reason> reasons, Action<string, int> onSelect)
    {
        Menu menu = new Menu(caller, (controller, _, arg3) => 
            SelectReasonAndTimeConstructor(controller, arg3, reasons, onSelect));
        menu.Open(caller, Localizer["MENUTITLE_Reason"], backMenu);
    }

    private static void SelectReasonAndTimeConstructor(CCSPlayerController caller, IMenu menu, 
        List<Reason> reasons, Action<string, int> onSelect)
    {
        menu.PostSelectAction = PostSelectAction.Nothing;
        foreach (var reason in reasons)
        {
            if (reason.Time >= 0)
            {
                menu.AddMenuOption(reason.Title, (_, _) =>
                {
                    onSelect.Invoke(reason.Title, (int)reason.Time);
                });
            }
            if (reason.Time == null)
            {
                SelectTime(caller, menu, time =>
                {
                    onSelect.Invoke(reason.Title, time);
                });
            }
            if (reason.Time == -1)
            {
                Api!.SendMessageToPlayer(caller, Localizer["NOTIFY_WriteReason"]);
                Api.NextCommandAction.Add(caller, s =>
                {
                    SelectTime(caller, menu, time =>
                    {
                        onSelect.Invoke(s, time);
                    });
                });
            }
        }
    }

    private static void SelectTime(CCSPlayerController caller, IMenu backMenu, Action<int> onSelect)
    {
        Menu menu = new Menu(caller, (controller, _, arg3) =>
        {
            SelectTimeConstructor(controller, arg3, onSelect);
        });
        menu.Open(caller, Localizer["MENUTITLE_Time"], backMenu);
    }
    private static void SelectTimeConstructor(CCSPlayerController caller, IMenu menu, Action<int> onSelect)
    {
        menu.PostSelectAction = PostSelectAction.Nothing;
        foreach (var time in Config.Times)
        {
            if (time.Value == -1)
            {
                menu.AddMenuOption(time.Key, (_, _) =>
                {
                    Api!.SendMessageToPlayer(caller, Localizer["NOTIFY_WriteTime"]);
                    Api.NextCommandAction.Add(caller, s =>
                    {
                        if (!int.TryParse(s, out var result))
                        {
                            Api.SendMessageToPlayer(caller, Localizer["NOTIFY_ErrorTime"]);
                        }
                        onSelect.Invoke(result);
                    });
                });
            }
            if (time.Value != -1)
            {
                menu.AddMenuOption(time.Key, (_, _) =>
                {
                    onSelect.Invoke(time.Value);
                });
            }
        }
    }

    private static void SelectReason(CCSPlayerController caller, IMenu backMenu ,List<string> reasons, Action<string> onSelect)
    {
        Menu menu = new Menu(caller, (controller, _, arg3) => 
            SelectReasonConstructor(controller, arg3, reasons, onSelect));
        menu.Open(caller, Localizer["MENUTITLE_Reason"], backMenu);
    }
    
    private static void SelectReasonConstructor(CCSPlayerController caller, IMenu menu, 
        List<string> reasons, Action<string> onSelect)
    {
        foreach (var reason in reasons)
        {
            if (reason.StartsWith("$"))
            {
                menu.AddMenuOption(reason.Remove(0, 1), (_, _) =>
                {
                    Api!.SendMessageToPlayer(caller, Localizer["NOTIFY_WriteReason"]);
                    Api.NextCommandAction.Add(caller, onSelect.Invoke);
                });
                continue;
            }
            menu.AddMenuOption(reason, (_, _) =>
            {
                onSelect.Invoke(reason);
            });
        }
    }

    private static void OpenSelectPlayersMenu(CCSPlayerController caller, IMenu backMenu, Action<PlayerInfo, IMenu> onSelect,
        bool ignoreYourself = true, bool offline = false, string? title = null, bool aliveOnly = false)
    {
        Menu menu = new Menu(caller, (controller, _, arg3) =>
        {
            ConstructSelectPlayerMenu(controller, arg3, onSelect, ignoreYourself, offline, aliveOnly);
        });
        title = title == null ? Localizer["MENUTITLE_SelectPlayer"] : title;
        menu.Open(caller, title, backMenu);
    }

    private static void ConstructSelectPlayerMenu(CCSPlayerController caller, IMenu menu, Action<PlayerInfo, IMenu> onSelect, bool ignoreYourself = true, bool offline = false, bool aliveOnly = false)
    {
        var players = Api!.DisconnectedPlayers;
        if (!offline)
        {
            players.Clear();
            foreach (var player in XHelper.GetOnlinePlayers())
            {
                if (aliveOnly && !player.PawnIsAlive) continue;
                players.Add(XHelper.CreateInfo(player));
            }
        }
        foreach (var player in players)
        {
            var playerInfo = new PlayerInfo(player.PlayerName, player.SteamId.SteamId64, player.IpAddress);
            if (!Api.HasMoreImmunity(caller.AuthorizedSteamID!.SteamId64.ToString(),
                    playerInfo.SteamId.SteamId64.ToString()))
            {
                if (playerInfo.SteamId.SteamId64 == caller.AuthorizedSteamID.SteamId64)
                {
                    if (ignoreYourself)
                    {
                        continue;
                    }
                }
                if (playerInfo.SteamId.SteamId64 != caller.AuthorizedSteamID.SteamId64)
                {
                    continue;
                }
            }
            menu.AddMenuOption(playerInfo.PlayerName, (_, _) =>
            {
                onSelect.Invoke(playerInfo, menu);
            });
        }
    }
    
}
