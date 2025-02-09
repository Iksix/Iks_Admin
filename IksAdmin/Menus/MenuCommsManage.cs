using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Functions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Menus;

public static class MenuCommsManage
{
    static IIksAdminApi _api {get; set;} = Main.AdminApi;
    static IStringLocalizer _localizer {get; set;} = Main.AdminApi.Localizer;

    public static void OpenCommsMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm.comm"), _localizer["MenuTitle.CM"], backMenu: backMenu);
        menu.AddMenuOption("add", _localizer["MenuOption.CM.Add"], (_, _) => {
            OpenAddCommMenu(caller, menu);
        });
        menu.AddMenuOption("remove", _localizer["MenuOption.CM.Remove"], (_, _) => {
            OpenRemoveCommsMenu(caller, _api.Comms.ToList(), menu);
        });
        menu.AddMenuOption("remove_offline", _localizer["MenuOption.CM.RemoveOffline"], (_, _) => {
            Task.Run(async () => {
                List<PlayerComm> comms = await _api.GetLastComms(_api.Config.LastPunishmentTime);
                comms.Reverse();
                Server.NextFrame(() => {
                    OpenRemoveCommsMenu(caller, comms, menu);
                });
            });
        });
        menu.Open(caller);
    }

    private static void OpenRemoveCommsMenu(CCSPlayerController caller, List<PlayerComm> comms, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_uncomm"), _localizer["MenuTitle.CM.UnComm"], backMenu: backMenu);
        var admin = caller.Admin()!;
        
        foreach (var comm in comms)
        {
            string postfix = "";
            if (comm.IsUnbanned)
                postfix = _localizer["MenuOption.Postfix.Unbanned"];
            else if (comm.IsExpired)
                postfix = _localizer["MenuOption.Postfix.Expired"];
            else
            {
                switch (comm.MuteType)
                {
                    case 0:
                        postfix = _localizer["MenuOption.Postfix.Muted"];
                        break;
                    case 1:
                        postfix = _localizer["MenuOption.Postfix.Gagged"];
                        break;
                    case 2:
                        postfix = _localizer["MenuOption.Postfix.Silenced"];
                        break;
                }
            }

            bool hasPermissions = true;
            if (comm.MuteType == 0)
            {
                hasPermissions = admin.HasPermissions("comms_manage.unmute");
            }
            else if (comm.MuteType == 1)
            {
                hasPermissions = admin.HasPermissions("comms_manage.ungag");
            }
            else if (comm.MuteType == 2)
            {
                hasPermissions = admin.HasPermissions("comms_manage.unsilence");
            }
            
            menu.AddMenuOption("cm_uncomm_" + comm.SteamId, comm.Name + postfix, (_, _) => {
                caller.Print(_localizer["Message.GL.ReasonSet"]);
                _api.HookNextPlayerMessage(caller, r => {
                    Task.Run(async () => {
                        switch (comm.MuteType)
                        {
                            case 0:
                                await MutesFunctions.Unmute(admin, comm.SteamId!, r);
                                break;
                            case 1:
                                await GagsFunctions.Ungag(admin, comm.SteamId!, r);
                                break;
                            case 2:
                                await SilenceFunctions.UnSilence(admin, comm.SteamId!, r);
                                break;
                        }
                        comm.UnbannedBy = admin.Id;
                        Server.NextFrame(() =>
                        {
                            OpenRemoveCommsMenu(caller, comms, backMenu);
                        });
                    });
                });
            }, disabled: (!AdminUtils.CanUnComm(admin, comm) || comm.IsExpired || comm.IsUnbanned) && hasPermissions);
        }
        menu.Open(caller);
    }
    public static void OpenAddCommMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_comm_select"), _localizer["MenuTitle.CM.SelectType"], backMenu: backMenu);
        
        menu.AddMenuOption("mute", _localizer["MenuOption.CM.Mute"], (_, _) =>
        {
            SelectPlayerForMute(caller, menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("comms_manage.mute"));
        menu.AddMenuOption("gag", _localizer["MenuOption.CM.Gag"], (_, _) => {
            SelectPlayerForGag(caller, menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("comms_manage.gag"));
        if (caller.HasPermissions("comms_manage.silence"))
        menu.AddMenuOption("silence", _localizer["MenuOption.CM.Silence"], (_, _) => {
            SelectPlayerForSilence(caller, menu);
        }, viewFlags: _api.GetCurrentPermissionFlags("comms_manage.silence"));
        
        menu.Open(caller);
    }

    private static void SelectPlayerForMute(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_mute_sp"), _localizer["MenuTitle.Other.SelectPlayer"], backMenu: backMenu);
        var players = PlayersUtils.GetOnlinePlayers();
        foreach (var p in players)
        {
            string postfix = "";
            if (p.GetComms().HasMute())
            {
                postfix = _localizer["MenuOption.Postfix.Muted"];
            }
            if (p.GetComms().HasSilence())
            {
                postfix = _localizer["MenuOption.Postfix.Silenced"];
            }
            menu.AddMenuOption("cm_mute_sp_" + p.GetSteamId(), p.PlayerName + postfix, (_, _) =>
            {
                OpenSelectMuteReasonMenu(caller, new PlayerInfo(p));
            }, disabled: !_api.CanDoActionWithPlayer(caller.GetSteamId(), p.GetSteamId()) || p.GetComms().HasMute() || p.GetComms().HasSilence());
        }
        
        menu.Open(caller);
    }
    private static void SelectPlayerForGag(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_gag_sp"), _localizer["MenuTitle.Other.SelectPlayer"], backMenu: backMenu);
        var players = PlayersUtils.GetOnlinePlayers();
        foreach (var p in players)
        {
            string postfix = "";
            if (p.GetComms().HasGag())
            {
                postfix = _localizer["MenuOption.Postfix.Gagged"];
            }
            if (p.GetComms().HasSilence())
            {
                postfix = _localizer["MenuOption.Postfix.Silenced"];
            }
            menu.AddMenuOption("cm_gag_sp_" + p.GetSteamId(), p.PlayerName + postfix, (_, _) =>
            {
                OpenSelectGagReasonMenu(caller, new PlayerInfo(p));
            }, disabled: !_api.CanDoActionWithPlayer(caller.GetSteamId(), p.GetSteamId()) || p.GetComms().HasGag() || p.GetComms().HasSilence());
        }
        
        menu.Open(caller);
    }
    private static void SelectPlayerForSilence(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_gag_sp"), _localizer["MenuTitle.Other.SelectPlayer"], backMenu: backMenu);
        var players = PlayersUtils.GetOnlinePlayers();
        foreach (var p in players)
        {
            string postfix = "";
            if (p.GetComms().HasGag())
            {
                postfix = _localizer["MenuOption.Postfix.Gagged"];
            }
            if (p.GetComms().HasMute())
            {
                postfix += _localizer["MenuOption.Postfix.Muted"];
            }
            if (p.GetComms().HasSilence())
            {
                postfix = _localizer["MenuOption.Postfix.Silenced"];
            }
            menu.AddMenuOption("cm_gag_sp_" + p.GetSteamId(), p.PlayerName + postfix, (_, _) =>
            {
                OpenSelectSilenceReasonMenu(caller, new PlayerInfo(p));
            }, disabled: !_api.CanDoActionWithPlayer(caller.GetSteamId(), p.GetSteamId()) || p.GetComms().HasGag() || p.GetComms().HasMute() || p.GetComms().HasSilence());
        }
        
        menu.Open(caller);
    }

    private static void OpenSelectMuteReasonMenu(CCSPlayerController caller, PlayerInfo target)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_mute_reason"), _localizer["MenuTitle.Other.SelectReason"]);
        var config = MutesConfig.Config;
        var reasons = config.Reasons;
        menu.AddMenuOption("own_reason" ,_localizer["MenuOption.Other.OwnReason"], (_, _) => {
            caller.Print( _localizer["Message.PrintOwnReason"]);
            _api.HookNextPlayerMessage(caller, reason => {
                OpenMuteTimeSelectMenu(caller, target, reason, menu);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("comms_manage.own_mute_reason"));
        foreach (var reason in reasons)
        {
            if (reason.HideFromMenu) continue;
            if (reason.Duration != null)
            {
                if (caller.Admin()!.MaxMuteTime != 0)
                {
                    if (reason.Duration > caller.Admin()!.MaxMuteTime)
                        continue;
                }
                if (caller.Admin()!.MinMuteTime != 0)
                {
                    if (reason.Duration < caller.Admin()!.MinMuteTime)
                        continue;
                }
            }
            menu.AddMenuOption(reason.Title, reason.Title, (_, _) => {
                if (reason.Duration == null)
                {
                    OpenMuteTimeSelectMenu(caller, target, reason.Text, menu);
                } else {
                    var comm = new PlayerComm(target, PlayerComm.MuteTypes.Mute, reason.Text, (int)reason.Duration, serverId: _api.ThisServer.Id);
                    if (MutesConfig.Config.BanOnAllServers) {
                        comm.ServerId = null;
                    }
                    comm.AdminId = caller.Admin()!.Id;
                    Task.Run(async () =>
                {
                    await MutesFunctions.Mute(comm);
                });
                    _api.CloseMenu(caller);
                }
            });
        }
        menu.Open(caller);
    }
    
    private static void OpenSelectGagReasonMenu(CCSPlayerController caller, PlayerInfo target)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_gag_reason"), _localizer["MenuTitle.Other.SelectReason"]);
        var config = GagsConfig.Config;
        var reasons = config.Reasons;
        menu.AddMenuOption("own_reason" ,_localizer["MenuOption.Other.OwnReason"], (_, _) => {
            caller.Print( _localizer["Message.PrintOwnReason"]);
            _api.HookNextPlayerMessage(caller, reason => {
                OpenGagTimeSelectMenu(caller, target, reason, menu);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("comms_manage.own_gag_reason"));
        foreach (var reason in reasons)
        {
            if (reason.HideFromMenu) continue;
            if (reason.Duration != null)
            {
                if (caller.Admin()!.MaxGagTime != 0)
                {
                    if (reason.Duration > caller.Admin()!.MaxGagTime)
                        continue;
                }
                if (caller.Admin()!.MinGagTime != 0)
                {
                    if (reason.Duration < caller.Admin()!.MinGagTime)
                        continue;
                }
            }
            menu.AddMenuOption(reason.Title, reason.Title, (_, _) => {
                if (reason.Duration == null)
                {
                    OpenGagTimeSelectMenu(caller, target, reason.Text, menu);
                } else {
                    var comm = new PlayerComm(target, PlayerComm.MuteTypes.Gag, reason.Text, (int)reason.Duration, serverId: _api.ThisServer.Id);
                    if (GagsConfig.Config.BanOnAllServers) {
                        comm.ServerId = null;
                    }
                    comm.AdminId = caller.Admin()!.Id;
                    Task.Run(async () =>
                {
                    await GagsFunctions.Gag(comm);
                });
                    _api.CloseMenu(caller);
                }
            });
        }
        menu.Open(caller);
    }
    private static void OpenSelectSilenceReasonMenu(CCSPlayerController caller, PlayerInfo target)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_silence_reason"), _localizer["MenuTitle.Other.SelectReason"]);
        var config = SilenceConfig.Config;
        var reasons = config.Reasons;
        menu.AddMenuOption("own_reason" ,_localizer["MenuOption.Other.OwnReason"], (_, _) => {
            caller.Print( _localizer["Message.PrintOwnReason"]);
            _api.HookNextPlayerMessage(caller, reason => {
                OpenSilenceTimeSelectMenu(caller, target, reason, menu);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("comms_manage.own_silence_reason"));
        foreach (var reason in reasons)
        {
            if (reason.HideFromMenu) continue;
            if (reason.Duration != null)
            {
                if (caller.Admin()!.MaxGagTime != 0 || caller.Admin()!.MaxMuteTime != 0)
                {
                    if (reason.Duration > caller.Admin()!.MaxGagTime || reason.Duration > caller.Admin()!.MaxMuteTime)
                        continue;
                }
                if (caller.Admin()!.MinGagTime != 0 || caller.Admin()!.MaxGagTime != 0)
                {
                    if (reason.Duration < caller.Admin()!.MinGagTime || reason.Duration < caller.Admin()!.MinMuteTime)
                        continue;
                }
            }
            menu.AddMenuOption(reason.Title, reason.Title, (_, _) => {
                if (reason.Duration == null)
                {
                    OpenSilenceTimeSelectMenu(caller, target, reason.Text, menu);
                } else {
                    var comm = new PlayerComm(target, PlayerComm.MuteTypes.Silence, reason.Text, (int)reason.Duration, serverId: _api.ThisServer.Id);
                    if (SilenceConfig.Config.BanOnAllServers) {
                        comm.ServerId = null;
                    }
                    comm.AdminId = caller.Admin()!.Id;
                    Task.Run(async () =>
                    {
                        await SilenceFunctions.Silence(comm);
                    });
                    _api.CloseMenu(caller);
                }
            });
        }
        menu.Open(caller);
    }
    private static void OpenMuteTimeSelectMenu(CCSPlayerController caller, PlayerInfo target, string reason, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_mute_time"), _localizer["MenuTitle.Other.SelectTime"], backMenu: backMenu);
        var config = MutesConfig.Config;
        var times = config.Times;
        var admin = caller.Admin()!;
        var comm = new PlayerComm(target, 0, reason, 0, serverId: _api.ThisServer.Id);
        if (MutesConfig.Config.BanOnAllServers) {
            comm.ServerId = null;
        }
        comm.AdminId = admin.Id;
        menu.AddMenuOption("own_mute_time" ,_localizer["MenuOption.Other.OwnTime"], (_, _) => {
            Helper.Print(caller, _localizer["Message.PrintOwnTime"]);
            _api.HookNextPlayerMessage(caller, time => {
                if (!int.TryParse(time, out var timeInt))
                {
                    Helper.Print(caller, _localizer["Error.MustBeANumber"]);
                    return;
                }
                comm.Duration = timeInt*60;
                Task.Run(async () =>
                {
                    await MutesFunctions.Mute(comm);
                });
                _api.CloseMenu(caller);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("comms_manage.own_mute_time"));
        foreach (var time in times)
        {
            if (caller.Admin()!.MaxMuteTime != 0)
            {
                if (time.Key > caller.Admin()!.MaxMuteTime)
                    continue;
            }
            if (caller.Admin()!.MinMuteTime != 0)
            {
                if (time.Key < caller.Admin()!.MinMuteTime)
                    continue;
            }
            menu.AddMenuOption("mute_time_" + time.Key, time.Value, (_, _) => {
                _api.CloseMenu(caller);
                comm.Duration = time.Key*60;
                Task.Run(async () =>
                {
                    await MutesFunctions.Mute(comm);
                });
            });
        }
        menu.Open(caller);
    }
    private static void OpenGagTimeSelectMenu(CCSPlayerController caller, PlayerInfo target, string reason, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_gag_time"), _localizer["MenuTitle.Other.SelectTime"], backMenu: backMenu);
        var config = GagsConfig.Config;
        var times = config.Times;
        var admin = caller.Admin()!;
        var comm = new PlayerComm(target, PlayerComm.MuteTypes.Gag, reason, 0, serverId: _api.ThisServer.Id);
        if (GagsConfig.Config.BanOnAllServers) {
            comm.ServerId = null;
        }
        comm.AdminId = admin.Id;
        menu.AddMenuOption("own_gag_time" ,_localizer["MenuOption.Other.OwnTime"], (_, _) => {
            Helper.Print(caller, _localizer["Message.PrintOwnTime"]);
            _api.HookNextPlayerMessage(caller, time => {
                if (!int.TryParse(time, out var timeInt))
                {
                    Helper.Print(caller, _localizer["Error.MustBeANumber"]);
                    return;
                }
                comm.Duration = timeInt*60;
                Task.Run(async () =>
                {
                    await GagsFunctions.Gag(comm);
                });
                _api.CloseMenu(caller);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("comms_manage.own_gag_time"));
        foreach (var time in times)
        {
            if (caller.Admin()!.MaxGagTime != 0)
            {
                if (time.Key > caller.Admin()!.MaxGagTime)
                    continue;
            }
            if (caller.Admin()!.MinGagTime != 0)
            {
                if (time.Key < caller.Admin()!.MinGagTime)
                    continue;
            }
            menu.AddMenuOption("gag_time_" + time.Key, time.Value, (_, _) => {
                _api.CloseMenu(caller);
                comm.Duration = time.Key*60;
                Task.Run(async () =>
                {
                    await GagsFunctions.Gag(comm);
                });
            });
        }
        menu.Open(caller);
    }
    private static void OpenSilenceTimeSelectMenu(CCSPlayerController caller, PlayerInfo target, string reason, IDynamicMenu? backMenu = null)
    {
        var menu = _api.CreateMenu(Main.MenuId("cm_silence_time"), _localizer["MenuTitle.Other.SelectTime"], backMenu: backMenu);
        var config = SilenceConfig.Config;
        var times = config.Times;
        var admin = caller.Admin()!;
        var comm = new PlayerComm(target, PlayerComm.MuteTypes.Silence, reason, 0, serverId: _api.ThisServer.Id);
        if (SilenceConfig.Config.BanOnAllServers) {
            comm.ServerId = null;
        }
        comm.AdminId = admin.Id;
        menu.AddMenuOption("own_silence_time" ,_localizer["MenuOption.Other.OwnTime"], (_, _) => {
            Helper.Print(caller, _localizer["Message.PrintOwnTime"]);
            _api.HookNextPlayerMessage(caller, time => {
                if (!int.TryParse(time, out var timeInt))
                {
                    Helper.Print(caller, _localizer["Error.MustBeANumber"]);
                    return;
                }
                comm.Duration = timeInt*60;
                Task.Run(async () =>
                {
                    await SilenceFunctions.Silence(comm);
                });
                _api.CloseMenu(caller);
            });
        }, viewFlags: AdminUtils.GetCurrentPermissionFlags("comms_manage.own_silence_time"));
        foreach (var time in times)
        {
            if (caller.Admin()!.MaxGagTime != 0 || caller.Admin()!.MaxMuteTime != 0)
            {
                if (time.Key > caller.Admin()!.MaxGagTime || time.Key > caller.Admin()!.MaxMuteTime)
                    continue;
            }
            if (caller.Admin()!.MinGagTime != 0 || caller.Admin()!.MinMuteTime != 0)
            {
                if (time.Key < caller.Admin()!.MinGagTime || time.Key < caller.Admin()!.MinMuteTime)
                    continue;
            }
            menu.AddMenuOption("silence_time_" + time.Key, time.Value, (_, _) => {
                _api.CloseMenu(caller);
                comm.Duration = time.Key*60;
                Task.Run(async () =>
                {
                    await SilenceFunctions.Silence(comm);
                });
            });
        }
        menu.Open(caller);
    }
}