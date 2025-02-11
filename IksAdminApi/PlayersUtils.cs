using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace IksAdminApi;

public static class PlayersUtils
{
    // HTML MESSAGES
    public static Dictionary<CCSPlayerController, string> HtmlMessages = new();
    public static Dictionary<CCSPlayerController, Timer> HtmlMessagesTimer = new();
    public static void HtmlMessage(this CCSPlayerController player, string message, float time = 1)
    {
        ClearHtmlMessage(player);
        if (message == "") return;
        HtmlMessages.Add(player, message);
        HtmlMessagesTimer.Add(player, AdminUtils.CoreInstance.AddTimer(time, () =>
        {
            ClearHtmlMessage(player);
        }));
    }
    public static void ClearHtmlMessage(this CCSPlayerController player)
    {
        HtmlMessages.Remove(player);
        if (HtmlMessagesTimer.TryGetValue(player, out var timer))
        {
            timer.Kill();
            HtmlMessagesTimer.Remove(player);
        }
    }
    public static void CloseMenu(this CCSPlayerController player)
    {
        AdminUtils.CoreApi.CloseMenu(player);
    }
    /// <summary>
    /// may cause errors
    /// </summary>
    public static CCSPlayerController? GetControllerBySteamIdUnsafe(string steamId)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && !x.IsBot && x.SteamID.ToString() == steamId);
    }
    public static CCSPlayerController? GetControllerBySteamId(string steamId)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.AuthorizedSteamID != null && x.AuthorizedSteamID.SteamId64.ToString() == steamId);
    }
    public static CCSPlayerController? GetControllerByUid(uint userId)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.Connected == PlayerConnectedState.PlayerConnected && x.UserId == userId);
    }
    public static CCSPlayerController? GetControllerByName(string name, bool ignoreRegistry = false)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.Connected == PlayerConnectedState.PlayerConnected && (ignoreRegistry ? x.PlayerName.ToLower().Contains(name) : x.PlayerName.Contains(name)));
    }
    public static CCSPlayerController? GetControllerByIp(string ip)
    {
        return Utilities.GetPlayers().FirstOrDefault(x => x != null && x.IsValid && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected && x.GetIp() == ip);
    }
    public static List<CCSPlayerController> GetOnlinePlayers(bool includeBots = false)
    {
        if (includeBots)
            return Utilities.GetPlayers().Where(x => x != null && x.IsValid && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
        return Utilities.GetPlayers().Where(x => x != null && x.IsValid && !x.IsBot && x.AuthorizedSteamID != null && x.Connected == PlayerConnectedState.PlayerConnected).ToList();
    }
}