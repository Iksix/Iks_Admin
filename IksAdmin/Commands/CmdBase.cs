using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using CoreRCON.Parsers.Standard;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdmin.Functions;
using IksAdmin.Menus;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Commands;

public static class CmdBase
{
    private static AdminApi _api = Main.AdminApi!;
    private static IStringLocalizer _localizer = _api.Localizer;
    public static List<CCSPlayerController> HidenPlayers = new();
    public static List<CCSPlayerController> FirstMessage = new();

    public static void AdminMenu(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        MenuMain.OpenAdminMenu(caller!);
    }

    public static void Reload(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        caller.Print("Reloading DB data...");
        Task.Run(async () =>
        {
            if (args.Count > 0 && args[0] == "all")
                await _api.ReloadDataFromDb();
            else await _api.ReloadDataFromDb(false);
            Server.NextFrame(() => {
                caller.Print( "DB data reloaded \u2714");
            });
        });
    }

    public static void ReloadInfractions(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var steamId = args[0];
        var player = PlayersUtils.GetControllerBySteamId(steamId) ?? PlayersUtils.GetControllerByIp(steamId);
        if (player == null) return;
        string? ip = player.GetIp();
        Task.Run(async () =>
        {
            await _api.ReloadInfractions(steamId, ip);
        });
    }

    public static void Who(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var identity = args[0];
        _api.DoActionWithIdentity(caller, identity, (target, type) =>
        {
            if (target != null && target.IsBot) return;
            var steamId = identity.Remove(0, 1);
            var targetAdmin = AdminUtils.ServerAdmin(target?.GetSteamId() ?? steamId);
            if (targetAdmin == null)
            {
                caller.Print(_localizer["Message.CmdWho_NotAdmin"].AReplace(
                    ["name", "steamId"],
                    [target?.PlayerName ?? steamId, target.GetSteamId()]
                ));
                return;
            }
            caller.Print(_localizer["Message.CmdWho"].AReplace(
                ["id", "name", "steamId", "group", "flags", "immunity"],
                [targetAdmin.Id, target?.PlayerName ?? targetAdmin.Name, target.GetSteamId(), targetAdmin.Group?.Name ?? "", targetAdmin.CurrentFlags, targetAdmin.CurrentImmunity]
            ));
        }, blockedArgs: ["@bots"], acceptNullSteamIdPlayer: true);
        
    }

    public static void Hide(CCSPlayerController caller, List<string> args, CommandInfo info)
    {
        if (HidenPlayers.Contains(caller))
        {
            HidenPlayers.Remove(caller);
            _api.HidenAdmins.Remove(caller.Admin()!);
            caller.Print(_localizer["Message.Hide_off"]);
            caller.ChangeTeam(CsTeam.Spectator);
            return;
        }
        HidenPlayers.Add(caller);
        _api.HidenAdmins.Remove(caller.Admin()!);
        _api.HidenAdmins.Add(caller.Admin()!);
        FirstMessage.Add(caller);
        Server.ExecuteCommand("sv_disable_teamselect_menu 1");
        if (caller.PlayerPawn.Value != null && caller.PawnIsAlive)
            caller.PlayerPawn.Value.CommitSuicide(true, false);
        _api!.Plugin.AddTimer(1.0f, () => { Server.NextFrame(() => caller.ChangeTeam(CsTeam.Spectator)); HidenPlayers.Add(caller); FirstMessage.Add(caller); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        _api.Plugin.AddTimer(1.4f, () => { Server.NextFrame(() => caller.ChangeTeam(CsTeam.None)); caller.Print(_localizer["Message.Hide_on"]); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        _api.Plugin.AddTimer(2.0f, () => { Server.NextFrame(() => Server.ExecuteCommand("sv_disable_teamselect_menu 0")); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
    }

    public static void Status(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var players = PlayersUtils.GetOnlinePlayers(true);
        if (args.Count > 0 && args[0] == "json")
        {
            if (args.Count > 1 && args[1] == "offline")
            {
                JsonPlayersReturn(_api.DisconnectedPlayers.GetRange(0, Math.Min(30, _api.DisconnectedPlayers.Count())));
                return;
            }
            var infos = new List<PlayerInfo>();
            foreach (var player in players)
            {
                infos.Add(new PlayerInfo(player));
            }
            JsonPlayersReturn(infos);
            return;
        }
        
        if (args.Count == 0)
        {
            var str = "=== STATUS ===\n<UID> <\"name\"> <SteamId64> <slot>\n";
            foreach (var player in players)
            {
                str += $"{player.UserId} \"{player.PlayerName}\" {(player.AuthorizedSteamID == null ? "BOT" : player.GetSteamId())} {player.Slot}\n";
            }
            if (caller != null)
            {
                caller.PrintToConsole(str);
            } else {
                Server.PrintToConsole(str);
            }
            return;
        }

        
    }
    private static void JsonPlayersReturn(List<PlayerInfo> infos)
    {
       var json = JsonSerializer.Serialize(infos, options: new JsonSerializerOptions() {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.Cyrillic)
       });
       Server.PrintToConsole(json);
    }

    public static void LVoices(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        caller.Print(_localizer["Other.LVoices"]);
        var count = 1;
        foreach (var player in Main.LastClientVoices)
        {
            caller.Print(_localizer["Other.LVoicesTemplate"].AReplace(
                ["i", "uid", "name", "steamId", "date"],
                [count, player.UserId, player.PlayerName, player.SteamId!, Utils.GetDateString(Main.LastClientVoicesTime[player.SteamId!], "HH:mm:ss")]
            ));
            count++;
        }
    }
}