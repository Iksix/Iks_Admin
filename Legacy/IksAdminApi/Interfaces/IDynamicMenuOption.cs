using CounterStrikeSharp.API.Core;
namespace IksAdminApi;

public interface IDynamicMenuOption
{
    public string Id {get; set;}
    public string Title { get; set; }
    public MenuColors Color { get; set; }
    public Action<CCSPlayerController, IDynamicMenuOption> OnExecute {get; set;}
    public bool Disabled { get; set; }
    public string ViewFlags { get; set; }
}

