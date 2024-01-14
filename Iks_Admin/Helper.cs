using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.VisualBasic;
using MySqlConnector;
using Serilog.Sinks.File;
using cssAdminManager = CounterStrikeSharp.API.Modules.Admin;

namespace Iks_Admin;
class Helper{
    public static bool CanExecute(string? sid, string targetSid, string flag, List<Admin> admins)
    {
        if(sid == null) return true;
        if (sid != null) // Проверка на админа и флаги и иммунитет
        {
            Admin? admin = GetAdminBySid(sid, admins);
            Admin? targetAdmin = null;
            targetAdmin = GetAdminBySid(targetSid, admins); // Попытка получить админа по стим айди игрока
            
            if (admin != null)
            {
                if (!admin.Flags.Contains(flag) && !admin.Flags.Contains("z")) // Проверка админ флага
                { 
                    return false;
                }
                if (targetAdmin != null) // Если цель админ
                {
                    if (targetAdmin.Immunity >= admin.Immunity) //Проверка иммунитета цели
                    {
                        return false;
                    }
                }
            } else // Если игрок не админ: HaveNotAccess
            {
                return false;
            }
        }
        return true;
    }

    public static bool AdminHaveFlag(string? sid, string flag, List<Admin> admins)
    {
        Admin? admin = GetAdminBySid(sid, admins);
        if (admin == null) return false;
        if (!admin.Flags.Contains(flag) && !admin.Flags.Contains("z"))
        {
            return false;
        }
        return true;
    }
    public static Admin? GetAdminBySid(string sid,  List<Admin> admins)
    {
        foreach (var admin in admins)
        {
            if (admin.SteamId == sid)
            {
                return admin;
            }
        }
        return null;
    }

}