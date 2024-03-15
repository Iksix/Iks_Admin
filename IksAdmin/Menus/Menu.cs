using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdminApi;

namespace IksAdmin.Menus;


public class Menu : BaseMenu
{
    public Menu(CCSPlayerController caller,Action<CCSPlayerController, Admin?, IMenu> onOpen)
    {
        OnOpen += onOpen;
    }
}

public abstract class BaseMenu : IBaseMenu
{
    public event Action<CCSPlayerController, Admin?, IMenu>? OnOpen;
    private IIksAdminApi.UsedMenuType _menuType = IksAdmin.Api!.MenuType;
    private IIksAdminApi _api = IksAdmin.Api!;
    public void Open(CCSPlayerController caller, string title, IMenu? backMenu = null)
    {
        IMenu menu;
        if (_menuType == IIksAdminApi.UsedMenuType.Html) menu = new CenterHtmlMenu(title);
        else
        {
            menu = new ChatMenu($" {_api.Localizer["PluginTag"]} {title}");
            menu.AddMenuOption(_api.Localizer["MENUOPTION_Close"], (p, _) =>
            {
                MenuManager.CloseActiveMenu(p);
                p.PrintToChat($" {_api.Localizer["PluginTag"]} {_api.Localizer["NOTIFY_MenuClosed"]}");
            });
        }
        var admin = _api.ThisServerAdmins.FirstOrDefault(x =>
            x.SteamId == caller.AuthorizedSteamID!.SteamId64.ToString());

        if (backMenu != null)
        {
            menu.AddMenuOption(_api.Localizer["MENUOPTION_Back"], (p, _) =>
            {
                if (_menuType == IIksAdminApi.UsedMenuType.Html) MenuManager.OpenCenterHtmlMenu(_api.Plugin, caller, (CenterHtmlMenu)backMenu);
                else MenuManager.OpenChatMenu(caller, (ChatMenu)backMenu);
            });
        }
        
        OnOpen?.Invoke(caller, admin, menu);
        
        if (_menuType == IIksAdminApi.UsedMenuType.Html) MenuManager.OpenCenterHtmlMenu(_api.Plugin, caller, (CenterHtmlMenu)menu);
        else MenuManager.OpenChatMenu(caller, (ChatMenu)menu);
    }
}

