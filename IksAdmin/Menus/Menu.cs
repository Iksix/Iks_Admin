using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdminApi;
using MenuManager;


namespace IksAdmin;


public class Menu : BaseMenu
{
    [Obsolete]
    public Menu(CCSPlayerController caller,Action<CCSPlayerController, Admin?, IMenu> onOpen)
    {
        OnOpen += onOpen;
    }
    
    public Menu(Action<CCSPlayerController, Admin?, IMenu> onOpen)
    {
        OnOpen += onOpen;
    }
}

public abstract class BaseMenu : IBaseMenu
{
    public event Action<CCSPlayerController, Admin?, IMenu>? OnOpen;
    private MenuType _menuType = IksAdmin.Api!.MenuType;
    private IIksAdminApi _api = IksAdmin.Api;
    private IMenuApi _menuManager = IksAdmin.MenuManager!;
    
    public void Open(CCSPlayerController caller, string title, IMenu? backMenu = null, string? menuTag = null)
    {
        var tag = menuTag == null ? _api.Localizer["PluginTag"] : menuTag;
        if ((_menuType == MenuType.Default && _menuManager.GetMenuType(caller) == MenuType.ChatMenu) || _menuType == MenuType.ChatMenu)
        {
            title = tag + $" {title}";
        }
        IMenu menu = _menuManager.NewMenuForcetype(title, _menuType);
        if (backMenu != null)
        {
            menu = _menuManager.NewMenuForcetype(title, _menuType, p => { OpenBackMenu(p, backMenu); });
        }
        
        // if (_menuType != MenuType.ButtonMenu)
        // {
        //     menu.AddMenuOption(_api.Localizer["MENUOPTION_Close"], (p, _) =>
        //     {
        //         _menuManager.CloseMenu(p);
        //         p.PrintToChat($" {_api.Localizer["PluginTag"]} {_api.Localizer["NOTIFY_MenuClosed"]}");
        //     });
        // }
        
        var admin = _api.ThisServerAdmins.FirstOrDefault(x =>
            x.SteamId == caller.AuthorizedSteamID!.SteamId64.ToString());
        
        
        OnOpen?.Invoke(caller, admin, menu);
        
        menu.Open(caller);
    }

    private void OpenBackMenu(CCSPlayerController player, IMenu menu)
    {
        menu.Open(player);
    }
}

