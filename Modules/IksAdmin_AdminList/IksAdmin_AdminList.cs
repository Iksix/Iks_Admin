using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdminApi;
using Microsoft.Extensions.Logging;

namespace IksAdmin_AdminList;

public class IksAdminAdminList : BasePlugin
{
    public override string ModuleName => "IksAdmin_AdminList";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks__";
    public static PluginCapability<IIksAdminApi> _adminCapability = new("iksadmin:core");

    private IIksAdminApi? _api;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        try
        {
            _api = _adminCapability.Get();
        }
        catch (Exception e)
        {
            Logger.LogError("IksAdminApi.dll nety :(");
            return;
        }
        _api!.AddNewCommand(
            "admins",
            "view admins online list",
            "css_admins",
            0,
            "*",
            "*",
            CommandUsage.CLIENT_ONLY,
            OnAdminsCommand
            );
    }

    private void OnAdminsCommand(CCSPlayerController caller, Admin? admin, List<string> args, CommandInfo info)
    {
        var menu = _api!.CreateMenu(caller, OpenAdminsMenu);
        menu.Open(caller, "Admin list");
    }

    private void OpenAdminsMenu(CCSPlayerController caller, Admin? _, IMenu menu)
    {
        menu.PostSelectAction = PostSelectAction.Close;
        var admins = _api!.ThisServerAdmins;
        var players = Utilities.GetPlayers().Where(p => p.Connected == PlayerConnectedState.PlayerConnected);

        foreach (var player in players)
        {
            if (player.AuthorizedSteamID == null) continue;
            var playerSid = player.AuthorizedSteamID!.SteamId64.ToString();
            if (admins.All(x => x.SteamId != playerSid)) continue;
            menu.AddMenuOption(player.PlayerName, (_, _) =>
            {
                var admin = admins.First(x => x.SteamId == playerSid);
                _api.SendMessageToPlayer(caller, $"=====================");
                _api.SendMessageToPlayer(caller, $"Name: {player.PlayerName}");
                _api.SendMessageToPlayer(caller, $"Group: {admin.GroupName}");
                _api.SendMessageToPlayer(caller, $"Flags: {admin.Flags}");
                _api.SendMessageToPlayer(caller, $"Immunity: {admin.Immunity}");
                _api.SendMessageToPlayer(caller, $"=====================");
            });
        }
    }
}
