using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class PluginMenuManager
{
    private static IIksAdminApi? _api = IksAdmin.Api;
    private static IStringLocalizer _localizer = _api!.Localizer;
    private static PluginConfig _config = IksAdmin.ConfigNow;
    public static void OpenAdminMenu(CCSPlayerController caller)
    {
        var menu = new Menu(caller, ConstructAdminMenu);
        menu.Open(caller, _localizer["MENUTITLE_Main"]);
    }

    public static void ConstructAdminMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        if (_api!.HasPermisions(caller, "blocks", "bmg"))
            menu.AddMenuOption(_localizer["MENUOPTION_Blocks"], (p, _) => { OpenBlocksMenu(p, menu); });
        if (_api.HasPermisions(caller, "players", "skt"))
            menu.AddMenuOption(_localizer["MENUOPTION_Players"], (p, _) => { OpenPlayersMenu(p, menu); });
        if (_api.HasPermisions(caller, "server", "z"))
            menu.AddMenuOption(_localizer["MENUOPTION_Server"], (p, _) => { OpenBlocksMenu(p, menu); });
        
        // Добавляем пункты из модулей
        var items = _api.ModulesOptions.Where(
            x => _api.HasPermisions(caller, x.FlagsAccess, x.FlagsDefault )
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
        menu.Open(caller, _localizer["MENUTITLE_Blocks"], backMenu);
    }
    
    public static void OpenPlayersMenu(CCSPlayerController caller, IMenu backMenu)
    {
        var menu = new Menu(caller, ConstructPlayersMenu);
        menu.Open(caller, _localizer["MENUTITLE_Players"], backMenu);
    }

    private static void ConstructPlayersMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        if (_api!.HasPermisions(caller, "slay", "s"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_Slay"], (_, _) =>
            {   
                OpenSlayMenu(caller, menu);
            });
        }
        if (_api.HasPermisions(caller, "kick", "k"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_Kick"], (_, _) =>
            {   
                OpenKickMenu(caller, menu);
            });
        }
        if (_api.HasPermisions(caller, "switchteam", "s"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_SwitchTeam"], (_, _) =>
            {   
                OpenSwitchTeamMenu(caller, menu);
            });
        }
        if (_api.HasPermisions(caller, "changeteam", "s"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_ChangeTeam"], (_, _) =>
            {   
                OpenChangeTeamMenu(caller, menu);
            });
        }
        
    }

    private static void OpenSwitchTeamMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, playersMenu) =>
        {
            OpenTeamsMenu(caller, target, playersMenu);
        }, false, false, _localizer["MENUTITLE_SwitchTeam"]);
    }
    private static void OpenChangeTeamMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, playersMenu) =>
        {
            OpenTeamsMenu(caller, target, playersMenu, true);
        }, false, false, _localizer["MENUTITLE_ChangeTeam"]);
    }

    private static void OpenTeamsMenu(CCSPlayerController caller, PlayerInfo target, IMenu backMenu, bool changeTeam = false)
    {
        Menu menu = new Menu(caller, (_, _, menu) =>
        {
            TeamsMenuConstructor(menu, caller, target, changeTeam);
        });
        menu.Open(caller, _localizer["MENUTITLE_SelectTeam"], backMenu);
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
                _api!.EChangeTeam(adminSid, target, oldTeam, newTeam);
            }
            else
            {
                selectedPlayer.SwitchTeam(newTeam);
                _api!.ESwitchTeam(adminSid, target, oldTeam, newTeam);
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
                _api!.EChangeTeam(adminSid, target, oldTeam, newTeam);
            }
            else
            {
                selectedPlayer.SwitchTeam(newTeam);
                _api!.ESwitchTeam(adminSid, target, oldTeam, newTeam);
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
                _api!.EChangeTeam(adminSid, target, oldTeam, newTeam);
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
            _api!.ESlay(caller.AuthorizedSteamID!.SteamId64.ToString(), target);
        }, false, false, _localizer["MENUTITLE_Slay"], true);
    }
    
    private static void OpenKickMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReason(caller, menu, _config.KickReasons, s =>
            {
                MenuManager.CloseActiveMenu(caller);
                var selectedPlayer = XHelper.GetPlayerFromArg($"#{target.SteamId.SteamId64}");
                if (selectedPlayer == null) return;
                Server.ExecuteCommand("kickid " + selectedPlayer.UserId);
                _api!.EKick(caller.AuthorizedSteamID!.SteamId64.ToString(), target, s);
            });
        }, true, false, _localizer["MENUTITLE_Kick"]);
    }

    public static void ConstructBlocksMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        if (_api!.HasPermisions(caller, "ban", "b"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_Ban"], (_, _) =>
            {   
                OpenBanMenu(caller, menu);
            });
            menu.AddMenuOption(_localizer["MENUOPTION_OfflineBan"], (_, _) =>
            {
                OpenOfflineBanMenu(caller, menu);
            });
        }
        if (_api.HasPermisions(caller, "gag", "g"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_Gag"], (_, _) =>
            {
                OpenGagMenu(caller, menu);
            });
        }
        if (_api.HasPermisions(caller, "mute", "m"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_Mute"], (_, _) =>
            {
                OpenMuteMenu(caller, menu);
            });
        }
        if (_api.HasPermisions(caller, "ungag", "g"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_UnGag"], (_, _) =>
            {
                var gagMenu = new Menu(caller, ConstructUnGagMenu);
                gagMenu.Open(caller, _localizer["MENUTITLE_UnGag"], menu);
            });
        }
        if (_api.HasPermisions(caller, "unmute", "m"))
        {
            menu.AddMenuOption(_localizer["MENUOPTION_UnMute"], (_, _) =>
            {
                var gagMenu = new Menu(caller, ConstructUnMuteMenu);
                gagMenu.Open(caller, _localizer["MENUTITLE_UnMute"], menu);
            });
        }
        
        // Добавляем пункты из модулей
        var items = _api.ModulesOptions.Where(
            x => _api.HasPermisions(caller, x.FlagsAccess, x.FlagsDefault )
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
            SelectReasonAndTime(caller, menu, _config.BanReasons, (reason, time) =>
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
                    _config.ServerId
                );
                Task.Run(async () =>
                {
                    await _api!.AddBan(newBan.AdminSid, newBan);
                });
            });
        }, true, false, _localizer["MENUTITLE_Ban"]);
    }
    
    private static void OpenOfflineBanMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, _config.BanReasons, (reason, time) =>
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
                    _config.ServerId
                );
                Task.Run(async () =>
                {
                    await _api!.AddBan(newBan.AdminSid, newBan);
                });
            });
        }, true, true, _localizer["MENUTITLE_OfflineBan"]);
    }
    
    private static void OpenGagMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, _config.GagReasons, (reason, time) =>
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
                    _config.ServerId
                );
                Task.Run(async () =>
                {
                    await _api!.AddGag(newBan.AdminSid, newBan);
                });
            });
        }, true, false, _localizer["MENUTITLE_Gag"]);
    }
    private static void ConstructUnGagMenu(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        var players = XHelper.GetOnlinePlayers();
        foreach (var player in players)
        {
            var playerSid = player.AuthorizedSteamID!.SteamId64.ToString();
            var adminSid = caller.AuthorizedSteamID!.SteamId64.ToString();
            if (_api!.OnlineGaggedPlayers.Any(x => x.Sid == playerSid))
            {
                menu.AddMenuOption(player.PlayerName, (_, _) =>
                {
                    Task.Run(async () =>
                    {
                        await _api.UnGag(playerSid, adminSid);
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
            if (_api!.OnlineGaggedPlayers.Any(x => x.Sid == playerSid))
            {
                menu.AddMenuOption(player.PlayerName, (_, _) =>
                {
                    Task.Run(async () =>
                    {
                        await _api.UnGag(playerSid, adminSid);
                    });
                });
            }
        }
    }
    
    private static void OpenMuteMenu(CCSPlayerController caller, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (target, _) =>
        {
            SelectReasonAndTime(caller, menu, _config.MuteReasons, (reason, time) =>
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
                    _config.ServerId
                );
                Task.Run(async () =>
                {
                    await _api!.AddMute(newBan.AdminSid, newBan);
                });
            });
        }, true, false, _localizer["MENUTITLE_Mute"]);
    }
    
    private static void SelectReasonAndTime(CCSPlayerController caller, IMenu backMenu ,List<Reason> reasons, Action<string, int> onSelect)
    {
        Menu menu = new Menu(caller, (controller, _, arg3) => 
            SelectReasonAndTimeConstructor(controller, arg3, reasons, onSelect));
        menu.Open(caller, _localizer["MENUTITLE_Reason"], backMenu);
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
                _api!.SendMessageToPlayer(caller, _localizer["NOTIFY_WriteReason"]);
                _api.NextCommandAction.Add(caller, s =>
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
        menu.Open(caller, _localizer["MENUTITLE_Time"], backMenu);
    }
    private static void SelectTimeConstructor(CCSPlayerController caller, IMenu menu, Action<int> onSelect)
    {
        menu.PostSelectAction = PostSelectAction.Nothing;
        foreach (var time in _config.Times)
        {
            if (time.Value == -1)
            {
                menu.AddMenuOption(time.Key, (_, _) =>
                {
                    _api!.SendMessageToPlayer(caller, _localizer["NOTIFY_WriteTime"]);
                    _api.NextCommandAction.Add(caller, s =>
                    {
                        if (!int.TryParse(s, out var result))
                        {
                            _api.SendMessageToPlayer(caller, _localizer["NOTIFY_ErrorTime"]);
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
        menu.Open(caller, _localizer["MENUTITLE_Reason"], backMenu);
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
                    _api!.SendMessageToPlayer(caller, _localizer["NOTIFY_WriteReason"]);
                    _api.NextCommandAction.Add(caller, onSelect.Invoke);
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
        title = title == null ? _localizer["MENUTITLE_SelectPlayer"] : title;
        menu.Open(caller, title, backMenu);
    }

    private static void ConstructSelectPlayerMenu(CCSPlayerController caller, IMenu menu, Action<PlayerInfo, IMenu> onSelect, bool ignoreYourself = true, bool offline = false, bool aliveOnly = false)
    {
        var players = _api!.DisconnectedPlayers;
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
            if (!_api.HasMoreImmunity(caller.AuthorizedSteamID!.SteamId64.ToString(),
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
