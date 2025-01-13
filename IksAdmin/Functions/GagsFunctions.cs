using IksAdminApi;

namespace IksAdmin.Functions;


public static class GagsFunctions
{
    public static AdminApi AdminApi = Main.AdminApi!;

    public static async Task Gag(PlayerComm gag)
    {
        AdminUtils.LogDebug("Add gag... " + gag.SteamId);
        var result = await AdminApi.AddGag(gag);
        AdminUtils.LogDebug("Gag result: " + result.QueryStatus);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(gag.Admin!.SteamId, AdminApi.Localizer["ActionSuccess.GagSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(gag.Admin!.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                break;
            case 2:
                Helper.PrintToSteamId(gag.Admin!.SteamId, AdminApi.Localizer["ActionError.NotEnoughPermissionsForUnban"]);
                break;
            case -1:
                Helper.PrintToSteamId(gag.Admin!.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }
    public static async Task Ungag(Admin admin, string steamId, string reason)
    {
        AdminUtils.LogDebug("Trying to ungag... " + steamId);
        var existingComm = (await AdminApi.GetActiveComms(steamId)).GetGag();
        if (existingComm == null)
        {
            Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.PunishmentNotFound"]);
            return;
        }
        existingComm.UnbanReason = reason;
        var result = await AdminApi.UnComm(admin, existingComm);
        AdminUtils.LogDebug("Ungag result: " + result.QueryStatus);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionSuccess.UnGagSuccess"]);
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