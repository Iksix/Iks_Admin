using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using MenuManager;
using IksAdminApi;
using CounterStrikeSharp.API.Modules.Utils;
using MenuType = IksAdminApi.MenuType;
using CounterStrikeSharp.API.Core.Translations;

namespace IksAdmin.Menu;

public class DynamicMenu : IDynamicMenu
{
    public string Id {get; set;}
    public string Title {get; set;} = "Dynamic Menu";
    public MenuColors TitleColor {get; set;}
    public MenuType Type {get; set;} = MenuType.Default;
    public Action<CCSPlayerController>? BackAction {get; set;} = null;
    public PostSelectAction PostSelectAction {get; set;} = PostSelectAction.Nothing;
    public List<IDynamicMenuOption> Options {get; set;} = new();
    private bool _backOptionRendered = false;
    public DynamicMenu(string id, string title, MenuType type = (MenuType)3, MenuColors titleColor = MenuColors.Default, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IDynamicMenu? backMenu = null)
    {
        Id = id;
        Title = title;
        TitleColor = titleColor;
        Type = type;
        PostSelectAction = postSelectAction;
        BackAction = backAction;
        if (backMenu != null)
        {
            BackAction = player => backMenu.Open(player);
        }
        
        AdminUtils.LogDebug($@"
            Menu created:
            Id: {Id}
            Title: {Title}
            Type: {Type}
        ");
    }

    public void Open(CCSPlayerController player, bool useSortMenu = true)
    {
        AdminUtils.LogDebug($@"
            Open menu... :
            Player: {player.PlayerName} | [{player.AuthorizedSteamID!.SteamId64}]
            Id: {Id}
            Title: {Title}
            Type: {Type}
            useSortMenu: {useSortMenu}
        ");

        IMenu menu;
        switch ((int)Type)
        {
            case -1: // [MM]
                menu = Main.MenuApi!.NewMenu(MenuTitle(player));
                break;
            case 0:
                menu = new ChatMenu(MenuTitle(player));
                break;
            case 1:
                menu = new ConsoleMenu(MenuTitle(player));
                break;
            case 2:
                menu = new CenterHtmlMenu(MenuTitle(player), Main.AdminApi.Plugin);
                break;
            case 3: // [MM]
                menu = Main.MenuApi!.NewMenuForcetype(MenuTitle(player), (MenuManager.MenuType)Type);
                break;
            default:
                menu = new CenterHtmlMenu(MenuTitle(player), Main.AdminApi.Plugin);
                break;
        }
        
        menu.PostSelectAction = PostSelectAction;
        var oldOptions = Options.ToList();
        var onMenuOpenPreResult = Main.AdminApi.OnMenuOpenPre(player, this, menu);
        if (!onMenuOpenPreResult)
        {
            return;
        }
        if (_backOptionRendered)
        {
            Options.RemoveAt(0);
            _backOptionRendered = false;
        }
        if (BackAction != null) { // Отрисовка пункта 'Назад'
            Options.Insert(0, new DynamicMenuOption("back_btn", Main.AdminApi.Localizer["MenuOption.Other.Back"], (p, _) => {
                BackAction.Invoke(p);
            }, null, false));
            _backOptionRendered = true;
        }
        
        if (useSortMenu)
        {
            var options = Options.ToList();
            if (Main.AdminApi.SortMenus.TryGetValue(Id, out var sortMenu))
            {
                AdminUtils.LogDebug("With sort menu");
                foreach (var sort in sortMenu)
                {
                    var option = options.FirstOrDefault(x => x.Id == sort.Id);
                    if (option == null) continue;
                    if (!sort.View)
                    {
                        options.Remove(option);
                        continue;
                    }
                    var viewFlags = sort.ViewFlags.ToLower() == "not override" ? option.ViewFlags : sort.ViewFlags;
                    if (!viewFlags.Contains("*"))
                    {
                        if (player.Admin() == null)
                        {
                            options.Remove(option);
                            continue;
                        }
                        var adminFlags = player.Admin()!.CurrentFlags.ToCharArray();
                        if (!adminFlags.Any(viewFlags.Contains) && !adminFlags.Contains('z'))
                        {
                            options.Remove(option);
                            continue;
                        }
                    }
                    if (!Main.AdminApi.OnOptionRenderPre(player, this, menu, option)) continue;
                    menu.AddMenuOption(OptionTitle(player, option), (_, _) => {
                        if(!Main.AdminApi.OnOptionExecutedPre(player, this, menu, option)) return; 
                        option.OnExecute(player, option);
                        if(!Main.AdminApi.OnOptionExecutedPost(player, this, menu, option)) return; 
                    }, option.Disabled);
                    options.Remove(option);
                    if (!Main.AdminApi.OnOptionRenderPost(player, this, menu, option)) continue;
                }
                foreach (var option in options)
                {
                    // Проверка на ViewFlags
                    var viewFlags = option.ViewFlags; // Текущие ViewFlags опции
                    if (!viewFlags.Contains("*")) // Если не содержит *, то проверяем на ViewFlags админа
                    {
                        if (player.Admin() == null)
                        {
                            continue;
                        }
                        var adminFlags = player.Admin()!.CurrentFlags.ToCharArray();
                        if (!adminFlags.Any(viewFlags.Contains) && !adminFlags.Contains('z'))
                        {
                            continue;
                        }
                    }
                    if (!Main.AdminApi.OnOptionRenderPre(player, this, menu, option)) continue;
                    menu.AddMenuOption(OptionTitle(player, option), (_, _) => {
                        if(!Main.AdminApi.OnOptionExecutedPre(player, this, menu, option)) return; 
                        option.OnExecute(player, option);
                        if(!Main.AdminApi.OnOptionExecutedPost(player, this, menu, option)) return; 
                    }, option.Disabled); 
                    Main.AdminApi.OnOptionRenderPost(player, this, menu, option);
                }
            } else {
                useSortMenu = false;
            }
        }
        if (!useSortMenu)
        {
            AdminUtils.LogDebug("Without sort menu");
            foreach (var option in Options)
            {
                var viewFlags = option.ViewFlags; // Текущие ViewFlags опции
                if (!viewFlags.Contains("*")) // Если не содержит *, то проверяем на ViewFlags админа
                {
                    if (player.Admin() == null)
                    {
                        continue;
                    }
                    var adminFlags = player.Admin()!.CurrentFlags.ToCharArray();
                    if (!adminFlags.Any(viewFlags.Contains) && !adminFlags.Contains('z'))
                    {
                        continue;
                    }
                }
                if (!Main.AdminApi.OnOptionRenderPre(player, this, menu, option)) continue;
                menu.AddMenuOption(OptionTitle(player, option), (_, _) => {
                    if(!Main.AdminApi.OnOptionExecutedPre(player, this, menu, option)) return; 
                    option.OnExecute(player, option);
                    if(!Main.AdminApi.OnOptionExecutedPost(player, this, menu, option)) return; 
                }, option.Disabled);
                Main.AdminApi.OnOptionRenderPost(player, this, menu, option);
            }
        }
        menu.Open(player); 
        Main.AdminApi.OnMenuOpenPost(player, this, menu);
        Options = oldOptions;
    }

    private string MenuTitle(CCSPlayerController player)
    {
        string colorString = GetMenuColorString(player, TitleColor);
        var fullTitleString = colorString.Replace("{value}", RemoveDangerSymbols(player, Title));
        AdminUtils.LogDebug($"Full title string: {fullTitleString}");
        return fullTitleString;
    }

    public string OptionTitle(CCSPlayerController player, IDynamicMenuOption option)
    {
        string colorString = GetMenuColorString(player, option.Color);
        var fullOptionString = colorString.Replace("{value}", RemoveDangerSymbols(player, option.Title));
        AdminUtils.LogDebug($"Full option string: {fullOptionString}");
        return fullOptionString;
    }

    private string RemoveDangerSymbols(CCSPlayerController player, string str)
    {
        var menuType = GetThisMenuType(player);
        if (menuType != MenuType.ButtonMenu && menuType != MenuType.CenterMenu)
        {
            return str;
        }
        var replaced = str.Replace("<", "");
        replaced = replaced.Replace(">", "");
        return replaced;
    }
    public MenuType GetThisMenuType(CCSPlayerController player)
    {
        var menuType = Type != MenuType.Default ? Type : (MenuType)Main.MenuApi!.GetMenuType(player);
        return menuType;
    }

    private string GetMenuColorString(CCSPlayerController player, MenuColors color)
    {
        // char[] chatColors = new char[] {
        //     ChatColors.Default, ChatColors.White, ChatColors.DarkRed, ChatColors.Green, ChatColors.LightYellow, ChatColors.LightBlue, ChatColors.Olive, ChatColors.Lime, ChatColors.Red, ChatColors.LightPurple, ChatColors.Purple, ChatColors.Grey, ChatColors.Yellow, ChatColors.Gold, ChatColors.Silver, ChatColors.Blue, ChatColors.DarkBlue, ChatColors.BlueGrey, ChatColors.Magenta, ChatColors.LightRed, ChatColors.Orange, ChatColors.DarkRed
        // };
        // var menuType = GetThisMenuType(player);
        // if (menuType == MenuType.ChatMenu)
        // {
        //     return $"{chatColors[(int)color]}" + "{value}";
        // } else if (menuType == MenuType.ButtonMenu)
        // {
        //     string[] htmlColors = new string[] {
        //         "<font color='white'>",
        //         "<font color='white'>",
        //         "<font color='darkred'>",
        //         "<font color='green'>",
        //         "<font color='lightyellow'>",
        //         "<font color='lightblue'>",
        //         "<font color='olive'>",
        //         "<font color='lime'>",
        //         "<font color='red'>",
        //         "<font color='lightpurple'>",
        //         "<font color='purple'>",
        //         "<font color='grey'>",
        //         "<font color='yellow'>",
        //         "<font color='gold'>",
        //         "<font color='silver'>",
        //         "<font color='blue'>",
        //         "<font color='darkblue'>",
        //         "<font color='lightred'>",
        //         "<font color='orange'>",
        //         "<font color='darkred'>"
        //     };
        //     return $"{htmlColors[(int)color]}" + "{value}</font>";
        // }
        
        return "{value}";
    }

    private string GetOnlyColorString(MenuColors color)
    {
        string[] colors = new string[] {
                "white",
                "white",
                "darkred",
                "green",
                "lightyellow",
                "lightblue",
                "olive",
                "lime",
                "red",
                "lightpurple",
                "purple",
                "grey",
                "yellow",
                "gold",
                "silver",
                "blue",
                "darkblue",
                "lightred",
                "orange",
                "darkred"
            };
        return colors[(int)color];
    }
    public void AddMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, MenuColors? color = null, bool disabled = false, string viewFlags = "*")
    {
        if (Options.Any(x => x.Id == id))
        {
            AdminUtils.LogDebug($"Option \"{id}\" already exists.");
        }
        var option = new DynamicMenuOption(id, title, onExecute, color, disabled, viewFlags);
        Options.Add(option);
    }
}

public class DynamicMenuOption : IDynamicMenuOption
{
    public string Id {get; set;}
    public string Title { get; set; } = "Option";
    public MenuColors Color { get; set; }
    public Action<CCSPlayerController, IDynamicMenuOption> OnExecute {get; set;}
    public bool Disabled { get; set; }
    public string ViewFlags { get; set; } = "*";

    public DynamicMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, MenuColors? color = null, bool disabled = false, string viewFlags = "*")
    {
        Id = id;
        Title = title;
        Color = color ?? MenuColors.Default;
        OnExecute = onExecute;
        Disabled = disabled;
        ViewFlags = viewFlags;

        AdminUtils.LogDebug($@"
            Option created:
            Id: {id}
            Title: {title}
            Color: {color}
            ViewFlags: {viewFlags}
            Disabled: {disabled}
        ");
    }
}

