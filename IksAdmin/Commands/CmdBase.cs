using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdmin.Menus;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin.Commands;

public static class CmdBase
{
    private static AdminApi _api = Main.AdminApi!;
    private static IStringLocalizer _localizer = _api.Localizer;

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
            var targetAdmin = AdminUtils.Admin(target?.GetSteamId() ?? steamId);
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
}