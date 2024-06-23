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
    public Menu(Action<CCSPlayerController, Admin?, IMenu> onOpen, string? uniqueMenuIndex = null)
    {
        index = uniqueMenuIndex;
        OnOpen += onOpen;
    }
}

public abstract class BaseMenu : IBaseMenu
{
    public event Action<CCSPlayerController, Admin?, IMenu>? OnOpen;
    public string? index = null;
    private MenuType _menuType = IksAdmin.Api!.MenuType;
    private IIksAdminApi _api = IksAdmin.Api;
    private IMenuApi _menuManager = IksAdmin.MenuManager!;
    
    public void Open(CCSPlayerController caller, string title, IMenu? backMenu = null)
    {
        Open(caller, title, null, backMenu);
    }
    
    public void Open(CCSPlayerController caller, string title, string? menuTag, IMenu? backMenu = null)
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
        
        var admin = _api.ThisServerAdmins.FirstOrDefault(x =>
            x.SteamId == caller.AuthorizedSteamID!.SteamId64.ToString());
        
        
        OnOpen?.Invoke(caller, admin, menu);
        
        if (index != null)
            _api.EOnMenuOpen(index, menu, caller);
        
        menu.Open(caller);
    }

    private void OpenBackMenu(CCSPlayerController player, IMenu menu)
    {
        menu.Open(player);
    }
}

