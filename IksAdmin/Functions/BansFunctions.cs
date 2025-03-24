using IksAdminApi;

namespace IksAdmin.Functions;


public static class BansFunctions
{
    public static AdminApi AdminApi = Main.AdminApi;
    public static async Task Ban(PlayerBan ban)
    {
        if (BansConfig.Config.BanOnAllServers) {
            ban.ServerId = null;
        }
        
        if (BansConfig.Config.AlwaysBanForIpAndSteamId && ban.SteamId != null && ban.Ip != null)
        {
            ban.BanType = 2;
        }
        AdminUtils.LogDebug("Add ban... " + ban.SteamId);
        var result = await AdminApi.AddBan(ban);
        AdminUtils.LogDebug("Ban result: " + result.QueryStatus);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(ban.Admin!.SteamId, AdminApi.Localizer["ActionSuccess.BanSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(ban.Admin!.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                break;
            case -1:
                Helper.PrintToSteamId(ban.Admin!.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }

    public static async Task Unban(Admin admin, string steamId, string reason)
    {
        AdminUtils.LogDebug("Trying to unban... " + steamId);
        var result = await AdminApi.Unban(admin, steamId, reason);
        AdminUtils.LogDebug("Unban result: " + result.QueryStatus);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionSuccess.UnBanSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.PunishmentNotFound"]);
                break;
            case 2:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.NotEnoughPermissionsForUnban"]);
                break;
            case -1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }
    public static async Task UnbanIp(Admin admin, string ip, string reason)
    {
        AdminUtils.LogDebug("Trying to unban ip... " + ip);
        var result = await AdminApi.UnbanIp(admin, ip, reason);
        AdminUtils.LogDebug("Unban ip result: " + result.QueryStatus);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionSuccess.UnBanSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.PunishmentNotFound"]);
                break;
            case 2:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.NotEnoughPermissionsForUnban"]);
                break;
            case -1:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }
}