
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;

namespace IksAdmin.Commands;


public class CmdMutes
{
    public static void Mute(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_mute <#uid/#sid/name/@...> <time> <reason>
        var identity = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var admin = caller.Admin()!;
        var reason = string.Join(" ", args.Skip(2));
        Main.AdminApi.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            var mute = new PlayerComm(
                new PlayerInfo(target),
                PlayerComm.MuteTypes.Mute,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            mute.AdminId = admin.Id;
            Task.Run(async () => {
                await MutesFunctions.Mute(mute);
            });
        }, blockedArgs: MutesConfig.Config.BlockedIdentifiers);
    }
    public static void AddMute(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_addmute <steamId> <time> <reason> (для оффлайн бана, так же можно использовать для онлайн бана)
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
            var mute = new PlayerComm(
                steamId,
                ip,
                name,
                PlayerComm.MuteTypes.Mute,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            mute.AdminId = adminId;
            await MutesFunctions.Mute(mute);
        });
    }
    public static void Unmute(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_unmute <#uid/#steamId/name/@...> <reason>
        var identity = args[0];
        var reason = string.Join(" ", args.Skip(1));
        var admin = caller.Admin()!;
        Main.AdminApi.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            var steamId = target.AuthorizedSteamID!.SteamId64.ToString();
            Task.Run(async () => {
                await MutesFunctions.Unmute(admin, steamId, reason);
            });
        }, blockedArgs: MutesConfig.Config.UnblockBlockedIdentifiers);
    }
    public static void RemoveMute(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_removemute <steamId> <reason>
        var steamId = args[0];
        var reason = string.Join(" ", args.Skip(1));
        var admin = caller.Admin()!;
        Task.Run(async () => {
            await MutesFunctions.Unmute(admin, steamId, reason);
        });
    }
}