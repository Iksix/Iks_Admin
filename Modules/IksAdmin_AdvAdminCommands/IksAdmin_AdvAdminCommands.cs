using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Menu;
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
    }

    private void OnRespawn(CCSPlayerController? caller, Admin? admin, List<string> args, CommandInfo info)
    {
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
}
