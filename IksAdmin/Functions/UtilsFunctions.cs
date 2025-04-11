using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using IksAdminApi;

namespace IksAdmin.Functions;

public static class UtilsFunctions
{
    public static Admin? FindAdminByControllerMethod(CCSPlayerController player)
    {
        if (player.AuthorizedSteamID == null) return null;
        return Main.AdminApi.ServerAdmins.TryGetValue(player.AuthorizedSteamID.SteamId64, out var admin) ? admin : null;
    }
    public static Admin? FindAdminByIdMethod(int id)
    {
        if (id == 1)
        {
            return Main.AdminApi.ConsoleAdmin;
        }
        return Main.AdminApi.AllAdmins.FirstOrDefault(x => x.Id == id);
    }

    public static Dictionary<string, Dictionary<string, string>> GetPermissions()
    {
        return Main.AdminApi.RegistredPermissions;
    }

    public static IksAdminApi.CoreConfig GetConfigMethod()
    {
        return Main.AdminApi.Config;
    }

    public static void SetDebugMethod(string message)
    {
        AdminUtils.LogDebug(message);
    }

    public static string GetCurrentFlagsFunc(Admin admin)
    {
        if (admin.GroupId == null)
            return admin.Flags ?? "";
        var group = Main.AdminApi.Groups.FirstOrDefault(x => x.Id == admin.GroupId);
        if (group == null) {
            return admin.Flags ?? "";
        } else return group.Flags;
    }
    public static int GetCurrentImmunityFunc(Admin admin)
    {
        if (admin.GroupId == null)
            return admin.Immunity ?? 0;
        var group = Main.AdminApi.Groups.FirstOrDefault(x => x.Id == admin.GroupId);
        if (group == null) {
            return admin.Immunity ?? 0;
        } else return group.Immunity;
    }

    public static Group? GetGroupFromIdFunc(int id)
    {
        return Main.AdminApi.Groups.FirstOrDefault(x => x.Id == id);
    }

    
}