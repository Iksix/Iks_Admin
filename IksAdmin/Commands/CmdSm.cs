using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;
using Microsoft.Extensions.Localization;
using Microsoft.VisualBasic;

namespace IksAdmin;

public static class CmdSm
{
    static IIksAdminApi _api {get; set;} = Main.AdminApi;
    static IStringLocalizer _localizer {get; set;} = Main.AdminApi.Localizer;

    public static void Servers(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        var servers = _api.AllServers;
        foreach (var server in servers)
        {
            caller.Print(_localizer["Message.SM.Server"].AReplace(
                ["id", "name", "ip"],
                [server.Id, server.Name, server.Ip]
            ));
        }
    }
    public static void Rcon(CCSPlayerController? caller, List<string> args, CommandInfo info)
    {
        // css_rcon <ServerID/this> <CMD>
        int serverId = 0;
        if (args[0] != "this" && !int.TryParse(args[0], out serverId))
        {
            throw new ArgumentException("ServerID must be a number");
        }
        var server = args[0] == "this" ? _api.ThisServer : _api.AllServers.FirstOrDefault(x => x.Id == serverId);
        if (server == null) {
            caller.Print(_localizer["ActionError.ServerNotFound"]);
            return;
        }
        if (!caller.Admin()!.IsConsole && caller.Admin()!.Servers.All(id => id != server.Id) && !caller.Admin()!.Servers.Contains(-1))
        {
            caller.Print(_localizer["ActionError.NotEnoughPermissionsForRcon"]);
            return;
        }
        var cmd = string.Join(" ", args.Skip(1));

        Task.Run(async () => {
            var result = await _api.SendRconToServer(server, cmd);
            Server.NextFrame(() => {
                caller.Print(_localizer["ActionSuccess.RconSuccess"]);
                caller.Print(result, toConsole: true);
            }); 
        });
    }
}