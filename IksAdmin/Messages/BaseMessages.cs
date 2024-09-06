using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdminApi;
using Microsoft.Extensions.Localization;

// ReSharper disable once CheckNamespace
namespace IksAdmin;

public class BaseMessages
{
    public static IIksAdminApi Api = IksAdmin.Api!;

    public static void SERVER_BAN(PlayerBan info)
    {
        Server.NextFrame(() =>
        {
            Api.SendMessageToAll(ReplaceBan(Api.Localizer["SERVER_OnBan"], info));
        });
    }
    public static void SERVER_GAG(PlayerComm info)
    {
        Server.NextFrame(() =>
        {
            Api.SendMessageToAll(ReplaceComm(Api.Localizer["SERVER_OnGag"], info));
        });
    }
    public static void SERVER_MUTE(PlayerComm info)
    {
        Server.NextFrame(() =>
        {
            Api.SendMessageToAll(ReplaceComm(Api.Localizer["SERVER_OnMute"], info));
        });
    }
    
    public static void SERVER_UNBAN(PlayerBan info)
    {
        Server.NextFrame(() =>
        {
            Api.SendMessageToAll(ReplaceBan(Api.Localizer["SERVER_OnUnBan"], info));
        });
    }
    public static void SERVER_UNMUTE(PlayerComm info)
    {
        Server.NextFrame(() =>
        {
            Api.SendMessageToAll(ReplaceComm(Api.Localizer["SERVER_OnUnMute"], info));
        });
    }
    public static void SERVER_UNGAG(PlayerComm info)
    {
        Server.NextFrame(() =>
        {
            Api.SendMessageToAll(ReplaceComm(Api.Localizer["SERVER_OnUnGag"], info));
        });
    }

    public static void SERVER_KICK(string adminSid, PlayerInfo target, string reason)
    {
        Api.SendMessageToAll(Api.Localizer["SERVER_OnKick"].Value
            .Replace("{name}", target.PlayerName)
            .Replace("{sid}", target.SteamId.SteamId64.ToString())
            .Replace("{admin}", AdminName(adminSid))
            .Replace("{adminSid}", adminSid)
            .Replace("{reason}", reason)
        );
    }
    public static void SERVER_SLAY(string adminSid, PlayerInfo target)
    {
        Api.SendMessageToAll(Api.Localizer["SERVER_OnSlay"].Value
            .Replace("{name}", target.PlayerName)
            .Replace("{sid}", target.SteamId.SteamId64.ToString())
            .Replace("{admin}", AdminName(adminSid))
            .Replace("{adminSid}", adminSid)
        );
    }
    public static void SERVER_SWITCHTEAM(string adminSid, PlayerInfo target, CsTeam oldTeam, CsTeam newTeam)
    {
        Api.SendMessageToAll(Api.Localizer["SERVER_SwitchTeam"].Value
            .Replace("{oldTeam}", XHelper.GetStringFromTeam(oldTeam))
            .Replace("{newTeam}",  XHelper.GetStringFromTeam(newTeam))
            .Replace("{name}", target.PlayerName)
            .Replace("{sid}", target.SteamId.SteamId64.ToString())
            .Replace("{admin}", AdminName(adminSid))
            .Replace("{adminSid}", adminSid)
        );
    }
    public static void SERVER_CHANGETEAM(string adminSid, PlayerInfo target, CsTeam oldTeam, CsTeam newTeam)
    {
        Api.SendMessageToAll(Api.Localizer["SERVER_SwitchTeam"].Value
            .Replace("{oldTeam}", XHelper.GetStringFromTeam(oldTeam))
            .Replace("{newTeam}",  XHelper.GetStringFromTeam(newTeam))
            .Replace("{name}", target.PlayerName)
            .Replace("{sid}", target.SteamId.SteamId64.ToString())
            .Replace("{admin}", AdminName(adminSid))
            .Replace("{adminSid}", adminSid)
        );
    }
    public static void SERVER_RENAME(string adminSid, PlayerInfo target, string oldName, string newName)
    {
        Api.SendMessageToAll(Api.Localizer["SERVER_Rename"].Value
            .Replace("{oldName}", oldName)
            .Replace("{newName}",  newName)
            .Replace("{sid}", target.SteamId.SteamId64.ToString())
            .Replace("{admin}", AdminName(adminSid))
            .Replace("{adminSid}", adminSid)
        );
    }
    private static string ReplaceComm(string message, PlayerComm comm)
    {
        var admin = AdminName(comm.AdminSid);
        var adminSid = comm.AdminSid;
        var unbannedBy = "";
        var unbannedBySid = "";
        if (comm.UnbannedBy != null)
        {
            unbannedBy = AdminName(comm.UnbannedBy);
            unbannedBySid = comm.UnbannedBy;
        }
        return message
            .Replace("{name}", comm.Name)
            .Replace("{reason}", comm.Reason)
            .Replace("{duration}", GetTime(comm.Time))
            .Replace("{unbannedBy}", unbannedBy)
            .Replace("{unbannedBySid}", unbannedBySid)
            .Replace("{admin}", admin)
            .Replace("{adminSid}", adminSid)
            .Replace("{sid}", comm.Sid)
            .Replace("{unbannedBy}", AdminName(unbannedBy))
            .Replace("{unbannedBySid}", unbannedBy)
            .Replace("{serverId}", comm.ServerId)
            .Replace("{end}", XHelper.GetDateStringFromUtc(comm.End))
            .Replace("{created}", XHelper.GetDateStringFromUtc(comm.Created));
    }
    private static string ReplaceBan(string message, PlayerBan ban)
    {
        var admin = AdminName(ban.AdminSid);
        var adminSid = ban.AdminSid;
        var unbannedBy = "";
        var unbannedBySid = "";
        if (ban.UnbannedBy != null)
        {
            unbannedBy = AdminName(ban.UnbannedBy);
            unbannedBySid = ban.UnbannedBy;
        }
        return message
            .Replace("{name}", ban.Name)
            .Replace("{reason}", ban.Reason)
            .Replace("{unbannedBy}", unbannedBy)
            .Replace("{unbannedBySid}", unbannedBySid)
            .Replace("{duration}", GetTime(ban.Time))
            .Replace("{admin}", admin)
            .Replace("{adminSid}", adminSid)
            .Replace("{sid}", ban.Sid)
            .Replace("{ip}", ban.Ip)
            .Replace("{serverId}", ban.ServerId)
            .Replace("{banType}", ban.BanType switch{ 0 => "Normal", 1 => "Ip", _ => "Normal" })
            .Replace("{end}", XHelper.GetDateStringFromUtc(ban.End))
            .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created));
    }

    private static string GetTime(int time)
    {
        time = time / 60;
        if (!Api.Config.Times.ContainsValue(time))
            return $"{time}{Api.Localizer["HELPER_Min"]}";
        return Api.Config.Times.First(x => x.Value == time).Key;
    }

    private static string AdminName(string? adminSid)
    {
        if (adminSid == null) return "CONSOLE";
        var admin = Api.GetAdminBySid(adminSid);
        string adminName = adminSid.ToLower() == "console" ? "CONSOLE" :
            admin == null ? "~Deleted Admin~" : admin.Name;
        return adminName;
    }
}