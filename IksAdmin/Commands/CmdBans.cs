using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class CmdBans
{
    public static void Ban(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_ban <#uid/#steamId/name> <time> <reason>
        var identity = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        Main.AdminApi.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            var ban = new PlayerBan(
                new PlayerInfo(target),
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            if (BansConfig.Config.BanOnAllServers) {
                ban.ServerId = null;
            }
            ban.AdminId = caller.Admin()!.Id;
            Task.Run(async () => {
                await BansFunctions.Ban(ban);
            });
        }, blockedArgs: BansConfig.Config.BlockedIdentifiers);
    }

    public static void AddBan(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_addban <steamId> <time> <reason> (для оффлайн бана, так же можно использовать для онлайн бана)
        var steamId = args[0];
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
            var ban = new PlayerBan(
                steamId,
                ip,
                name,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id
            );
            if (BansConfig.Config.BanOnAllServers) {
                ban.ServerId = null;
            }
            ban.AdminId = adminId;
            await BansFunctions.Ban(ban);
        });
    }

    public static void Unban(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var admin = caller.Admin()!;
        var steamId = args[0];
        var reason = args[1];
        Task.Run(async () => {
            await BansFunctions.Unban(admin, steamId, reason);
        });
    }
    public static void UnbanIp(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var admin = caller.Admin()!;
        var ip = args[0];
        var reason = args[1];
        Task.Run(async () => {
            await BansFunctions.UnbanIp(admin, ip, reason);
        });
    }

    public static void BanIp(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_banip <#uid/#steamId/name/@...> <time> <reason>
        var identity = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        Main.AdminApi.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            var ban = new PlayerBan(
                new PlayerInfo(target),
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id,
                banType: 2
            );
            if (BansConfig.Config.BanOnAllServers) {
                ban.ServerId = null;
            }
            ban.AdminId = caller.Admin()!.Id;
            Task.Run(async () => {
                await BansFunctions.Ban(ban);
            });
        }, blockedArgs: BansConfig.Config.BlockedIdentifiers);
    }

    public static void AddBanIp(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_addbanip <ip> <time> <reason>
        var ip = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        string? name = null;
        var target = PlayersUtils.GetControllerByIp(ip);
        var adminId = caller.Admin()!.Id;
        string? steamId = null;
        if (target != null)
        {
            steamId = target.AuthorizedSteamID!.SteamId64.ToString();
        }
        sbyte banType = steamId == null ? (sbyte)1 : (sbyte)2;
        Task.Run(async () => {
            if (Main.AdminApi.Config.WebApiKey != "") 
            {
                var playerSummaryResponse = await Main.AdminApi.GetPlayerSummaries(ulong.Parse(steamId));
                name = playerSummaryResponse!.PersonaName;
            }
            var ban = new PlayerBan(
                steamId,
                ip,
                name,
                reason,
                timeInt,
                serverId: Main.AdminApi.ThisServer.Id,
                banType: banType
            );
            if (BansConfig.Config.BanOnAllServers) {
                ban.ServerId = null;
            }
            ban.AdminId = adminId;
            await BansFunctions.Ban(ban);
        });
    }
}