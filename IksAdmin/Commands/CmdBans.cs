using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class CmdBans
{
    private static IIksAdminApi _api = Main.AdminApi;
    public static void Ban(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        //css_ban <#uid/#steamId/name> <time> <reason>
        var identity = args[0];
        var time = args[1];
        if (!int.TryParse(time, out int timeInt)) throw new ArgumentException("Time is not a number");
        var reason = string.Join(" ", args.Skip(2));
        _api.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            if (!_api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
            {
                caller.Print(_api.Localizer["ActionError.NotEnoughPermissionsForAction"]);
                return;
            }
            var ban = new PlayerBan(
                new PlayerInfo(target!),
                reason,
                timeInt,
                serverId: _api.ThisServer.Id
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
        if (!_api.CanDoActionWithPlayer(caller.GetSteamId(), steamId))
        {
            caller.Print(_api.Localizer["ActionError.NotEnoughPermissionsForAction"]);
            return;
        }
        if (target != null)
        {
            ip = target.GetIp();
        }
        var adminId = caller.Admin()!.Id;
        Task.Run(async () => {
            if (_api.Config.WebApiKey != "") 
            {
                var playerSummaryResponse = await _api.GetPlayerSummaries(ulong.Parse(steamId));
                if (playerSummaryResponse != null)
                    name = playerSummaryResponse!.PersonaName;
            }
            var ban = new PlayerBan(
                steamId,
                ip,
                name,
                reason,
                timeInt,
                serverId: _api.ThisServer.Id
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
        _api.DoActionWithIdentity(caller, identity, (target, _) => 
        {
            if (!_api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
            {
                caller.Print(_api.Localizer["ActionError.NotEnoughPermissionsForAction"]);
                return;
            }
            var ban = new PlayerBan(
                new PlayerInfo(target!),
                reason,
                timeInt,
                serverId: _api.ThisServer.Id,
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
            if (!_api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
            {
                caller.Print(_api.Localizer["ActionError.NotEnoughPermissionsForAction"]);
                return;
            }
        }
        sbyte banType = steamId == null ? (sbyte)1 : (sbyte)2;
        Task.Run(async () => {
            if (_api.Config.WebApiKey != "") 
            {
                var playerSummaryResponse = await _api.GetPlayerSummaries(ulong.Parse(steamId!));
                name = playerSummaryResponse!.PersonaName;
            }
            var ban = new PlayerBan(
                steamId,
                ip,
                name,
                reason,
                timeInt,
                serverId: _api.ThisServer.Id,
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