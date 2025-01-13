using CounterStrikeSharp.API.Core;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin;

public static class MsgOther
{
    private static AdminApi _api = Main.AdminApi!;
    private static IStringLocalizer _localizer = _api.Localizer;

    public static void PrintWarns(CCSPlayerController? caller, Admin admin)
    {

        var str = "";
        foreach (var warn in admin.Warns)
        {
            string warnTemplate = _localizer["Message.WarnsTemplate"].AReplace(
                ["id", "reason", "admin", "created", "duration", "end"],
                [warn.Id, warn.Reason, AdminUtils.Admin(warn.AdminId)!.Name, 
                    Utils.GetDateString(warn.CreatedAt), 
                    $"{(warn.Duration == 0 ? _localizer["Other.Never"] : warn.Duration + _localizer["Other.Minutes"])}", 
                    Utils.GetDateString(warn.EndAt)]
            );
            str += warnTemplate;
        }
        caller.Print(_localizer["Message.Warns"].AReplace(["name", "warnsTemplate"], [admin.Name, str]));
    }

    public static string SWarnTemplate(Warn warn) 
    {
        string warnTemplate = _localizer["Message.WarnsTemplate"].AReplace(
                ["id", "reason", "admin", "created", "duration", "end"],
                [warn.Id, warn.Reason, AdminUtils.Admin(warn.AdminId)!.Name, 
                    Utils.GetDateString(warn.CreatedAt), 
                    $"{(warn.Duration == 0 ? _localizer["Other.Never"] : warn.Duration + _localizer["Other.Minutes"])}", 
                    Utils.GetDateString(warn.EndAt)]
            );
        return warnTemplate;
    }
}