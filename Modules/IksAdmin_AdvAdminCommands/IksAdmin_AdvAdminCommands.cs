using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdmin.Menus;
using IksAdminApi;
using Microsoft.Extensions.Logging;

namespace IksAdmin_AdvAdminCommands;

public class IksAdminAdvAdminCommands : BasePlugin
{
    public override string ModuleName => "IksAdmin_AdvAdminCommands";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks__";

    private readonly PluginCapability<IIksAdminApi> _adminCapability = new("iksadmin:core");

    private IIksAdminApi? _api;
    
    public void ReplyToCommand(CommandInfo info, string reply, string? replyToConsole = null)
    {
        Server.NextFrame(() =>
        {
            var player = info.CallingPlayer;
            string message = player != null ? reply : replyToConsole == null ? reply : replyToConsole; 
            foreach (var str in message.Split("\n"))
            {
                if (player == null)
                {
                    info.ReplyToCommand($" [IksAdmin] {str}");
                }
                else
                {
                    if (reply.Trim() == "") return;
                    player.PrintToChat($" {_api!.Localizer["PluginTag"]} {str}");
                }
            }
        });
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _adminCapability.Get();
        if (_api == null) Logger.LogError("Gde API cyka?");
        _api!.AddNewCommand(
            "respawn",
            "respawn the players",
            "css_respawn <#uid/#sid/name/>",
            0,
            "respawn",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            OnRespawn
            );
        _api.ModulesOptions.Add(new AdminMenuOption("Respawn", "respawn", "s", "ManagePlayers", OnRespawnSelect));
    }

    private void OnRespawnSelect(CCSPlayerController caller, Admin? admin, IMenu menu)
    {
        OpenSelectPlayersMenu(caller, menu, (playerInfo, _) =>
        {
            var player = XHelper.GetPlayerFromArg("#" + playerInfo.SteamId.SteamId64);
            if (player == null)
            {
                _api!.SendMessageToPlayer(caller, "Player is null!");
                return;
            }
            player.Respawn();
        }, false, title: "Respawn");
    }


    private void OnRespawn(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
        ChatMenu backMenu = new ChatMenu("asd");
        var menu = _api!.CreateMenu(caller!, (p, _, menu) =>
        {
            // у типо реализация какая то
        });
        menu.Open(caller, "Название меню", backMenu);
        if (caller == null && args.Count == 0) return;
        if (args.Count == 0 && caller != null)
        {
            caller.Respawn();
            return;
        }

        var target = XHelper.GetPlayerFromArg(args[0]);
        if (target == null)
        {
            ReplyToCommand(info, _api!.Localizer["NOTIFY_PlayerNotFound"], "Target not found!");
            return;
        }
        
        target.Respawn();
    }
    
    
    private void OpenSelectPlayersMenu(CCSPlayerController caller, IMenu backMenu, Action<PlayerInfo, IMenu> onSelect,
        bool ignoreYourself = true, bool offline = false, string? title = null, bool aliveOnly = false, string? filter = null)
    {
        Menu menu = new Menu(caller, (controller, _, arg3) =>
        {
            ConstructSelectPlayerMenu(controller, arg3, onSelect, ignoreYourself, offline, aliveOnly, filter);
        });
        title = title == null ? Localizer["MENUTITLE_SelectPlayer"] : title;
        menu.Open(caller, title, backMenu);
    }

    private void ConstructSelectPlayerMenu(CCSPlayerController caller, IMenu menu, 
        Action<PlayerInfo, IMenu> onSelect, bool ignoreYourself = true, bool offline = false, bool aliveOnly = false, string? filter = null)
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
            if (filter != null)
            {
                switch (filter)
                {
                    case "gag":
                        if (_api.OnlineGaggedPlayers.Any(x => x.Sid == playerInfo.SteamId.SteamId64.ToString()))
                            continue;
                        break;
                    case "mute":
                        if (_api.OnlineMutedPlayers.Any(x => x.Sid == playerInfo.SteamId.SteamId64.ToString()))
                            continue;
                        break;
                }
            }
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
