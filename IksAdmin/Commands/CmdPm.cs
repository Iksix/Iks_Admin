using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using IksAdmin.Menus;
using IksAdminApi;

namespace IksAdmin.Commands;

public static class CmdPm
{
    public static AdminApi _api = Main.AdminApi;

    public static void Kick(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_kick <#uid/#steamId/name/@...> <reason>
        var identity = args[0];
        var reason = args[1];
        _api.DoActionWithIdentity(caller, identity,
            (target, _) =>
            {
                if (target!.IsBot || _api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
                {
                    _api.Kick(caller.Admin()!, target, reason);
                }
            },
            blockedArgs: KicksConfig.Config.BlockedIdentifiers
        );
    }
    public static void Slay(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_slay <#uid/#steamId/name/@...>
        var identity = args[0];
        _api.DoActionWithIdentity(caller, identity,
            (target, _) =>
            {
                if (target!.PawnIsAlive)
                {
                    _api.Slay(caller.Admin()!, target);
                }
            },
            blockedArgs: AdminUtils.BlockedIdentifiers("css_slay")
        );
    }
    public static void Respawn(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_respawn <#uid/#steamId/name/@...> [alive also(true/false*)]
        var identity = args[0];
        _api.DoActionWithIdentity(caller, identity,
            (target, type) =>
            {
                if (!target!.PawnIsAlive || (args.Count > 1 && args[1] == "true") || (type is IdentityType.Name or IdentityType.UserId or IdentityType.SteamId))
                {
                    _api.Respawn(caller.Admin()!, target);
                }
            },
            blockedArgs: AdminUtils.BlockedIdentifiers("css_respawn")
        );
    }
    public static void ChangeTeam(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_changeteam <#uid/#steamId/name/@...> <ct/t/spec>
        var identity = args[0];
        var teamNum = args[1].ToLower() switch {
            "ct" => 3,
            "t" => 2,
            "spec" => 1,
            _ => throw new ArgumentException("invalid team")
        };
        _api.DoActionWithIdentity(caller, identity,
            (target, _) =>
            {
                _api.ChangeTeam(caller.Admin()!, target, teamNum);
            },
            blockedArgs: AdminUtils.BlockedIdentifiers("css_changeteam")
        );
    }
    public static void SwitchTeam(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_switchteam <#uid/#steamId/name/@...> <ct/t/spec>
        var identity = args[0];
        var teamNum = args[1].ToLower() switch {
            "ct" => 3,
            "t" => 2,
            "spec" => 1,
            _ => throw new ArgumentException("invalid team")
        };
        _api.DoActionWithIdentity(caller, identity,
            (target, _) =>
            {
                _api.SwitchTeam(caller.Admin()!, target!, teamNum);
            },
            blockedArgs: AdminUtils.BlockedIdentifiers("css_switchteam")
        );
    }
}