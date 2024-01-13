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

namespace Iks_Admin;

// Создание БД админов
// CREATE TABLE `u2194959_FallowCS2`.`iks_admins` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `name` VARCHAR(32) NOT NULL , `flags` VARCHAR(32) NOT NULL , `immunity` INT NOT NULL , PRIMARY KEY (`id`), UNIQUE (`sid`), UNIQUE (`name`)) ENGINE = InnoDB;


public class Iks_Admin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName { get; } = "Iks_Admin";
    public override string ModuleVersion { get; } = "1.0.0";
    public override string ModuleAuthor { get; } = "iks";
    private string _dbConnectionString = "";
    
    
    public CCSPlayerController?[] OwnReasonsTyper = new CCSPlayerController[64];
    public CCSPlayerController?[] SkipActionPlayer = new CCSPlayerController[64];
    public string?[] Actions = new String[64];
    public CCSPlayerController?[] ActionTargets = new CCSPlayerController[64];
    public DisconnectedPlayer?[] ActionTargetsDisconnected = new DisconnectedPlayer[64];
    public int[] ActionTimes = new Int32[64];
    

    private List<string> GaggedSids = new List<string>();
    private List<string> MutedSids = new List<string>();

    private List<DisconnectedPlayer> DisconnectedPlayers = new List<DisconnectedPlayer>();
    

    private List<Admin> admins = new List<Admin>();

    private VkLog? vkLog = null;

    public PluginConfig Config { get; set; }

    public void OnConfigParsed(PluginConfig config)
    {
        config = ConfigManager.Load<PluginConfig>(ModuleName);
        
        _dbConnectionString = "Server=" + config.Host + ";Database=" + config.Name
                              + ";port=" + config.Port + ";User Id=" + config.Login + ";password=" + config.Password;
        
        string sql =
            "CREATE TABLE IF NOT EXISTS `iks_admins` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `name` VARCHAR(32) NOT NULL , `flags` VARCHAR(32) NOT NULL , `immunity` INT NOT NULL, `group_id` INT NOT NULL DEFAULT '-1' ,`end` INT NOT NULL , `server_id` VARCHAR(64) NOT NULL , PRIMARY KEY (`id`), UNIQUE (`sid`), UNIQUE (`name`)) ENGINE = InnoDB;";
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                var comm = new MySqlCommand(sql, connection);
                comm.ExecuteNonQuery();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        sql = "CREATE TABLE IF NOT EXISTS `iks_bans` ( `id` INT NOT NULL AUTO_INCREMENT , `name` VARCHAR(32) NOT NULL ,`sid` VARCHAR(32) NOT NULL, `ip` VARCHAR(32) NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL, `BanType` INT(1) NOT NULL DEFAULT '0', `Unbanned` INT(1) NOT NULL DEFAULT '0', `UnbannedBy` VARCHAR(32) NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB;";
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                var comm = new MySqlCommand(sql, connection);
                comm.ExecuteNonQuery();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        
        sql = "CREATE TABLE IF NOT EXISTS `iks_mutes` ( `id` INT NOT NULL AUTO_INCREMENT , `name` VARCHAR(32) NOT NULL , `sid` VARCHAR(32) NOT NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL, `Unbanned` INT(1) NOT NULL DEFAULT '0', `UnbannedBy` VARCHAR(32) NULL, PRIMARY KEY (`id`)) ENGINE = InnoDB;";
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                var comm = new MySqlCommand(sql, connection);
                comm.ExecuteNonQuery();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        
        sql = "CREATE TABLE IF NOT EXISTS `iks_gags` ( `id` INT NOT NULL AUTO_INCREMENT , `name` VARCHAR(32) NOT NULL , `sid` VARCHAR(32) NOT NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL, `Unbanned` INT(1) NOT NULL DEFAULT '0', `UnbannedBy` VARCHAR(32) NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB;";
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                var comm = new MySqlCommand(sql, connection);
                comm.ExecuteNonQuery();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        sql = "CREATE TABLE IF NOT EXISTS `iks_groups` ( `id` INT NOT NULL AUTO_INCREMENT , `flags` VARCHAR(32) NOT NULL , `name` VARCHAR(32) NOT NULL , `immunity` INT NOT NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB;";
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                var comm = new MySqlCommand(sql, connection);
                comm.ExecuteNonQuery();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        if (config.LogToVk)
        {
            vkLog = new VkLog(config.Token, config.ChatId, config);
        }
        Config = config;

        ReloadAdmins(null);
        
    }
    
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [ConsoleCommand("css_reload_admins_cfg")]
    public void OnReloadCfgCommand(CCSPlayerController? controller, CommandInfo info)
    {
        OnConfigParsed(Config);
    }
    public override void Load(bool hotReload)
    {
        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSay);
        AddTimer(3, () =>
        {
            List<string> sids = GetListSids();
            Task.Run(async () =>
            {
                await SetMutedPlayers(sids);
                await SetGaggedPlayers(sids);
            });
        }, TimerFlags.REPEAT);
    }

    // COMMANDS
    [ConsoleCommand("css_reload_admins")]
    public void OnReloadAdminsCommand(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
        { 
            ReloadAdmins(null); 
            return;
        }

        Admin? admin = GetAdminBySid(controller.SteamID.ToString());

        if (admin == null || !admin.Flags.Contains("z"))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
            return;
        }
        
        OnConfigParsed(Config);
        foreach (var str in Localizer["reload_admins"].ToString().Split("\n"))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {str}");
        }
        ReloadAdmins(controller.SteamID.ToString()); 
        
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [ConsoleCommand("css_admin")]
    public void OnAdminCommand(CCSPlayerController controller, CommandInfo info)
    {
        Admin? admin = GetAdminBySid(controller.SteamID.ToString());
        if (admin == null)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
            return;
        }

        ChatMenu menu = AdminMenuConstructor(admin);

        ChatMenus.OpenMenu(controller, menu);
    }

    #region Admin Console Commands

    [ConsoleCommand("css_adminadd", "css_adminadd <sid> <name> <flags/-> <immunity> <group_id> <time>")]
    public void OnAdminAddCommand(CCSPlayerController? controller, CommandInfo info)
    {
        var args = GetArgsFromCommandLine(info.GetCommandString);

        string sid = args[1];
        string name = args[2];
        string flags = args[3];
        string server_id = Config.ServerId;
        if (flags == "-")
        {
            flags = "";
        }
        int immunity = Int32.Parse(args[4]);
        int group_id = -1;
        long end = 0;
        if (args.Count > 4) group_id = Int32.Parse(args[5]);
        if (args.Count > 5) {
            if (Int32.Parse(args[6]) > 0)
            {
                end = DateTimeOffset.UtcNow.ToUnixTimeSeconds()+(Int32.Parse(args[6])*60);
            }
        }
        if (args.Count > 6) {
            server_id = args[7];
        }

        if (controller != null)
        {
            Admin? admin = GetAdminBySid(controller.SteamID.ToString());
            if (admin == null)
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            if (!admin.Flags.Contains("z"))
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
        }
        foreach (var a in admins)
        {
            if (a.SteamId == sid)
            {
                if (controller != null)
                {
                    controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["AdminAlredyExist"]}");
                }
                Console.WriteLine("[Iks_Admin] Admin with sid alredy exists!");
                return;
            }
        }
        AdminManager am = new AdminManager(_dbConnectionString);
        Task.Run(async () => {
            await am.AddAdmin(sid, name, flags, immunity, group_id, end, server_id);
            Server.NextFrame(() => {
                ReloadAdmins(null);
            });
        });

        if (controller != null)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["AdminCreated"]}");
        }
        Console.WriteLine("[Iks_Admin] Admin created!");
    }

    [ConsoleCommand("css_admindel", "css_admindel <sid>")]
    public void OnAdminDelCommand(CCSPlayerController? controller, CommandInfo info)
    {
        var args = GetArgsFromCommandLine(info.GetCommandString);

        string sid = args[1];
        if (controller != null)
        {
            Admin? admin = GetAdminBySid(controller.SteamID.ToString());
            if (admin == null)
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            if (!admin.Flags.Contains("z"))
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
        }
        bool aExist = false;
        foreach (var a in admins)
        {
            if (a.SteamId == sid)
            {
                aExist = true;
            }
        }

        if (!aExist)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["AdminNotExist"]}");
            return;
        }

        AdminManager am = new AdminManager(_dbConnectionString);
        Task.Run(async () => {
            await am.DeleteAdmin(sid);
            Server.NextFrame(() => {
                ReloadAdmins(null);
            });
        });

        if (controller != null)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["AdminDeleted"]}");
        }
        Console.WriteLine("[Iks_Admin] Admin deleted!");
    }

    [ConsoleCommand("css_kick", "css_kick uid/sid")]
    public void OnKickCommand(CCSPlayerController? controller, CommandInfo info)
    {
        List<string> args = GetArgsFromCommandLine(info.GetCommandString);
        string reason = args[2];
        string AdminName = "Console";
        Admin? admin = null;
        CCSPlayerController? target = GetPlayerFromSidOrUid(info.GetArg(1));
        if (target == null && controller == null)
        {
            controller.PrintToChat($"[Iks_Admin] Player not finded!");
            return;
        }
        if (controller != null)
        {
            admin = GetAdminBySid(controller.SteamID.ToString());
            if (admin == null)
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            if (!admin.Flags.Contains("z") && !admin.Flags.Contains("k"))
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            if (target == null)
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFinded"]}");
                return;
            }
            Admin? targetAdmin = GetAdminBySid(target.SteamID.ToString());
            if (targetAdmin != null && targetAdmin.Immunity >= admin.Immunity)
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IfTargetImmunity"]}");
                return;
            }
            AdminName = controller.PlayerName;
        }

        foreach (var str in Localizer["KickMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", target.PlayerName)
                .Replace("{admin}", AdminName)
                .Replace("{reason}", reason)
            }");
        }
        string name = target.PlayerName;
        string sid = target.SteamID.ToString();
        Task.Run(async () => {
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["KickMessage"], name, sid, "Undefined", AdminName, reason, 0);
            }
        });
        
        NativeAPI.IssueServerCommand($"kickid {target.UserId}");
        


    }

    [ConsoleCommand("css_ban", "css_ban uid/sid duration reason <name if needed>")]
    public void OnBanCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);

        List<string> args = GetArgsFromCommandLine(info.GetCommandString);


        string identity = info.GetArg(1);
        int time = Int32.Parse(info.GetArg(2));
        string reason = args[3];
        string name = "Undefined";
        string ip = "Undefined";  
        string sid = identity.Length >= 17 ? identity : "Undefined";

        // Установка Name
        if (args.Count > 4)
        {
            if(args[4].Trim() != "")
            {
                name = args[4];
            }
        }


        CCSPlayerController? target = GetPlayerFromSidOrUid(identity); // Проверка есть ли игрок которого банят на сервере
        // Установки если игрок на сервере
        if (target != null) 
        {
            if (name == "Undefined")
            {
                name = target.PlayerName;
            }
            ip = target.IpAddress ?? "Undefined";
            sid = target.SteamID.ToString();
        }

        if (!isSteamId(sid))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IncorrectSid"]}");
            return;
        }

        string AdminSid = "CONSOLE";
        string AdminName = "CONSOLE";

        if (controller != null)
        {
            AdminSid = controller.SteamID.ToString();
            AdminName = controller.PlayerName;
        }
        
        if (controller != null) // Проверка на админа и флаги и иммунитет
        {
            Admin? admin = GetAdminBySid(controller.SteamID.ToString());
            Admin? targetAdmin = null;
            targetAdmin = GetAdminBySid(sid); // Попытка получить админа по стим айди игрока
            
            if (admin != null)
            {
                if (!admin.Flags.Contains("b") && !admin.Flags.Contains("z")) // Проверка админ флага
                { 
                    controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                    return;
                }
                if (targetAdmin != null) // Если цель админ
                {
                    if (targetAdmin.Immunity >= admin.Immunity) //Проверка иммунитета цели
                    {
                        controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IfTargetImmunity"].ToString().Replace("{name}", name)}");
                        return;
                    }
                }
            } else // Если игрок не админ: HaveNotAccess
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            
        }

        Task.Run(async () =>
        {
            if (await bm.IsPlayerBannedAsync(identity)) // Проверка есть ли бан по identity
            {
                Server.NextFrame(() => {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }
            if (await bm.IsPlayerBannedAsync(ip)) // Проверка есть ли бан по ip
            {
                Server.NextFrame(() => {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }

            //Если всё нормально то баним
            await bm.BanPlayer(name, sid, ip, AdminSid, time, reason);  
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, time);
            }
            Server.NextFrame(() => {
                PrintBanMessage(name, AdminName, time, reason);
            });
        });

        

        // Кикаем игрока после бана
        if (target != null)
            NativeAPI.IssueServerCommand($"kickid {target.UserId}");
    }

    [ConsoleCommand("css_banip", "css_banip uid/sid/ip(if offline) duration reason <name if needed>")]
    public void OnBanIpCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);

        string[] args = info.GetCommandString.Split(" ");

        string identity = info.GetArg(1);
        int time = Int32.Parse(info.GetArg(2));
        string reason = args[3];
        string name = "Undefined";
        string ip = "Undefined";  
        string sid = identity.Length >= 17 ? identity : "Undefined";

        // Установка Name
        if (args.Length > 4)
        {
            if(args[4].Trim() != "")
            {
                name = args[4];
            }
        }


        CCSPlayerController? target = GetPlayerFromSidOrUid(identity); // Проверка есть ли игрок которого банят на сервере

        if(target == null)
        {
            ip = identity;
        }

        
        // Установки если игрок на сервере
        if (target != null) 
        {
            if (name == "Undefined")
            {
                name = target.PlayerName;
            }
            ip = target.IpAddress ?? "Undefined";
            sid = target.SteamID.ToString();
        }

        if(ip == "Undefined")
        {
            info.ReplyToCommand("[IKS_Admin] Incorrect Ip Address");
        }

        if (!isSteamId(sid))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IncorrectSid"]}");
            return;
        }

        string AdminSid = "CONSOLE";
        string AdminName = "CONSOLE";

        if (controller != null)
        {
            AdminSid = controller.SteamID.ToString();
            AdminName = controller.PlayerName;
        }
        
        if (controller != null) // Проверка на админа и флаги и иммунитет
        {
            Admin? admin = GetAdminBySid(controller.SteamID.ToString());
            Admin? targetAdmin = null;
            targetAdmin = GetAdminBySid(sid); // Попытка получить админа по стим айди игрока
            
            if (admin != null)
            {
                if (!admin.Flags.Contains("b") && !admin.Flags.Contains("z")) // Проверка админ флага
                { 
                    controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                    return;
                }
                if (targetAdmin != null) // Если цель админ
                {
                    if (targetAdmin.Immunity >= admin.Immunity) //Проверка иммунитета цели
                    {
                        controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IfTargetImmunity"].ToString().Replace("{name}", name)}");
                        return;
                    }
                }
            } else // Если игрок не админ: HaveNotAccess
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            
        }

        Task.Run(async () =>
        {
            if (await bm.IsPlayerBannedAsync(identity)) // Проверка есть ли бан по identity
            {
                Server.NextFrame(() => {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }
            if (await bm.IsPlayerBannedAsync(ip)) // Проверка есть ли бан по ip
            {
                Server.NextFrame(() => {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }

            //Если всё нормально то баним ПО АЙПИ
            await bm.BanPlayerIp(name, sid, ip, AdminSid, time, reason);
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, time);
            }
            Server.NextFrame(() => {
                PrintBanMessage(name, AdminName, time, reason);
            });
        });

        // Кикаем игрока после бана
        if (target != null)
            NativeAPI.IssueServerCommand($"kickid {target.UserId}");
    }



    [ConsoleCommand("css_unban", "css_unban sid/ip")]
    public void OnUnBanCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        string arg = info.GetArg(1);
        string adminSid = "Console";
        string adminName = "Console";

        Admin? admin = null; // Тут мы получаем админа если команда от игрока

        if (controller != null)
        {
            admin = GetAdminBySid(controller.SteamID.ToString());
            if (admin == null)
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            if (!admin.Flags.Contains("z") && !admin.Flags.Contains("u"))
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            adminSid = controller.SteamID.ToString();
            adminName = controller.PlayerName;
        }
        BannedPlayer? bannedPlayer = null;
        Task.Run(async () => {
            bannedPlayer = await bm.GetPlayerBan(arg);
            if (bannedPlayer != null)
            {
                Server.NextFrame(() => {
                    PrintUnbanMessage(bannedPlayer.Name, adminName);
                });
            }
            await bm.UnBanPlayer(arg, adminSid);
            if (Config.LogToVk)
            {
                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnBanMessage"], bannedPlayer.Name, arg, adminName);
            }
        });


    }
   
   
    public void PrintUnbanMessage(string playerName, string adminName)
    {
        foreach (var str in Localizer["UnBanMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", playerName)
                .Replace("{admin}", adminName)
            }");
        }
    }
    public void PrintBanMessage(string name, string aName, int duration, string reason)
    {
        string title = $" {duration}{Localizer["min"]}";
        if (duration == 0)
        {
            title = $" {Localizer["Options.Infinity"]}";
        }
        foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", name)
                .Replace("{admin}", aName)
                .Replace("{reason}", reason)
                .Replace("{duration}", title)
            }");
        }
    }



    // GAG
    [ConsoleCommand("css_gag", "css_gag uid/sid duration reason <name if needed>")]
    public void OnGagCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);

        List<string> args = GetArgsFromCommandLine(info.GetCommandString);

        string identity = info.GetArg(1);
        int time = Int32.Parse(info.GetArg(2));
        string reason = args[3];
        string name = "Undefined";
        string sid = identity.Length >= 17 ? identity : "Undefined";

        // Установка Name
        if (args.Count > 4)
        {
            if(args[4].Trim() != "")
            {
                name = args[4];
            }
        }


        CCSPlayerController? target = GetPlayerFromSidOrUid(identity); // Проверка есть ли игрок которого банят на сервере
        // Установки если игрок на сервере
        if (target != null) 
        {
            if (name == "Undefined")
            {
                name = target.PlayerName;
            }
            sid = target.SteamID.ToString();
        }

        if (!isSteamId(sid))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IncorrectSid"]}");
            return;
        }

        string AdminSid = "CONSOLE";
        string AdminName = "CONSOLE";

        if (controller != null)
        {
            AdminSid = controller.SteamID.ToString();
            AdminName = controller.PlayerName;
        }
        
        if (controller != null) // Проверка на админа и флаги и иммунитет
        {
            Admin? admin = GetAdminBySid(controller.SteamID.ToString());
            Admin? targetAdmin = null;
            targetAdmin = GetAdminBySid(sid); // Попытка получить админа по стим айди игрока
            
            if (admin != null)
            {
                if (!admin.Flags.Contains("g") && !admin.Flags.Contains("z")) // Проверка админ флага
                { 
                    controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                    return;
                }
                if (targetAdmin != null) // Если цель админ
                {
                    if (targetAdmin.Immunity >= admin.Immunity) //Проверка иммунитета цели
                    {
                        controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IfTargetImmunity"].ToString().Replace("{name}", name)}");
                        return;
                    }
                }
            } else // Если игрок не админ: HaveNotAccess
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            
        }
        List<string> sids = GetListSids();
        Task.Run(async () =>
        {
            if (await bm.IsPlayerGaggedAsync(sid)) // Проверка есть ли бан по identity
            {
                Server.NextFrame(() => {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }

            //Если всё нормально то баним
            await bm.GagPlayer(name, sid, AdminSid, time, reason);
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["GagMessage"], name, sid, "Undefined", AdminName, reason, time);
            }
            await SetGaggedPlayers(sids);

            Server.NextFrame(() => {
                UpdateChatColorsGagged();
                PrintGagMessage(name, AdminName, time, reason);
            });
        });

    }

    [ConsoleCommand("css_ungag", "css_ungag sid/uid")]
    public void OnUnGagCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        string arg = info.GetArg(1);
        string adminSid = "Console";
        string adminName = "Console";

        CCSPlayerController? target = GetPlayerFromSidOrUid(arg);

        if (target != null)
        {
            arg = target.SteamID.ToString();
        }


        Admin? admin = null; // Тут мы получаем админа если команда от игрока
        if (controller != null)
        {
            admin = GetAdminBySid(controller.SteamID.ToString());
            if (admin == null)
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            if (!admin.Flags.Contains("z") && !admin.Flags.Contains("g"))
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            adminSid = controller.SteamID.ToString();
            adminName = controller.PlayerName;
        }
        List<string> sids = GetListSids();
        BannedPlayer? bannedPlayer = null;
        Task.Run(async () => {
            bannedPlayer = await bm.GetPlayerGag(arg);
            Console.WriteLine("[Iks_Admin] player ungaged");
            await bm.UnGagPlayer(arg, adminSid);
            if (bannedPlayer != null)
            {
                Server.NextFrame(() => {
                    PrintUnGagMessage(bannedPlayer.Name, adminName);
                });
            }
            if (Config.LogToVk)
            {
                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnGagMessage"], bannedPlayer.Name, arg, adminName);
            }
            await SetGaggedPlayers(sids);
        });


    }
   
   
    public void PrintUnGagMessage(string playerName, string adminName)
    {
        foreach (var str in Localizer["UnGagMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", playerName)
                .Replace("{admin}", adminName)
            }");
        }
    }
    public void PrintGagMessage(string name, string aName, int duration, string reason)
    {
        string title = $" {duration}{Localizer["min"]}";
        if (duration == 0)
        {
            title = $" {Localizer["Options.Infinity"]}";
        }
        foreach (var str in Localizer["GagMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", name)
                .Replace("{admin}", aName)
                .Replace("{reason}", reason)
                .Replace("{duration}", title)
            }");
        }
    }

    // MUTE
    [ConsoleCommand("css_mute", "css_mute uid/sid duration reason <name if needed>")]
    public void OnMuteCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);

        //Fix args Need to do in other commands
        List<string> args = GetArgsFromCommandLine(info.GetCommandString);

        string identity = info.GetArg(1);
        int time = Int32.Parse(info.GetArg(2));
        string reason = args[3];
        string name = "Undefined";
        string sid = identity.Length >= 17 ? identity : "Undefined";

        // Установка Name        
        if (args.Count > 4)
        {
            if(args[4].Trim() != "")
            {
                name = args[4];
            }
        }


        CCSPlayerController? target = GetPlayerFromSidOrUid(identity); // Проверка есть ли игрок которого банят на сервере
        // Установки если игрок на сервере
        if (target != null) 
        {
            if (name == "Undefined")
            {
                name = target.PlayerName;
            }
            sid = target.SteamID.ToString();
        }

        if (!isSteamId(sid))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IncorrectSid"]}");
            return;
        }

        string AdminSid = "CONSOLE";
        string AdminName = "CONSOLE";

        if (controller != null)
        {
            AdminSid = controller.SteamID.ToString();
            AdminName = controller.PlayerName;
        }
        
        if (controller != null) // Проверка на админа и флаги и иммунитет
        {
            Admin? admin = GetAdminBySid(controller.SteamID.ToString());
            Admin? targetAdmin = null;
            targetAdmin = GetAdminBySid(sid); // Попытка получить админа по стим айди игрока
            
            if (admin != null)
            {
                if (!admin.Flags.Contains("m") && !admin.Flags.Contains("z")) // Проверка админ флага
                { 
                    controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                    return;
                }
                if (targetAdmin != null) // Если цель админ
                {
                    if (targetAdmin.Immunity >= admin.Immunity) //Проверка иммунитета цели
                    {
                        controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["IfTargetImmunity"].ToString().Replace("{name}", name)}");
                        return;
                    }
                }
            } else // Если игрок не админ: HaveNotAccess
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            
        }
        List<string> sids = GetListSids();

        Task.Run(async () =>
        {
            if (await bm.IsPlayerMutedAsync(sid)) // Проверка есть ли бан по identity
            {
                Server.NextFrame(() => {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }

            //Если всё нормально то баним
            await bm.MutePlayer(name, sid, AdminSid, time, reason);
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["MuteMessage"], name, sid, "Undefined", AdminName, reason, time);
            }
            await SetMutedPlayers(sids);
            Server.NextFrame(() => {
                PrintMuteMessage(name, AdminName, time, reason);
            });
        });

    }

    [ConsoleCommand("css_unmute", "css_unmute sid/uid")]
    public void OnUnMuteCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        string arg = info.GetArg(1);
        string adminSid = "Console";
        string adminName = "Console";

        CCSPlayerController? target = GetPlayerFromSidOrUid(arg);

        if (target != null)
        {
            arg = target.SteamID.ToString();
        }


        Admin? admin = null; // Тут мы получаем админа если команда от игрока

        if (controller != null)
        {
            admin = GetAdminBySid(controller.SteamID.ToString());
            if (admin == null)
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            if (!admin.Flags.Contains("z") && !admin.Flags.Contains("g"))
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            adminSid = controller.SteamID.ToString();
            adminName = controller.PlayerName;
        }
        List<string> sids = GetListSids();
        BannedPlayer? bannedPlayer = null;
        Task.Run(async () => {
            bannedPlayer = await bm.GetPlayerMute(arg);
            if (bannedPlayer != null)
            {
                Server.NextFrame(() => {
                    PrintUnMuteMessage(bannedPlayer.Name, adminName);
                });
            }
            Console.WriteLine("[Iks_Admin] player unmuted");
            await bm.UnMutePlayer(arg, adminSid);
            if (Config.LogToVk)
            {
                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnMuteMessage"], bannedPlayer.Name, arg, adminName);
            }
            await SetMutedPlayers(sids);
        });


    }
   
   
    public void PrintUnMuteMessage(string playerName, string adminName)
    {
        foreach (var str in Localizer["UnMuteMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", playerName)
                .Replace("{admin}", adminName)
            }");
        }
    }
    public void PrintMuteMessage(string name, string aName, int duration, string reason)
    {
        string title = $" {duration}{Localizer["min"]}";
        if (duration == 0)
        {
            title = $" {Localizer["Options.Infinity"]}";
        }
        foreach (var str in Localizer["MuteMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", name)
                .Replace("{admin}", aName)
                .Replace("{reason}", reason)
                .Replace("{duration}", title)
            }");
        }
    }





    public void NotifyIfPlayerAlredyBanned(string name, string aSid)
    {
        if (aSid == "CONSOLE")
        {
            Console.WriteLine($"[Iks_Admin] Player: {name} Alredy banned!");
            return;
        }
        CCSPlayerController? controller = GetPlayerFromSidOrUid(aSid);
        if (controller != null)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
        }
    }

    
    public CCSPlayerController? GetPlayerFromSidOrUid(string arg)
    {
        CCSPlayerController? player = null;

        int uid;
        if (Int32.TryParse(arg, out uid))
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (!p.IsBot && p.IsValid)
                {
                    if (p.UserId == uid)
                    {
                        return p;
                    }
                }
            }
        }

        ulong sid;
        if (UInt64.TryParse(arg, out sid))
        {
            if (Utilities.GetPlayerFromSteamId(sid) != null)
            {
                player = Utilities.GetPlayerFromSteamId(sid);
                return player;
            }
        }
        
        return null;
    }

    public bool isSteamId(string arg)
    {
        if (arg.Length >= 17)
        {
            return true;
        }

        return false;
    }

    
    

    [ConsoleCommand("css_searchbans", "!searchbans sid")]
    public void OnSearchBansCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        string? cSid = controller == null ? null : controller.SteamID.ToString();
        string sid = info.GetArg(1);
        List<BannedPlayer> playerBans = new List<BannedPlayer>();
        Task.Run(async () =>
        {
            playerBans = await bm.GetPlayerBansBySid(sid);
            
            Server.NextFrame(() =>
            {
                WritePlayerBans(cSid, info, playerBans);
            });
        });
    }
    public void WritePlayerBans(string? cSid, CommandInfo info, List<BannedPlayer> playerBans)
        {
            if (cSid == null)
            {
                info.ReplyToCommand("[Iks_Admin] Player bans:");
            }
            CCSPlayerController? controller = null;
            if (cSid != null)
            {
                controller = Utilities.GetPlayerFromSteamId(UInt64.Parse(cSid));
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["searchbansTitle"]}");

            }
            foreach (var p in playerBans)
            {

                DateTime utcDateTime = UnixTimeStampToDateTime(p.BanCreated);
                string CreatedTimeString = utcDateTime.ToString("dd/MM/yy HH:mm:ss");
                    
                utcDateTime = UnixTimeStampToDateTime(p.BanTimeEnd);
                string EndTimeString = utcDateTime.ToString("dd/MM/yy HH:mm:ss");
                
                Admin? admin = GetAdminBySid(p.AdminSid);
                string AdminName = admin == null ? p.AdminSid : admin.Name;
                    
                string UbanAdminName = "";
                if (p.Unbanned)
                {
                    Admin? UnbannedAdmin = GetAdminBySid(p.UnbannedBy); 
                    UbanAdminName = UnbannedAdmin == null ? p.UnbannedBy : UnbannedAdmin.Name;
                }
                if (controller == null)
                {
                    info.ReplyToCommand("[Iks_Admin] ====================");
                    info.ReplyToCommand($"[Iks_Admin] Player name: {p.Name}");
                    info.ReplyToCommand($"[Iks_Admin] Player ip: {p.Ip}");
                    info.ReplyToCommand($"[Iks_Admin] Admin: {AdminName}");
                    info.ReplyToCommand($"[Iks_Admin] Ban reason: {p.BanReason}");
                    info.ReplyToCommand($"[Iks_Admin] Ban Time: {p.BanTime}sec.");
                    info.ReplyToCommand($"[Iks_Admin] Ban Created: {CreatedTimeString}");
                    info.ReplyToCommand($"[Iks_Admin] Ban End: {EndTimeString}");
                    info.ReplyToCommand($"[Iks_Admin] Unbanned: {p.Unbanned}");
                    info.ReplyToCommand($"[Iks_Admin] UnbannedBy: {UbanAdminName}");
                    info.ReplyToCommand("[Iks_Admin] ====================");
                }

                if (controller != null)
                {
                    foreach (var str in Localizer["css_searchbans"].ToString().Split("\n"))
                    {
                        controller.PrintToChat($" {Localizer["PluginTag"]} {str
                            .Replace("{name}", p.Name)
                            .Replace("{ip}", p.Ip)
                            .Replace("{admin}", AdminName)
                            .Replace("{reason}", p.BanReason)
                            .Replace("{time}", p.BanTime.ToString())
                            .Replace("{created}", CreatedTimeString)
                            .Replace("{end}", EndTimeString)
                            .Replace("{unbanned}", p.Unbanned.ToString())
                            .Replace("{unbannedBy}", UbanAdminName)
                        }");
                    }
                }
                
            }
            info.ReplyToCommand("end?");

            
        }
    #endregion
    
    
    
    #region FUNC

    public List<string> GetArgsFromCommandLine(string commandLine)
    {
        List<string> args = new List<string>();
        var regex = new Regex(@"(""((\\"")|([^""]))*"")|('((\\')|([^']))*')|(\S+)");
        var matches = regex.Matches(commandLine);
        foreach (Match match in matches) {
            args.Add(match.Value);
        }
        return args;
    }
    public void UpdateChatColorsGagged()
    {
        if (Config.HaveIksChatColors)
        {
            NativeAPI.IssueServerCommand("css_chatcolors_setgaggedplayers");
        }
    }
    public async Task SetMutedPlayers(List<string> sids) // IN FUTURE
    {
        BanManager bm = new BanManager(_dbConnectionString);
        
        List<string> muted = new List<string>();
        foreach (var sid in sids)
        {
            if (await bm.IsPlayerMutedAsync(sid))
            {
                muted.Add(sid);
            }
        }

        MutedSids = muted;
        Server.NextFrame(() =>
        {
            SetMuteForPlayers();
        });

    }
    
    public void SetMuteForPlayers()
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsBot && p.IsValid)
            {
                if (MutedSids.Contains(p.SteamID.ToString()))
                {
                    p.VoiceFlags = VoiceFlags.Muted;
                }
                else
                {
                    p.VoiceFlags = VoiceFlags.Normal;
                }
            }
        }
    }

    public List<string> GetListSids()
    {
        List<string> sids = new List<string>();
        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsBot && p.IsValid)
            {
                sids.Add(p.SteamID.ToString());
            }
        }

        return sids;
    }
    public async Task SetGaggedPlayers(List<string> sids)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        
        List<string> muted = new List<string>();
        foreach (var sid in sids)
        {
            if (await bm.IsPlayerGaggedAsync(sid))
            {
                muted.Add(sid);
            }
        }

        GaggedSids = muted;
    }
    public static DateTime UnixTimeStampToDateTime( double unixTimeStamp )
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
        return dateTime;
    }
    public void ReloadAdmins(string? sid)
    {
        Console.WriteLine("[Iks_Admin] Admins reloaded!");
        AdminManager am = new AdminManager(_dbConnectionString);
        string server_id = Config.ServerId;
        Task.Run(async () => {
            admins = await am.GetAllAdmins(server_id);
            Server.NextFrame(() => {
                PrintAdminList(sid);
            });
        });

        for (int i = 0; i < OwnReasonsTyper.Length; i++)
        {
            OwnReasonsTyper[i] = null;
            Actions[i] = null;
            ActionTargets[i] = null;
            ActionTargetsDisconnected[i] = null;
            SkipActionPlayer[i] = null;
            ActionTimes[i] = 0;
        }
    }
    public void PrintAdminList(string? sid)
    {
        CCSPlayerController? controller = null;
        if (sid != null)
        {
            controller = Utilities.GetPlayerFromSteamId(UInt64.Parse(sid));
        }
        foreach (var a in admins)
        {
            if (sid == null)
            {
                Console.WriteLine("=============");
                Console.WriteLine($"Name: {a.Name}");
                Console.WriteLine($"SteamId: {a.SteamId}");
                Console.WriteLine($"Immunity: {a.Immunity}");
                Console.WriteLine($"Flags: {a.Flags}");
                Console.WriteLine($"Group Name: {a.GroupName}");
                Console.WriteLine($"Group Id: {a.GroupId}");
                Console.WriteLine("=============");
            }
            if(controller != null)
            {
                controller.PrintToChat("=============");
                controller.PrintToChat($"Name: {ChatColors.DarkBlue}{a.Name}");
                controller.PrintToChat($"SteamId: {ChatColors.DarkBlue}{a.SteamId}");
                controller.PrintToChat($"Immunity: {ChatColors.DarkBlue}{a.Immunity}");
                controller.PrintToChat($"Flags: {ChatColors.DarkBlue}{a.Flags}");
                controller.PrintToChat($"Group Name: {ChatColors.DarkBlue}{a.GroupName}");
                controller.PrintToChat($"Group Id: {ChatColors.DarkBlue}{a.GroupId}");
                controller.PrintToChat("=============");
            }
        }
    }

    public Admin? GetAdminBySid(string sid)
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

    public HookResult OnSay(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
        {
            return HookResult.Continue;
        }
        
        if (GaggedSids.Contains(controller.SteamID.ToString()))
        {
            return HookResult.Stop;
        }
       

        for (int i = 0; i < SkipActionPlayer.Length; i++)
        {
            if (SkipActionPlayer[i] == controller)
            {
                SkipActionPlayer[i] = null;
                return HookResult.Continue;
            }
        }
        
        
        if (!info.GetArg(1).StartsWith("!") || info.GetArg(1).Trim() == "")
        {
            return HookResult.Continue;
        }

        for (int i = 0; i < OwnReasonsTyper.Length; i++)
        {
            if (OwnReasonsTyper[i] == controller)
            {
                switch (Actions[i])
                {
                    case "kick":
                        NativeAPI.IssueServerCommand($"kickid {ActionTargets[i].UserId}");
                        string name = ActionTargets[i].PlayerName;
                        string sid = ActionTargets[i].SteamID.ToString();
                        string adminName = OwnReasonsTyper[i].PlayerName;
                        string reason = info.GetArg(1).Replace("!", "");
                        Task.Run(async () => {
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["KickMessage"], name, sid, "Undefined", adminName, reason, 0);
                            }
                        });
                        foreach (var str in Localizer["KickMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargets[i].PlayerName)
                                .Replace("{admin}", controller.PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))
                            }");
                        }
                    break;
                    case "banTime":
                        int time = Int32.Parse(info.GetArg(1).Replace("!", ""));
                        string title = $"{time}{Localizer["min"]}";
                        if (time == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        
                        ChatMenu BanMenuReasons = BanMenuReasonsConstructor(OwnReasonsTyper[i], ActionTargets[i], time, title);
                        
                        ChatMenus.OpenMenu(OwnReasonsTyper[i], BanMenuReasons);
                    break;
                    case "banTimeDisconnected": // Для вышедших игроков
                        time = Int32.Parse(info.GetArg(1).Replace("!", ""));
                        title = $"{time}{Localizer["min"]}";
                        if (time == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        
                        BanMenuReasons = BanMenuDisconnectedPlayersReasonsConstructor(OwnReasonsTyper[i], ActionTargetsDisconnected[i], time, title);
                        
                        ChatMenus.OpenMenu(OwnReasonsTyper[i], BanMenuReasons);
                    break;
                    case "banReason":
                        title = $"{ ActionTimes[i]}{Localizer["min"]}";
                        if (ActionTimes[i] == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        BanManager bm = new BanManager(_dbConnectionString);
                        if (bm.IsPlayerBanned(ActionTargets[i].SteamID.ToString()))
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                            break;
                        }

                        name = ActionTargets[i].PlayerName;
                        sid = ActionTargets[i].SteamID.ToString();
                        string? ip = ActionTargets[i].IpAddress;
                        string adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        int Btime = ActionTimes[i];
                        reason = info.GetArg(1);
                        string AdminName = OwnReasonsTyper[i].PlayerName;
                        Task.Run(async () =>
                        {
                            await bm.BanPlayer(name, sid, ip, adminsid, Btime, reason);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, Btime);
                            }
                        });
                        NativeAPI.IssueServerCommand($"kickid {ActionTargets[i].UserId}");

                        foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargets[i].PlayerName)
                                .Replace("{admin}", OwnReasonsTyper[i].PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))
                                .Replace("{duration}", title)
                            }");
                        }

                        break;
                    case "banReasonDisconnected":
                        title = $"{ ActionTimes[i]}{Localizer["min"]}";
                        if (ActionTimes[i] == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        bm = new BanManager(_dbConnectionString);
                        if (bm.IsPlayerBanned(ActionTargetsDisconnected[i].Sid))
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                            break;
                        }

                        name = ActionTargetsDisconnected[i].Name;
                        sid = ActionTargetsDisconnected[i].Sid;
                        ip = ActionTargetsDisconnected[i].Ip;
                        adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        Btime = ActionTimes[i];
                        reason = info.GetArg(1).Replace("!", "");
                        AdminName = OwnReasonsTyper[i].PlayerName;
                
                        Task.Run(async () =>
                        {
                            await bm.BanPlayer(name, sid, ip, adminsid, Btime, reason);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, Btime);
                            }   
                        });

                        foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargetsDisconnected[i].Name)
                                .Replace("{admin}", OwnReasonsTyper[i].PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))
                                .Replace("{duration}", title)
                            }");
                        }

                        break;
                    case "muteTime":
                        time = Int32.Parse(info.GetArg(1).Replace("!", ""));
                        title = $"{time}{Localizer["min"]}";
                        if (time == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        
                        ChatMenu MuteMenuReasons = MuteMenuReasonsConstructor(OwnReasonsTyper[i], ActionTargets[i], time, title);
                        
                        ChatMenus.OpenMenu(OwnReasonsTyper[i], MuteMenuReasons);
                    break;
                    case "muteReason":
                        title = $"{ ActionTimes[i]}{Localizer["min"]}";
                        if (ActionTimes[i] == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        bm = new BanManager(_dbConnectionString);
                        if (MutedSids.Contains(ActionTargets[i].SteamID.ToString()))
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                            break;
                        }

                        name = ActionTargets[i].PlayerName;
                        sid = ActionTargets[i].SteamID.ToString();
                        adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        Btime = ActionTimes[i];
                        reason = info.GetArg(1).Replace("!", "");
                        AdminName =  OwnReasonsTyper[i].PlayerName;

                        
                
                        Task.Run(async () =>
                        {
                            await bm.MutePlayer(name, sid, adminsid, Btime, reason);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["MuteMessage"], name, sid, "Undefined", AdminName, reason, Btime);
                            }
                            List<string> sids = GetListSids();
                            Task.Run(async () =>
                            {
                                await SetMutedPlayers(sids);
                            });
                        });

                        foreach (var str in Localizer["MuteMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargets[i].PlayerName)
                                .Replace("{admin}", OwnReasonsTyper[i].PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))
                                .Replace("{duration}", title)
                            }");
                        }

                        break;
                    case "gagTime":
                        time = Int32.Parse(info.GetArg(1).Replace("!", ""));
                        title = $"{time}{Localizer["min"]}";
                        if (time == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        
                        ChatMenu GagMenuReasons = GagMenuReasonsConstructor(OwnReasonsTyper[i], ActionTargets[i], time, title);
                        
                        ChatMenus.OpenMenu(OwnReasonsTyper[i], GagMenuReasons);
                    break;
                    case "gagReason":
                        title = $"{ ActionTimes[i]}{Localizer["min"]}";
                        if (ActionTimes[i] == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        bm = new BanManager(_dbConnectionString);
                        if (GaggedSids.Contains(ActionTargets[i].SteamID.ToString()))
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                            break;
                        }

                        name = ActionTargets[i].PlayerName;
                        sid = ActionTargets[i].SteamID.ToString();
                        adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        Btime = ActionTimes[i];
                        reason = info.GetArg(1).Replace("!", "");
                        AdminName =  OwnReasonsTyper[i].PlayerName;
                        ip = ActionTargets[i].IpAddress;
                        Task.Run(async () =>
                        {
                            await bm.GagPlayer(name, sid, adminsid, Btime, reason);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["GagMessage"], name, sid, ip, AdminName, reason, Btime);
                            }
                        });
                        List<string> gagCheckSids = GetListSids(); 
                        Task.Run(async () =>
                        {
                            await SetGaggedPlayers(gagCheckSids);
                            Server.NextFrame(() => {
                                UpdateChatColorsGagged();
                            });
                        });
                        


                        foreach (var str in Localizer["GagMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargets[i].PlayerName)
                                .Replace("{admin}", OwnReasonsTyper[i].PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))
                                .Replace("{duration}", title)
                            }");
                        }

                        break;
                }
                OwnReasonsTyper[i] = null;
                Actions[i] = "";
                ActionTargets[i] = null;
                SkipActionPlayer[i] = null;
                ActionTargetsDisconnected[i] = null;
                ActionTimes[i] = 0;
                return HookResult.Stop;
            }
        }
        return HookResult.Continue;
    }

    #endregion
    
    #region EVENTS

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        if(@event.Userid.IsBot || !@event.Userid.IsValid) return HookResult.Continue;
        int? uid =  @event.Userid.UserId;
        string sid = @event.Userid.SteamID.ToString();
        string ip = @event.Userid.IpAddress;
        Task.Run(async () => {
            if (await bm.IsPlayerBannedAsync(sid))
            {
                Server.NextFrame(() => {
                    Kick(uid);
                });
            }
            if (await bm.IsPlayerBannedAsync(ip))
            {
                Server.NextFrame(() => {
                    Kick(uid);
                });
            }
        });
        CCSPlayerController controller = @event.Userid;

        // Установка вышедших игроков

        for (int i = 0; i < DisconnectedPlayers.Count; i++)
        {
            if (controller.SteamID.ToString() == DisconnectedPlayers[i].Sid)
            {
                DisconnectedPlayers.RemoveAt(i);
            }
        }

        
        return HookResult.Continue;
    }

    public void Kick(int? uid)
    {
        NativeAPI.IssueServerCommand($"kickid {uid}");
    }


    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        for (int i = 0; i < OwnReasonsTyper.Length; i++)
        {
            if (OwnReasonsTyper[i] == @event.Userid)
            {
                OwnReasonsTyper[i] = null;
                Actions[i] = "";
                ActionTargets[i] = null;
                ActionTimes[i] = 0;
                SkipActionPlayer[i] = null;
            }
        }

        // Установка вышедших игроков
        CCSPlayerController controller = @event.Userid;

        DisconnectedPlayer disconnectedPlayer = new DisconnectedPlayer(controller.PlayerName, controller.SteamID.ToString(), controller.IpAddress);

        DisconnectedPlayers.Add(disconnectedPlayer);


        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        List<string> gagCheckSids = GetListSids(); 
        Task.Run(async () =>
        {
            await SetGaggedPlayers(gagCheckSids);
        });
        Task.Run(async () =>
        {
            List<string> sids = GetListSids();
            await SetMutedPlayers(sids);
        });
        return HookResult.Continue;
    }

    #endregion

    

    #region MENUS
    public ChatMenu AdminMenuConstructor(Admin admin)
    {
        ChatMenu menu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["AdminMenuTitle"]}");

        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });

        // Кик
        if (admin.Flags.Contains("z") || admin.Flags.Contains("k"))
        {
            menu.AddMenuOption($" {Localizer["Options.Kick"]}", (controller, option) =>
            {
                ChatMenus.OpenMenu(controller, KickMenuConstructor(controller));
            });
        }
        // Бан
        if (admin.Flags.Contains("z") || admin.Flags.Contains("b"))
        {
            menu.AddMenuOption($" {Localizer["Options.Ban"]}", (controller, option) =>
            {
                ChatMenus.OpenMenu(controller, BanMenuConstructor(controller));
            });
            menu.AddMenuOption($" {Localizer["Options.BanDisconnected"]}", (controller, option) =>
            {
                ChatMenus.OpenMenu(controller, BanMenuDisconnectedPlayersConstructor(controller));
            });
        }
        // Мут
        if (admin.Flags.Contains("z") || admin.Flags.Contains("m"))
        {
            menu.AddMenuOption($" {Localizer["Options.Mute"]}", (controller, option) =>
            {
                ChatMenus.OpenMenu(controller, MuteMenuConstructor(controller));
            });
        }
        // Гаг
        if (admin.Flags.Contains("z") || admin.Flags.Contains("g"))
        {
            menu.AddMenuOption($" {Localizer["Options.Gag"]}", (controller, option) =>
            {
                ChatMenus.OpenMenu(controller, GagMenuConstructor(controller));
            });
        }
        // Снять наказание
        if (admin.Flags.Contains("z") || admin.Flags.Contains("m") || admin.Flags.Contains("g"))
        {
            menu.AddMenuOption($" {Localizer["Options.UnGagMute"]}", (controller, option) =>
            {
                ChatMenus.OpenMenu(controller, UnMuteGagMenuConstructor(controller));
            });
        }
        
        return menu;
    }
    public ChatMenu KickMenuConstructor(CCSPlayerController activator)
    {
        ChatMenu menu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["KickMenuTitle"]}");
        
        // Назад
        menu.AddMenuOption($" {Localizer["Options.Back"]}", (controller, option) =>
        {
            ChatMenus.OpenMenu(activator, AdminMenuConstructor(GetAdminBySid(activator.SteamID.ToString())));
        });
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid) continue;
            if (player == activator) continue;
            if (GetAdminBySid(player.SteamID.ToString()) != null)
            {
                if (GetAdminBySid(player.SteamID.ToString()).Immunity > GetAdminBySid(activator.SteamID.ToString()).Immunity)
                {
                    continue;
                }
            }
            
            
            menu.AddMenuOption($" {player.PlayerName}", (controller, option) =>
            {
                //KickReasons
                ChatMenu KickReasonsMenu =
                    new ChatMenu($" {Localizer["PluginTag"]} {Localizer["KickMenuReasonsTitle"]}");
                
                foreach (var reason in Config.KickReasons)
                {
                    KickReasonsMenu.AddMenuOption($"{reason}", (playerController, menuOption) =>
                    {
                        NativeAPI.IssueServerCommand($"kickid {player.UserId}");
                        string name = player.PlayerName;
                        string sid = player.SteamID.ToString();
                        string adminName = playerController.PlayerName;
                        Task.Run(async () => {
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["KickMessage"], name, sid, "Undefined", adminName, reason, 0);
                            }
                        });
                        foreach (var str in Localizer["KickMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", player.PlayerName)
                                .Replace("{admin}", controller.PlayerName)
                                .Replace("{reason}", reason)
                            }");
                        }
                    });
                }
                //Своя причина
                KickReasonsMenu.AddMenuOption($"{Localizer["Options.OwnReason"]}", (playerController, menuOption) =>
                {
                    playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnReasonMessage"]}"]}");
                    for (int i = 0; i < OwnReasonsTyper.Length; i++)
                    {
                        if (OwnReasonsTyper[i] == null)
                        {
                            OwnReasonsTyper[i] = playerController;
                            Actions[i] = "kick";
                            ActionTargets[i] = player;
                            return;
                        }
                    }
                });
                ChatMenus.OpenMenu(controller, KickReasonsMenu);
            });
        }
        
        return menu;
    }
    public ChatMenu BanMenuConstructor(CCSPlayerController activator)
    {
        ChatMenu menu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["BanMenuTitle"]}");
        
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Back"]}", (controller, option) =>
        {
            ChatMenus.OpenMenu(activator, AdminMenuConstructor(GetAdminBySid(activator.SteamID.ToString())));
        });
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid) continue;
            if (player == activator) continue;
            if (GetAdminBySid(player.SteamID.ToString()) != null)
            {
                if (GetAdminBySid(player.SteamID.ToString()).Immunity > GetAdminBySid(activator.SteamID.ToString()).Immunity)
                {
                    continue;
                }
            }
            
            menu.AddMenuOption($" {player.PlayerName}", (controller, option) =>
            {
                ChatMenu BanMenuTimes = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["TimesTitle"]}");

                foreach (var time in Config.Times)
                {
                    string title = $"{time}{Localizer["min"]}";
                    if (time == 0)
                    {
                        title = Localizer["Options.Infinity"].ToString();
                    }

                    BanMenuTimes.AddMenuOption(title, (ccsPlayerController, chatMenuOption) =>
                    {

                        ChatMenu BanMenuReasons = BanMenuReasonsConstructor(ccsPlayerController, player, time, title);
                        
                        ChatMenus.OpenMenu(ccsPlayerController, BanMenuReasons);
                       
                    });
                }
                

                BanMenuTimes.AddMenuOption($"{Localizer["Options.OwnTime"]}", (playerController, menuOption) =>
                {
                    playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnTimeMessage"]}"]}");
                    for (int i = 0; i < OwnReasonsTyper.Length; i++)
                    {
                        if (OwnReasonsTyper[i] == null)
                        {
                            OwnReasonsTyper[i] = playerController;
                            Actions[i] = "banTime";
                            ActionTargets[i] = player;
                            return;
                        }
                    }
                });
                ChatMenus.OpenMenu(controller, BanMenuTimes);
            });
        }
        
        return menu;
    }

    public ChatMenu BanMenuDisconnectedPlayersConstructor(CCSPlayerController activator)
    {
        ChatMenu menu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["BanMenuTitle"]}");
        
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Back"]}", (controller, option) =>
        {
            ChatMenus.OpenMenu(activator, AdminMenuConstructor(GetAdminBySid(activator.SteamID.ToString())));
        });
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });
        DisconnectedPlayers.Reverse();
        var DisconnectedPlayersReversed = DisconnectedPlayers;
        DisconnectedPlayers.Reverse();
        foreach (var player in DisconnectedPlayersReversed)
        {
            if (GetAdminBySid(player.Sid) != null)
            {
                if (GetAdminBySid(player.Sid).Immunity >= GetAdminBySid(activator.SteamID.ToString()).Immunity)
                {
                    continue;
                }
            }
            
            menu.AddMenuOption($" {player.Name}", (controller, option) =>
            {
                ChatMenu BanMenuTimes = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["TimesTitle"]}");

                foreach (var time in Config.Times)
                {
                    string title = $"{time}{Localizer["min"]}";
                    if (time == 0)
                    {
                        title = Localizer["Options.Infinity"].ToString();
                    }

                    BanMenuTimes.AddMenuOption(title, (ccsPlayerController, chatMenuOption) =>
                    {

                        ChatMenu BanMenuReasons = BanMenuDisconnectedPlayersReasonsConstructor(ccsPlayerController, player, time, title);
                        
                        ChatMenus.OpenMenu(ccsPlayerController, BanMenuReasons);
                       
                    });
                }
                

                BanMenuTimes.AddMenuOption($"{Localizer["Options.OwnTime"]}", (playerController, menuOption) =>
                {
                    playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnTimeMessage"]}"]}");
                    for (int i = 0; i < OwnReasonsTyper.Length; i++)
                    {
                        if (OwnReasonsTyper[i] == null)
                        {
                            OwnReasonsTyper[i] = playerController;
                            Actions[i] = "banReasonDisconnected";
                            ActionTargetsDisconnected[i] = player;
                            return;
                        }
                    }
                });
                ChatMenus.OpenMenu(controller, BanMenuTimes);
            });
        }
        
        return menu;
    }

    public ChatMenu BanMenuDisconnectedPlayersReasonsConstructor(CCSPlayerController ccsPlayerController, DisconnectedPlayer player , int time, string title)
    {
        ChatMenu BanMenuReasons =
            new ChatMenu($" {Localizer["PluginTag"]} {Localizer["BanMenuReasonsTitle"]}");
        foreach (var reason in Config.BanReasons)
        {
            BanMenuReasons.AddMenuOption($"{reason}", (playerController, menuOption) =>
            {
                BanManager bm = new BanManager(_dbConnectionString);
                if (bm.IsPlayerBanned(player.Sid))
                {
                    ccsPlayerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                    return;
                }

                string adminsid = ccsPlayerController.SteamID.ToString();
                string AdminName = ccsPlayerController.PlayerName;
                Task.Run(async () =>
                {
                    await bm.BanPlayer(player.Name, player.Sid, player.Ip, adminsid, time, reason);
                    if (Config.LogToVk)
                    {
                        await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], player.Name, player.Sid, player.Ip, AdminName, reason, time);
                    }
                });

                foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                {
                    Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                        .Replace("{name}", player.Name)
                        .Replace("{admin}", ccsPlayerController.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{duration}", title)
                    }");
                }
            });
        }
        BanMenuReasons.AddMenuOption($"{Localizer["Options.OwnReason"]}", (playerController, menuOption) =>
        {
            playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnReasonMessage"]}"]}");
            for (int i = 0; i < OwnReasonsTyper.Length; i++)
            {
                if (OwnReasonsTyper[i] == null)
                {
                    OwnReasonsTyper[i] = playerController;
                    Actions[i] = "banReason";
                    ActionTargetsDisconnected[i] = player;
                    ActionTimes[i] = time;
                    return;
                }
            }
        });
        return BanMenuReasons;
    }
    public ChatMenu BanMenuReasonsConstructor(CCSPlayerController ccsPlayerController, CCSPlayerController player, int time, string title)
    {
        ChatMenu BanMenuReasons =
            new ChatMenu($" {Localizer["PluginTag"]} {Localizer["BanMenuReasonsTitle"]}");
        foreach (var reason in Config.BanReasons)
        {
            BanMenuReasons.AddMenuOption($"{reason}", (playerController, menuOption) =>
            {
                BanManager bm = new BanManager(_dbConnectionString);
                if (bm.IsPlayerBanned(player.SteamID.ToString()))
                {
                    ccsPlayerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                    return;
                }

                string name = player.PlayerName;
                string sid = player.SteamID.ToString();
                string adminsid = ccsPlayerController.SteamID.ToString();
                string AdminName = ccsPlayerController.PlayerName;

                string? ip = player.IpAddress;
                
                Task.Run(async () =>
                {
                    await bm.BanPlayer(name, sid, ip, adminsid, time, reason);
                    if (Config.LogToVk)
                    {
                        await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, time);
                    }
                });
                NativeAPI.IssueServerCommand($"kickid {player.UserId}");

                foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                {
                    Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                        .Replace("{name}", player.PlayerName)
                        .Replace("{admin}", ccsPlayerController.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{duration}", title)
                    }");
                }
            });
        }
        BanMenuReasons.AddMenuOption($"{Localizer["Options.OwnReason"]}", (playerController, menuOption) =>
        {
            playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnReasonMessage"]}"]}");
            for (int i = 0; i < OwnReasonsTyper.Length; i++)
            {
                if (OwnReasonsTyper[i] == null)
                {
                    OwnReasonsTyper[i] = playerController;
                    Actions[i] = "banReason";
                    ActionTargets[i] = player;
                    ActionTimes[i] = time;
                    return;
                }
            }
        });
        return BanMenuReasons;
    }
    
    public ChatMenu MuteMenuConstructor(CCSPlayerController activator)
    {
        ChatMenu menu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["MuteMenuTitle"]}");
        
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Back"]}", (controller, option) =>
        {
            ChatMenus.OpenMenu(activator, AdminMenuConstructor(GetAdminBySid(activator.SteamID.ToString())));
        });
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid) continue;
            if (player == activator) continue;
            if (MutedSids.Contains(player.SteamID.ToString()))
            {
                continue;
            }
            if (GetAdminBySid(player.SteamID.ToString()) != null)
            {
                if (GetAdminBySid(player.SteamID.ToString()).Immunity > GetAdminBySid(activator.SteamID.ToString()).Immunity)
                {
                    continue;
                }
            }
            menu.AddMenuOption($" {player.PlayerName}", (controller, option) =>
            {
                ChatMenu MuteMenuTimes = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["TimesTitle"]}");
                MuteMenuTimes.AddMenuOption($" {Localizer["Options.Back"]}", (controller, option) =>
                {
                    ChatMenus.OpenMenu(controller, AdminMenuConstructor(GetAdminBySid(controller.SteamID.ToString())));
                });
                // Закрыть меню
                MuteMenuTimes.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
                {
                    controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
                });
                foreach (var time in Config.Times)
                {
                    string title = $"{time}{Localizer["min"]}";
                    if (time == 0)
                    {
                        title = Localizer["Options.Infinity"].ToString();
                    }

                    MuteMenuTimes.AddMenuOption(title, (ccsPlayerController, chatMenuOption) =>
                    {

                        ChatMenu BanMenuReasons = MuteMenuReasonsConstructor(ccsPlayerController, player, time, title);
                        
                        ChatMenus.OpenMenu(ccsPlayerController, BanMenuReasons);
                       
                    });
                }
                

                MuteMenuTimes.AddMenuOption($"{Localizer["Options.OwnTime"]}", (playerController, menuOption) =>
                {
                    playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnTimeMessage"]}"]}");
                    for (int i = 0; i < OwnReasonsTyper.Length; i++)
                    {
                        if (OwnReasonsTyper[i] == null)
                        {
                            OwnReasonsTyper[i] = playerController;
                            Actions[i] = "muteTime";
                            ActionTargets[i] = player;
                            return;
                        }
                    }
                });
                ChatMenus.OpenMenu(controller, MuteMenuTimes);
            });
        }
        
        return menu;
    }

    public ChatMenu MuteMenuReasonsConstructor(CCSPlayerController ccsPlayerController, CCSPlayerController player, int time, string title)
    {
        ChatMenu MuteMenuReasons =
            new ChatMenu($" {Localizer["PluginTag"]} {Localizer["MuteMenuReasonsTitle"]}");
        // Назад
        MuteMenuReasons.AddMenuOption($" {Localizer["Options.Back"]}", (controller, option) =>
        {
            ChatMenus.OpenMenu(ccsPlayerController, AdminMenuConstructor(GetAdminBySid(ccsPlayerController.SteamID.ToString())));
        });
        // Закрыть меню
        MuteMenuReasons.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });
        foreach (var reason in Config.MuteReason)
        {
            MuteMenuReasons.AddMenuOption($"{reason}", (playerController, menuOption) =>
            {
                BanManager bm = new BanManager(_dbConnectionString);
                if (MutedSids.Contains(player.SteamID.ToString()))
                {
                    ccsPlayerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                    return;
                }

                string name = player.PlayerName;
                string sid = player.SteamID.ToString();
                string adminsid = ccsPlayerController.SteamID.ToString();
                string AdminName =  ccsPlayerController.PlayerName;
                
                Task.Run(async () =>
                {
                    await bm.MutePlayer(name, sid, adminsid, time, reason);
                    if (Config.LogToVk)
                    {
                        await vkLog.sendPunMessage(Config.LogToVkMessages["MuteMessage"], name, sid, "Undefined", AdminName, reason, time);
                    }
                    List<string> sids = GetListSids();
                    Task.Run(async () =>
                    {
                        await SetMutedPlayers(sids);
                    });
                });
                

                foreach (var str in Localizer["MuteMessage"].ToString().Split("\n"))
                {
                    Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                        .Replace("{name}", player.PlayerName)
                        .Replace("{admin}", ccsPlayerController.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{duration}", title)
                    }");
                }
            });
        }
        MuteMenuReasons.AddMenuOption($"{Localizer["Options.OwnReason"]}", (playerController, menuOption) =>
        {
            playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnReasonMessage"]}"]}");
            for (int i = 0; i < OwnReasonsTyper.Length; i++)
            {
                if (OwnReasonsTyper[i] == null)
                {
                    OwnReasonsTyper[i] = playerController;
                    Actions[i] = "muteReason";
                    ActionTargets[i] = player;
                    ActionTimes[i] = time;
                    return;
                }
            }
        });
        return MuteMenuReasons;
    }
    
    public ChatMenu GagMenuConstructor(CCSPlayerController activator)
    {
        ChatMenu menu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["GagMenuTitle"]}");
        
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Back"]}", (controller, option) =>
        {
            ChatMenus.OpenMenu(activator, AdminMenuConstructor(GetAdminBySid(activator.SteamID.ToString())));
        });
        // Закрыть меню
        menu.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });
        
        
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid) continue;
            if (player == activator) continue;
            if (GaggedSids.Contains(player.SteamID.ToString()))
            {
                continue;
            }
            if (GetAdminBySid(player.SteamID.ToString()) != null)
            {
                if (GetAdminBySid(player.SteamID.ToString()).Immunity > GetAdminBySid(activator.SteamID.ToString()).Immunity)
                {
                    continue;
                }
            }
            
            menu.AddMenuOption($" {player.PlayerName}", (controller, option) =>
            {
                ChatMenu MuteMenuTimes = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["TimesTitle"]}");
                MuteMenuTimes.AddMenuOption($" {Localizer["Options.Back"]}", (controller, option) =>
                {
                    ChatMenus.OpenMenu(controller, AdminMenuConstructor(GetAdminBySid(controller.SteamID.ToString())));
                });
                // Закрыть меню
                MuteMenuTimes.AddMenuOption($" {Localizer["Options.Exit"]}", (controller, option) =>
                {
                    controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
                });
                foreach (var time in Config.Times)
                {
                    string title = $"{time}{Localizer["min"]}";
                    if (time == 0)
                    {
                        title = Localizer["Options.Infinity"].ToString();
                    }

                    MuteMenuTimes.AddMenuOption(title, (ccsPlayerController, chatMenuOption) =>
                    {

                        ChatMenu BanMenuReasons = GagMenuReasonsConstructor(ccsPlayerController, player, time, title);
                        
                        ChatMenus.OpenMenu(ccsPlayerController, BanMenuReasons);
                       
                    });
                }
                

                MuteMenuTimes.AddMenuOption($"{Localizer["Options.OwnTime"]}", (playerController, menuOption) =>
                {
                    playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnTimeMessage"]}"]}");
                    for (int i = 0; i < OwnReasonsTyper.Length; i++)
                    {
                        if (OwnReasonsTyper[i] == null)
                        {
                            OwnReasonsTyper[i] = playerController;
                            Actions[i] = "gagTime";
                            ActionTargets[i] = player;
                            return;
                        }
                    }
                });
                ChatMenus.OpenMenu(controller, MuteMenuTimes);
            });
        }
        
        return menu;
    }

    public ChatMenu GagMenuReasonsConstructor(CCSPlayerController ccsPlayerController, CCSPlayerController player, int time, string title)
    {
        ChatMenu MuteMenuReasons =
            new ChatMenu($" {Localizer["PluginTag"]} {Localizer["GagMenuReasonsTitle"]}");
        MuteMenuReasons.AddMenuOption($" {Localizer["Options.Back"]}", (ccsPlayerController, option) =>
        {
            ChatMenus.OpenMenu(ccsPlayerController, AdminMenuConstructor(GetAdminBySid(ccsPlayerController.SteamID.ToString())));
        });
        // Закрыть меню
        MuteMenuReasons.AddMenuOption($" {Localizer["Options.Exit"]}", (ccsPlayerController, option) =>
        {
            ccsPlayerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });
        foreach (var reason in Config.GagReason)
        {
            MuteMenuReasons.AddMenuOption($"{reason}", (playerController, menuOption) =>
            {
                BanManager bm = new BanManager(_dbConnectionString);
                if (GaggedSids.Contains(player.SteamID.ToString()))
                {
                    ccsPlayerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                    return;
                }

                string name = player.PlayerName;
                string sid = player.SteamID.ToString();
                string adminsid = ccsPlayerController.SteamID.ToString();
                string AdminName = ccsPlayerController.PlayerName;
                string ip = player.IpAddress;
                
                Task.Run(async () =>
                {
                    await bm.GagPlayer(name, sid, adminsid, time, reason);
                    if (Config.LogToVk)
                    {
                        await vkLog.sendPunMessage(Config.LogToVkMessages["GagMessage"], name, sid, ip, AdminName, reason, time);
                    }
                });
                List<string> gagCheckSids = GetListSids(); 
                Task.Run(async () =>
                {
                    await SetGaggedPlayers(gagCheckSids);
                    Server.NextFrame(() => {
                                UpdateChatColorsGagged();
                            });
                });
                

                foreach (var str in Localizer["GagMessage"].ToString().Split("\n"))
                {
                    Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                        .Replace("{name}", player.PlayerName)
                        .Replace("{admin}", ccsPlayerController.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{duration}", title)
                    }");
                }
            });
        }
        MuteMenuReasons.AddMenuOption($"{Localizer["Options.OwnReason"]}", (playerController, menuOption) =>
        {
            playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer[$"{Localizer["OwnReasonMessage"]}"]}");
            for (int i = 0; i < OwnReasonsTyper.Length; i++)
            {
                if (OwnReasonsTyper[i] == null)
                {
                    OwnReasonsTyper[i] = playerController;
                    Actions[i] = "gagReason";
                    ActionTargets[i] = player;
                    ActionTimes[i] = time;
                    return;
                }
            }
        });
        return MuteMenuReasons;
    }

    public ChatMenu UnMuteGagMenuConstructor(CCSPlayerController activator) //UnMute UnGag
    {
        ChatMenu UnMenu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["UnGagMuteMenuTitle"]}");
        UnMenu.AddMenuOption($" {Localizer["Options.Back"]}", (activator, option) =>
        {
            ChatMenus.OpenMenu(activator, AdminMenuConstructor(GetAdminBySid(activator.SteamID.ToString())));
        });
        // Закрыть меню
        UnMenu.AddMenuOption($" {Localizer["Options.Exit"]}", (activator, option) =>
        {
            activator.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid) continue;
            BanManager bm = new BanManager(_dbConnectionString);
            bool playerGagged = false;
            bool playerMuted = false;
            if (GaggedSids.Contains(player.SteamID.ToString()))
            {
                playerGagged = true;
            }
            if (MutedSids.Contains(player.SteamID.ToString()))
            {
                playerMuted = true;
            }

            if (!playerMuted && !playerGagged)
            {
                continue;
            }

            string title = $" {player.PlayerName}";
            if (playerGagged)
            {
                title += $" {Localizer["GAGGED"]}";
            }
            if (playerMuted)
            {
                title += $" {Localizer["MUTED"]}";
            }

            UnMenu.AddMenuOption(title, (controller, option) =>
            {
                ChatMenu playerMenu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["UnGagMuteMenuTitleWhenPlayerSelected"].ToString().Replace("{name}", player.PlayerName)}");
                playerMenu.AddMenuOption($" {Localizer["Options.Back"]}", (activator, option) =>
                {
                    ChatMenus.OpenMenu(activator, AdminMenuConstructor(GetAdminBySid(activator.SteamID.ToString())));
                });
                // Закрыть меню
                playerMenu.AddMenuOption($" {Localizer["Options.Exit"]}", (activator, option) =>
                {
                    controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
                });
                if (playerGagged)
                {
                    playerMenu.AddMenuOption($" {Localizer["Options.UnGag"]}", (playerController, menuOption) =>
                    {
                        BanManager bm = new BanManager(_dbConnectionString);
                        string adminName = playerController.PlayerName;
                        Task.Run(async () =>
                        {
                            await bm.UnGagPlayer(player.SteamID.ToString(), playerController.SteamID.ToString());
                            if (Config.LogToVk)
                            {
                                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnGagMessage"], player.PlayerName, player.SteamID.ToString(), adminName);
                            }
                        });
                        List<string> gagCheckSids = GetListSids(); 
                        Task.Run(async () =>
                        {
                            await SetGaggedPlayers(gagCheckSids);
                            Server.NextFrame(() => {
                                UpdateChatColorsGagged();
                            });
                        });
                        foreach (var str in Localizer["UnGagMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", player.PlayerName)
                                .Replace("{admin}", activator.PlayerName)}");
                        }
                        
                    });
                }
                if (playerMuted)
                {
                    playerMenu.AddMenuOption($" {Localizer["Options.UnMute"]}", (playerController, menuOption) =>
                    {
                        BanManager bm = new BanManager(_dbConnectionString);
                        string adminName = playerController.PlayerName;
                        Task.Run(async () =>
                        {
                            await bm.UnMutePlayer(player.SteamID.ToString(), playerController.SteamID.ToString());
                            if (Config.LogToVk)
                            {
                                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnMuteMessage"], player.PlayerName, player.SteamID.ToString(), adminName);
                            }

                            List<string> sids = GetListSids();
                            Task.Run(async () =>
                            {
                                await SetMutedPlayers(sids);
                            });
                        });
                        foreach (var str in Localizer["UnMuteMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", player.PlayerName)
                                .Replace("{admin}", activator.PlayerName)}");
                        }
                    });
                }

                ChatMenus.OpenMenu(activator, playerMenu);
            });


        }

        return UnMenu;
    }
    #endregion

}