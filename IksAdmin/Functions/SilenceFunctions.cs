using IksAdminApi;

namespace IksAdmin.Functions;


public static class SilenceFunctions
{
    public static AdminApi AdminApi = Main.AdminApi!;

    public static async Task Silence(PlayerComm comm)
    {
        if (AdminApi.CheckCooldown(comm.Admin!.USteamId, "silence"))
            return;
        
        AdminUtils.LogDebug("Add silence... " + comm.SteamId);
        var result = await AdminApi.AddComm(comm);
        AdminUtils.LogDebug("Silence result: " + result.QueryStatus);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(comm.Admin!.SteamId, AdminApi.Localizer["ActionSuccess.SilenceSuccess"]);
                break;
            case 1:
                Helper.PrintToSteamId(comm.Admin!.SteamId, AdminApi.Localizer["ActionError.AlreadyBanned"]);
                break;
            case 2:
                Helper.PrintToSteamId(comm.Admin!.SteamId, AdminApi.Localizer["ActionError.NotEnoughPermissionsForUnban"]);
                break;
            case -1:
                Helper.PrintToSteamId(comm.Admin!.SteamId, AdminApi.Localizer["ActionError.Other"]);
                break;
        }
    }
    public static async Task UnSilence(Admin admin, string steamId, string reason)
    {
        if (AdminApi.CheckCooldown(admin!.USteamId, "unsilence"))
            return;
        
        AdminUtils.LogDebug("Trying to unsilence... " + steamId);
        var existingComm = (await AdminApi.GetActiveComms(steamId)).GetSilence();
        if (existingComm == null)
        {
            Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionError.PunishmentNotFound"]);
            return;
        }
        existingComm.UnbanReason = reason;
        var result = await AdminApi.UnComm(admin, existingComm);
        AdminUtils.LogDebug("Unsilence result: " + result.QueryStatus);
        switch (result.QueryStatus)
        {
            case 0:
                Helper.PrintToSteamId(admin.SteamId, AdminApi.Localizer["ActionSuccess.UnSilenceSuccess"]);
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