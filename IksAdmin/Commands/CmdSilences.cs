
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;

namespace IksAdmin.Commands;


public class CmdSilences
{
    public static void Silence(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_silence <#uid/#sid/name/@...> <time> <reason>
        var identity = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var admin = caller.Admin()!;
        var reason = string.Join(" ", args.Skip(2));
        Main.AdminApi.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            var mute = new PlayerComm(
                new PlayerInfo(target),
                PlayerComm.MuteTypes.Silence,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            mute.AdminId = admin.Id;
            Task.Run(async () => {
                await SilenceFunctions.Silence(mute);
            });
        }, blockedArgs: SilenceConfig.Config.BlockedIdentifiers);
    }
    public static void AddSilence(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_addmute <steamId> <time> <reason> (для оффлайн бана, так же можно использовать для онлайн бана)
        var steamId = args[0];
        if (!ulong.TryParse(steamId, out _)) throw new ArgumentException("Steam id is not a number");
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
            var comm = new PlayerComm(
                steamId,
                ip,
                name,
                PlayerComm.MuteTypes.Silence,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            comm.AdminId = adminId;
            await SilenceFunctions.Silence(comm);
        });
    }
    public static void UnSilence(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_unsilence <#uid/#steamId/name/@...> <reason>
        var identity = args[0];
        var reason = string.Join(" ", args.Skip(1));
        var admin = caller.Admin()!;
        Main.AdminApi.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            var steamId = target.AuthorizedSteamID!.SteamId64.ToString();
            Task.Run(async () => {
                await SilenceFunctions.UnSilence(admin, steamId, reason);
            });
        }, blockedArgs: SilenceConfig.Config.UnblockBlockedIdentifiers);
    }
    public static void RemoveSilence(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_removesilence <steamId> <reason>
        var steamId = args[0];
        var reason = string.Join(" ", args.Skip(1));
        var admin = caller.Admin()!;
        Task.Run(async () => {
            await SilenceFunctions.UnSilence(admin, steamId, reason);
        });
    }
}