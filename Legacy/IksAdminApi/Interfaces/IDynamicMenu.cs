using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace IksAdminApi;

public interface IDynamicMenu : IMenu
{
    public string Id {get; set;}
    public string Title {get; set;}
    public MenuColors TitleColor {get; set;}
    public MenuType Type {get; set;}
    public Action<CCSPlayerController>? BackAction {get; set;}
    public PostSelectAction PostSelectAction {get; set;}
    public List<IDynamicMenuOption> Options {get; set;}
    public void Open(CCSPlayerController player, bool useSortMenu = true);
    public void AddMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, MenuColors? color = null, bool disabled = false, string viewFlags = "*");
}