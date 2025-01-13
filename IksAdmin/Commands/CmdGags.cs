
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;

namespace IksAdmin.Commands;


public class CmdGags
{
    public static void Gag(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_gag <#uid/#sid/name/@...> <time> <reason>
        var identity = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        Main.AdminApi.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            var gag = new PlayerComm(
                new PlayerInfo(target),
                PlayerComm.MuteTypes.Gag,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            gag.AdminId = caller.Admin()!.Id;
            Task.Run(async () => {
                await GagsFunctions.Gag(gag);
            });
        }, blockedArgs: GagsConfig.Config.BlockedIdentifiers);
    }
    public static void AddGag(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_addgag <steamId> <time> <reason> (для оффлайн бана, так же можно использовать для онлайн бана)
        var steamId = args[0];
        if (!ulong.TryParse(steamId, out var uSteamID)) throw new ArgumentException("Steam id is not a number");
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        string? name = null;
        string? ip = null;
        var target = PlayersUtils.GetControllerBySteamId(steamId);
        if (target != null)
        {
            ip = target.GetIp();
        }
        var adminId = caller.Admin()!.Id;
        Task.Run(async () => {
            if (Main.AdminApi.Config.WebApiKey != "") 
            {
                var playerSummaryResponse = await Main.AdminApi.GetPlayerSummaries(ulong.Parse(steamId));
                if (playerSummaryResponse != null)
                    name = playerSummaryResponse!.PersonaName;
            }
            var ban = new PlayerComm(
                steamId,
                ip,
                name,
                PlayerComm.MuteTypes.Gag,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            ban.AdminId = adminId;
            await GagsFunctions.Gag(ban);
        });
    }
    public static void Ungag(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_ungag <#uid/#steamId/name/@...> <reason>
        var identity = args[0];
        var reason = string.Join(" ", args.Skip(1));
        var admin = caller.Admin()!;
        Main.AdminApi.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            var steamId = target.AuthorizedSteamID!.SteamId64.ToString();
            Task.Run(async () => {
                await GagsFunctions.Ungag(admin, steamId, reason);
            });
        }, blockedArgs: GagsConfig.Config.UnblockBlockedIdentifiers);
    }
    public static void RemoveGag(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_removegag <steamId> <reason>
        var steamId = args[0];
        var reason = string.Join(" ", args.Skip(1));
        var admin = caller.Admin()!;
        Task.Run(async () => {
            await GagsFunctions.Ungag(admin, steamId, reason);
        });
    }
}