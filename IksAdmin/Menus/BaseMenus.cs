using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdminApi;
using MenuManager;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class BaseMenus
{
    private static readonly IIksAdminApi Api = IksAdmin.Api!;
    private static readonly IStringLocalizer Localizer = Api.Localizer;
    private static readonly IPluginCfg Config = Api.Config;
    private static IMenuApi _menuManager = IksAdmin.MenuManager!;
    public static void OpenAdminMenu(CCSPlayerController caller)
    {
        var menu = new Menu(ConstructAdminMenu);
        menu.Open(caller, Localizer["MENUTITLE_Main"]);
    }

    public static void ConstructAdminMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
        if (Api!.HasPermissions(adminSid, "blocks", "bmg"))
            menu.AddMenuOption(Localizer["MENUOPTION_Blocks"], (p, _) => { OpenBlocksMenu(p, menu); });
        if (Api.HasPermissions(adminSid, "players", "skt"))
            menu.AddMenuOption(Localizer["MENUOPTION_Players"], (p, _) => { OpenPlayersMenu(p, menu); });
        if (Api.HasPermissions(adminSid, "server", "z"))
            menu.AddMenuOption(Localizer["MENUOPTION_Server"], (p, _) => { OpenServerMenu(p, menu); });
        
        // Добавляем пункты из модулей
        var items = Api.ModulesOptions.Where(
            x => Api.HasPermissions(adminSid, x.FlagsAccess, x.FlagsDefault )
            && x.OptionLocation == "Main"
            );
        foreach (var item in items)
        {
            menu.AddMenuOption(item.Title, (p, _) =>
            {
                item.OnSelect.Invoke(p, admin, menu);
            });
        }
        Api.EOnMenuOpen("Main", menu, caller);
    }

    public static void OpenBlocksMenu(CCSPlayerController caller, IMenu backMenu)
    {
        var menu = new Menu(ConstructBlocksMenu);
        menu.Open(caller, Localizer["MENUTITLE_Blocks"], backMenu);
    }
    
    public static void OpenServerMenu(CCSPlayerController caller, IMenu backMenu)
    {
        var menu = new Menu(ConstructServerMenu);
        menu.Open(caller, Localizer["MENUTITLE_Server"], backMenu);
    }

    private static void ConstructServerMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
        if (Api!.HasPermissions(adminSid, "map", "z"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Maps"], (_, _) =>
            {
                OpenMapsMenu(caller, menu);
            });
        }
        
        // Добавляем пункты из модулей
        var items = Api.ModulesOptions.Where(
            x => Api.HasPermissions(adminSid, x.FlagsAccess, x.FlagsDefault )
                 && x.OptionLocation == "ManageServer"
        );
        foreach (var item in items)
        {
            menu.AddMenuOption(item.Title, (p, _) =>
            {
                item.OnSelect.Invoke(p, admin, menu);
            });
        }
        Api.EOnMenuOpen("ManageServer", menu, caller);
    }
    
    public static void OpenMapsMenu(CCSPlayerController caller, IMenu backMenu)
    {
        var menu = new Menu(ConstructMapsMenu);
        menu.Open(caller, Localizer["MENUTITLE_Maps"], backMenu);
    }

    private static void ConstructMapsMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        foreach (var map in Config.Maps)
        {
            menu.AddMenuOption(map.Title, (_, _) =>
            {
                _menuManager.CloseMenu(caller);
                Api!.SendMessageToAll($"Change map to {map.Title} ...");
                Api.Plugin.AddTimer(3, () =>
                {
                    if (map.Workshop)
                        Server.ExecuteCommand($"host_workshop_map {map.Id}");
                    else Server.ExecuteCommand($"map {map.Id}");
                    Api.EChangeMap(caller.AuthorizedSteamID!.SteamId64.ToString(), map);
                });
            });
        }
    }

    public static void OpenPlayersMenu(CCSPlayerController caller, IMenu backMenu)
    {
        var menu = new Menu(ConstructPlayersMenu);
        menu.Open(caller, Localizer["MENUTITLE_Players"], backMenu);
    }

    private static void ConstructPlayersMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
        if (Api!.HasPermissions(adminSid, "slay", "s"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Slay"], (_, _) =>
            {   
                OpenSlayMenu(caller, menu);
            });
        }
        if (Api.HasPermissions(adminSid, "kick", "k"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Kick"], (_, _) =>
            {   
                OpenKickMenu(caller, menu);
            });
        }
        if (Api.HasPermissions(adminSid, "switchteam", "t"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_SwitchTeam"], (_, _) =>
            {   
                OpenSwitchTeamMenu(caller, menu);
            });
        }
        if (Api.HasPermissions(adminSid, "changeteam", "t"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_ChangeTeam"], (_, _) =>
            {   
                OpenChangeTeamMenu(caller, menu);
            });
        }
        if (Api.HasPermissions(adminSid, "rename", "s"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Rename"], (_, _) =>
            {   
                OpenRenameMenu(caller, menu);
            });
        }
        
        // Добавляем пункты из модулей
        var items = Api.ModulesOptions.Where(
            x => Api.HasPermissions(adminSid, x.FlagsAccess, x.FlagsDefault )
                 && x.OptionLocation == "ManagePlayers"
        );
        foreach (var item in items)
        {
            menu.AddMenuOption(item.Title, (p, _) =>
            {
                item.OnSelect.Invoke(p, admin, menu);
            });
        }
        Api.EOnMenuOpen("ManagePlayers", menu, caller);
    }
    
    private static void OpenRenameMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            _menuManager.CloseMenu(caller);
            var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
            var player = XHelper.GetPlayerFromArg($"#{target.SteamId.SteamId64}");
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
        Menu menu = new Menu((_, _, menu) =>
        {
            TeamsMenuConstructor(menu, caller, target, changeTeam);
        });
        menu.Open(caller, Localizer["MENUTITLE_SelectTeam"], backMenu);
    }

    private static void TeamsMenuConstructor(IMenu menu, CCSPlayerController caller, PlayerInfo target, bool changeTeam)
    {
        var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
        menu.PostSelectAction = PostSelectAction.Close;
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
                _menuManager.CloseMenu(caller);
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
        if (Api!.HasPermissions(adminSid, "ban", "b"))
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
        if (Api.HasPermissions(adminSid, "gag", "g"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Gag"], (_, _) =>
            {
                OpenGagMenu(caller, menu);
            });
        }
        if (Api.HasPermissions(adminSid, "mute", "m"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Mute"], (_, _) =>
            {
                OpenMuteMenu(caller, menu);
            });
        }
        if (Api.HasPermissions(adminSid, "silence", "gm"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_Silence"], (_, _) =>
            {
                OpenSilenceMenu(caller, menu);
            });
        }
        if (Api.HasPermissions(adminSid, "ungag", "g"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_UnGag"], (_, _) =>
            {
                var gagMenu = new Menu(ConstructUnGagMenu);
                gagMenu.Open(caller, Localizer["MENUTITLE_UnGag"], menu);
            });
        }
        if (Api.HasPermissions(adminSid, "unmute", "m"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_UnMute"], (_, _) =>
            {
                var gagMenu = new Menu(ConstructUnMuteMenu);
                gagMenu.Open(caller, Localizer["MENUTITLE_UnMute"], menu);
            });
        }
        if (Api.HasPermissions(adminSid, "unsilence", "gm"))
        {
            menu.AddMenuOption(Localizer["MENUOPTION_UnSilence"], (_, _) =>
            {
                var unsilenceMenu = new Menu(ConstructUnSilenceMenu);
                unsilenceMenu.Open(caller, Localizer["MENUTITLE_UnSilence"], menu);
            });
        }
        
        // Добавляем пункты из модулей
        var items = Api.ModulesOptions.Where(
            x => Api.HasPermissions(adminSid, x.FlagsAccess, x.FlagsDefault )
                 && x.OptionLocation == "ManageBlocks"
        );
        foreach (var item in items)
        {
            menu.AddMenuOption(item.Title, (p, _) =>
            {
                item.OnSelect.Invoke(p, admin, menu);
            });
        }
        Api.EOnMenuOpen("ManageBlocks", menu, caller);
    }

    private static void OpenBanMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, Config.BanReasons, (reason, time) =>
            {
                _menuManager.CloseMenu(caller);
                time = time * 60;
                var newBan = new PlayerBan(
                    target.PlayerName,
                    target.SteamId.SteamId64.ToString(),
                    target.IpAddress,
                    caller.AuthorizedSteamID!.SteamId64.ToString(),
                    caller.PlayerName,
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
    
    private static void OpenOfflineBanMenu(CCSPlayerController caller, IMenu backMenu)
    {
        Menu menu = new Menu(ConstructOfflineBanMenu);
        menu.Open(caller, Localizer["MENUTITLE_OfflineBan"], backMenu);
    }

    private static void ConstructOfflineBanMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        foreach (var player in Api!.DisconnectedPlayers)
        {
            if (!Api.HasMoreImmunity(caller.GetSteamId(), player.Value.SteamId.SteamId64.ToString())) continue;
            menu.AddMenuOption(player.Value.PlayerName, (_, _) =>
            {
                SelectReasonAndTime(caller, menu, Config.BanReasons, (reason, time) =>
                {
                    time = time * 60;
                    var newBan = new PlayerBan(
                        player.Value.PlayerName,
                        player.Value.SteamId.SteamId64.ToString(),
                        player.Value.IpAddress,
                        caller.AuthorizedSteamID!.SteamId64.ToString(),
                        caller.PlayerName,
                        (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        time,
                        (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
                        reason,
                        Config.ServerId
                    );
                    Task.Run(async () => { await Api.AddBan(newBan.AdminSid, newBan); });
                });
            });
        }
    }


    private static void OpenGagMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, Config.GagReasons, (reason, time) =>
            {
                _menuManager.CloseMenu(caller);
                time = time * 60;
                var newBan = new PlayerComm(
                    target.PlayerName,
                    target.SteamId.SteamId64.ToString(),
                    caller.AuthorizedSteamID!.SteamId64.ToString(),
                    caller.PlayerName,
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
        }, true, false, Localizer["MENUTITLE_Gag"], false, "gag");
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
                        _menuManager.CloseMenu(caller);
                        await Api.UnGag(playerSid, adminSid);
                    });
                });
            }
        }
    }
    private static void OpenSilenceMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, Config.GagReasons, (reason, time) =>
            {
                _menuManager.CloseMenu(caller);
                time = time * 60;
                var newBan = new PlayerComm(
                    target.PlayerName,
                    target.SteamId.SteamId64.ToString(),
                    caller.AuthorizedSteamID!.SteamId64.ToString(),
                    caller.PlayerName,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    time,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time,
                    reason,
                    Config.ServerId
                );
                Task.Run(async () =>
                {
                    if (Api!.HasPermissions(newBan.AdminSid, "gag", "g")) await Api.AddGag(newBan.AdminSid, newBan);
                    if (Api.HasPermissions(newBan.AdminSid, "mute", "m")) await Api.AddMute(newBan.AdminSid, newBan);
                });
            });
        }, true, false, Localizer["MENUTITLE_Silence"]);
    }
    
    private static void ConstructUnMuteMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var players = XHelper.GetOnlinePlayers();
        foreach (var player in players)
        {
            var playerSid = player.AuthorizedSteamID!.SteamId64.ToString();
            var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
            if (Api!.OnlineMutedPlayers.Any(x => x.Sid == playerSid))
            {
                menu.AddMenuOption(player.PlayerName, (_, _) =>
                {
                    Task.Run(async () =>
                    {
                        _menuManager.CloseMenu(caller);
                        await Api.UnMute(playerSid, adminSid);
                    });
                });
            }
        }
    }
    private static void ConstructUnSilenceMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var players = XHelper.GetOnlinePlayers();
        foreach (var player in players)
        {
            var playerSid = player.AuthorizedSteamID!.SteamId64.ToString();
            var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
            menu.AddMenuOption(player.PlayerName, (_, _) =>
            {
                Task.Run(async () =>
                {
                    _menuManager.CloseMenu(caller);
                    if (Api!.HasPermissions(adminSid, "ungag", "g")) await Api.UnGag(playerSid, adminSid);
                    if (Api.HasPermissions(adminSid, "unmute", "m")) await Api.UnMute(playerSid, adminSid);
                    
                });
            });
        }
    }
    
    private static void OpenMuteMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, Config.MuteReasons, (reason, time) =>
            {
                _menuManager.CloseMenu(caller);
                time = time * 60;
                var newBan = new PlayerComm(
                    target.PlayerName,
                    target.SteamId.SteamId64.ToString(),
                    caller.AuthorizedSteamID!.SteamId64.ToString(),
                    caller.PlayerName,
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
        }, true, false, Localizer["MENUTITLE_Mute"], false,  "mute");
    }
    
    private static void SelectReasonAndTime(CCSPlayerController caller, IMenu backMenu ,List<Reason> reasons, Action<string, int> onSelect)
    {
        Menu menu = new Menu((controller, _, arg3) => 
            SelectReasonAndTimeConstructor(controller, arg3, reasons, onSelect));
        menu.Open(caller, Localizer["MENUTITLE_Reason"], backMenu);
    }

    private static void SelectReasonAndTimeConstructor(CCSPlayerController caller, IMenu menu, 
        List<Reason> reasons, Action<string, int> onSelect)
    {
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
                menu.AddMenuOption(reason.Title, (_, _) =>
                {
                    SelectTime(caller, menu, time =>
                    {
                        onSelect.Invoke(reason.Title, time);
                    });
                });
            }
            if (reason.Time == -1)
            {
                menu.AddMenuOption(reason.Title, (_, _) =>
                {
                    Api!.SendMessageToPlayer(caller, Localizer["NOTIFY_WriteReason"]);
                    Api.NextCommandAction.Add(caller, s =>
                    {
                        SelectTime(caller, menu, time =>
                        {
                            onSelect.Invoke(s, time);
                        });
                    });
                });
            }
        }
    }

    private static void SelectTime(CCSPlayerController caller, IMenu backMenu, Action<int> onSelect)
    {
        Menu menu = new Menu((controller, _, arg3) =>
        {
            SelectTimeConstructor(controller, arg3, onSelect);
        });
        menu.Open(caller, Localizer["MENUTITLE_Time"], backMenu);
    }
    private static void SelectTimeConstructor(CCSPlayerController caller, IMenu menu, Action<int> onSelect)
    {
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
        Menu menu = new Menu((controller, _, arg3) => 
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
        bool ignoreYourself = true, bool offline = false, string? title = null, bool aliveOnly = false, string? filter = null)
    {
        Menu menu = new Menu((controller, _, arg3) =>
        {
            ConstructSelectPlayerMenu(controller, arg3, onSelect, ignoreYourself, offline, aliveOnly, filter);
        });
        title = title == null ? Localizer["MENUTITLE_SelectPlayer"] : title;
        menu.Open(caller, title, backMenu);
    }

    private static void ConstructSelectPlayerMenu(CCSPlayerController caller, IMenu menu, 
        Action<PlayerInfo, IMenu> onSelect, bool ignoreYourself = true, bool offline = false, bool aliveOnly = false, string? filter = null)
    {
        var players = Api!.DisconnectedPlayers.Values.ToList();
        if (!offline)
        {
            players.Clear();
            foreach (var player in XHelper.GetOnlinePlayers())
            {
                if (aliveOnly && !player.PawnIsAlive) continue;
                players.Add(XHelper.CreateInfo(player));
            }
        }

        if (offline) players.Reverse();
        
        foreach (var player in players)
        {
            var playerInfo = new PlayerInfo(player.PlayerName, player.SteamId.SteamId64, player.IpAddress);
            if (filter != null)
            {
                switch (filter)
                {
                    case "gag":
                        if (Api.OnlineGaggedPlayers.Any(x => x.Sid == playerInfo.SteamId.SteamId64.ToString()))
                            continue;
                        break;
                    case "mute":
                        if (Api.OnlineMutedPlayers.Any(x => x.Sid == playerInfo.SteamId.SteamId64.ToString()))
                            continue;
                        break;
                }
            }
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
