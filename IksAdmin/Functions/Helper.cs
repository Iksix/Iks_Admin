using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace IksAdmin.Functions;

public static class Helper
{
    public static void SetSortMenus()
    {
        using var streamReader = new StreamReader($"{Main.AdminApi.ModuleDirectory}/sortmenus.json");
        string json = streamReader.ReadToEnd();
        var sortMenus = JsonSerializer.Deserialize<Dictionary<string, SortMenu[]>>(json, new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip })!;
        Main.AdminApi.SortMenus = sortMenus;
        AdminUtils.LogDebug("Sort Menus setted!");
        foreach (var item in Main.AdminApi.SortMenus)
        {
            AdminUtils.LogDebug($@"Menu key: {item.Key}");
            AdminUtils.LogDebug($@"Menu options: ");
            AdminUtils.LogDebug($@"ID | ViewFlags | View");
            foreach (var option in item.Value)
            {
                AdminUtils.LogDebug($@"{option.Id} | {option.ViewFlags} | {option.View}");
            }
        }
    }
    public static void Print(CCSPlayerController? player, string message)
    {
        AdminUtils.LogDebug("Print: " + message);
        Server.NextWorldUpdate(() => {
            player.Print(message, Main.AdminApi.Localizer["Tag"]);
        });
    }
    public static void PrintToSteamId(string steamId, string message)
    {
        AdminUtils.LogDebug("Print to steamId: " + message);
        if (steamId == "CONSOLE")
        {
            Console.WriteLine(message);
            return;
        }
        Server.NextWorldUpdate(() => {
            var controller = PlayersUtils.GetControllerBySteamId(steamId);
            controller.Print(message, Main.AdminApi.Localizer["Tag"]);
        });
    }
    public static void PrintToAdmin(Admin admin, string message)
    {
        AdminUtils.LogDebug("Print to admin: " + message);
        if (admin.SteamId == "CONSOLE")
        {
            Console.WriteLine(message);
            return;
        }
        Server.NextWorldUpdate(() => {
            var controller = PlayersUtils.GetControllerBySteamId(admin.SteamId);
            controller.Print(message, Main.AdminApi.Localizer["Tag"]);
        });
    }
    public static void Reply(CommandInfo info, string message)
    {
        Server.NextWorldUpdate(() => {
            info.Reply(message, Main.AdminApi.Localizer["Tag"]);
        });
    }
}