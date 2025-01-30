using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin;

public static class MenuSM
{
    private static IIksAdminApi _api = Main.AdminApi;
    private static IStringLocalizer _localizer = _api.Localizer;

    public static void OpenRconMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null)
    {
        MenuUtils.SelectItem<ServerModel?>(caller, "rcon_server", "Name", _api.AllServers.Where(x => caller.Admin()!.Servers.Contains(x.Id) || caller.Admin()!.OnAllServers).ToList()!,
        (s, m) => {
            caller.Print(_localizer["Message.SM.Rcon.WriteCmd"]);
            _api.HookNextPlayerMessage(caller, cmd => {
                Task.Run(async () => {
                    var result = await _api.SendRconToServer(s!, cmd);
                    Server.NextFrame(() => {
                        caller.Print(_localizer["ActionSuccess.RconSuccess"]);
                        caller.Print(result, toConsole: true);
                    }); 
                });
            });
        }, backMenu: backMenu, nullOption: false
        );
    }
}