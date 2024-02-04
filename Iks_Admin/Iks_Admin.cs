using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.VisualBasic;
using MySqlConnector;
using Serilog.Sinks.File;
using cssAdminManager = CounterStrikeSharp.API.Modules.Admin;

namespace Iks_Admin;

// Создание БД админов
// CREATE TABLE `u2194959_FallowCS2`.`iks_admins` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `name` VARCHAR(32) NOT NULL , `flags` VARCHAR(32) NOT NULL , `immunity` INT NOT NULL , PRIMARY KEY (`id`), UNIQUE (`sid`), UNIQUE (`name`)) ENGINE = InnoDB;


public class Iks_Admin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName { get; } = "Iks_Admin";
    public override string ModuleVersion { get; } = "1.1.4";
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
            "CREATE TABLE IF NOT EXISTS `iks_admins` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `name` VARCHAR(32) NOT NULL , `flags` VARCHAR(32) NOT NULL , `immunity` INT NOT NULL, `group_id` INT NOT NULL DEFAULT '-1' ,`end` INT NOT NULL , `server_id` VARCHAR(64) NOT NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB;";
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

        sql = "CREATE TABLE IF NOT EXISTS `iks_bans` ( `id` INT NOT NULL AUTO_INCREMENT , `name` VARCHAR(32) NOT NULL ,`sid` VARCHAR(32) NOT NULL, `ip` VARCHAR(32) NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL, `BanType` INT(1) NOT NULL DEFAULT '0', `Unbanned` INT(1) NOT NULL DEFAULT '0', `UnbannedBy` VARCHAR(32) NULL , `server_id` VARCHAR(1) NOT NULL DEFAULT '', PRIMARY KEY (`id`)) ENGINE = InnoDB;";
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

        sql = "CREATE TABLE IF NOT EXISTS `iks_mutes` ( `id` INT NOT NULL AUTO_INCREMENT , `name` VARCHAR(32) NOT NULL , `sid` VARCHAR(32) NOT NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL, `Unbanned` INT(1) NOT NULL DEFAULT '0', `UnbannedBy` VARCHAR(32) NULL, `server_id` VARCHAR(1) NOT NULL DEFAULT '', PRIMARY KEY (`id`)) ENGINE = InnoDB;";
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

        sql = "CREATE TABLE IF NOT EXISTS `iks_gags` ( `id` INT NOT NULL AUTO_INCREMENT , `name` VARCHAR(32) NOT NULL , `sid` VARCHAR(32) NOT NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL, `Unbanned` INT(1) NOT NULL DEFAULT '0', `UnbannedBy` VARCHAR(32) NULL , `server_id` VARCHAR(1) NOT NULL DEFAULT '', PRIMARY KEY (`id`)) ENGINE = InnoDB;";
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
            AdminManager am = new AdminManager(_dbConnectionString);
            List<string> sids = GetListSids();
            Task.Run(async () =>
            {
                await SetMutedPlayers(sids);
                await SetGaggedPlayers(sids);

                foreach (var admin in admins)
                {
                    if (await am.DeleteAdminIfEnd(admin.SteamId))
                    {
                        Console.WriteLine($"[Iks_Admin] admin: {admin.Name} removed by end time");
                        admins.Remove(admin);
                    }
                }
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

    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [ConsoleCommand("css_reload_infractions", "css_reload_infractions {sid}")]
    public void OnReloadInfractionsCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        string identity = info.GetArg(1);
        if (identity.Length < 17) return;
        var sids = GetListSids();
        Task.Run(async () =>
        {
            if (await bm.IsPlayerBannedAsync(identity, Config))
            {
                Server.NextFrame(() =>
                {
                    KickBySid(identity);
                });
            }
            if (await bm.IsPlayerGaggedAsync(identity, Config))
            {
                if (!GaggedSids.Contains(identity))
                {
                    GaggedSids.Add(identity);
                }
            }
            else
            {
                if (GaggedSids.Contains(identity))
                {
                    GaggedSids.Remove(identity);
                }
            }

            if (await bm.IsPlayerMutedAsync(identity, Config))
            {
                if (!MutedSids.Contains(identity))
                {
                    MutedSids.Add(identity);
                }
            }
            else
            {
                if (MutedSids.Contains(identity))
                {
                    MutedSids.Remove(identity);
                }
            }
            Server.NextFrame(() =>
            {
                SetMuteForPlayers();
                UpdateChatColorsGagged();
            });
        });
        Server.NextFrame(() =>
        {
            ReloadAdmins(null);
        });
    }

    public void KickBySid(string sid)
    {
        if (Utilities.GetPlayerFromSteamId(UInt64.Parse(sid)) != null)
        {
            NativeAPI.IssueServerCommand($"kickid {Utilities.GetPlayerFromSteamId(UInt64.Parse(sid)).UserId}");
        }
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

    [ConsoleCommand("css_adminadd", "css_adminadd <sid> <name> <flags/-> <immunity> <group_id> <time> <server_id/ - (ALL SERVERS)>")]
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
        if (args.Count > 5)
        {
            if (Int32.Parse(args[6]) > 0)
            {
                end = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (Int32.Parse(args[6]) * 60);
            }
        }
        if (args.Count > 7)
        {
            server_id = args[7];
            if (server_id == "-")
            {
                server_id = "";
            }
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
        Task.Run(async () =>
        {
            await am.AddAdmin(sid, name, flags, immunity, group_id, end, server_id);
            Server.NextFrame(() =>
            {
                ReloadAdmins(null);
            });
        });

        if (controller != null)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["AdminCreated"]}");
        }
        Console.WriteLine("[Iks_Admin] Admin created!");
    }
    #region RCON COMMANS
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [ConsoleCommand("css_rban", "css_rban <sid> <ip/-(Auto)> <adminsid/CONSOLE> <duration> <reason> <BanType (0 - default / 1 - ip> <name if needed")]
    public void OnRBanCommand(CCSPlayerController? controller, CommandInfo info)
    {
        var args = GetArgsFromCommandLine(info.GetCommandString);
        string sid = args[1];
        string ip = args[2];
        string adminSid = args[3];
        int duration = Int32.Parse(args[4]);
        string reason = args[5];
        int BanType = Int32.Parse(args[6]);
        string name = "Undefined";
        string adminName = GetAdminBySid(adminSid) != null ? GetAdminBySid(adminSid).Name : adminSid;

        CCSPlayerController? target = Utilities.GetPlayerFromSteamId(UInt64.Parse(sid));
        bool offline = target == null;
        if (args.Count > 7)
        {
            name = args[7];
        }
        else if (target != null)
        {
            name = target.PlayerName;
        }
        if (Helper.CanExecute(adminSid, sid, "b", admins) == false && adminSid != "CONSOLE")
        {
            info.ReplyToCommand("[Iks_Admin] Selected Admin can't execute it For target!");
            return;
        }
        if (ip == "-")
        {
            ip = "Undefined";
        }
        if (target != null)
        {
            ip = target.IpAddress;
        }
        if (ip == "Undefined" && BanType == 1)
        {
            info.ReplyToCommand("[Iks_Admin] Ip is Undefined!");
            return;
        }

        BanManager bm = new BanManager(_dbConnectionString);


        Task.Run(async () =>
        {
            if (await bm.IsPlayerBannedAsync(sid, Config))
            {
                Console.WriteLine("[Iks_Admin] Player Alredy banned");
                return;
            }
            if (BanType == 1)
            {
                await bm.BanPlayerIp(name, sid, ip, adminSid, duration, reason, Config);
            }
            if (BanType == 0)
            {
                await bm.BanPlayer(name, sid, ip, adminSid, duration, reason, Config);
            }
            Console.WriteLine("[Iks_Admin] Player banned!");

            Server.NextFrame(() =>
            {
                PrintBanMessage(name, adminName, duration, reason);
            });
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, adminName, reason, duration, offline);
            }
        });
        if (target != null)
        {
            NativeAPI.IssueServerCommand($"kickid {target.UserId}");
        }


    }

    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [ConsoleCommand("css_runban", "css_runban <sid> <adminsid/CONSOLE>")]
    public void OnRUnBanCommand(CCSPlayerController? controller, CommandInfo info)
    {
        var args = GetArgsFromCommandLine(info.GetCommandString);
        string sid = args[1];
        string adminSid = args[2];
        Admin? admin = GetAdminBySid(adminSid);

        if (!Helper.AdminHaveFlag(adminSid, "u", admins))
        {
            info.ReplyToCommand("[Iks_Admin] Admin can't execute this command!");
            return;
        }

        BanManager bm = new BanManager(_dbConnectionString);
        BannedPlayer? bannedPlayer = null;
        Task.Run(async () =>
        {
            bannedPlayer = await bm.GetPlayerBan(sid, Config);
            await bm.UnBanPlayer(sid, adminSid, Config);
            if (bannedPlayer != null && (admin != null || adminSid == "CONSOLE"))
            {
                Server.NextFrame(() =>
                {
                    PrintUnbanMessage(bannedPlayer.Name, admin != null ? admin.Name : adminSid);
                });
            }
            if (Config.LogToVk)
            {
                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnBanMessage"], bannedPlayer.Name, sid, admin != null ? admin.Name : adminSid, bannedPlayer.Ip, true);
            }
        });
        info.ReplyToCommand("[Iks_Admin] Player Unbanned!");
    }

    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [ConsoleCommand("css_rgag", "css_rgag <sid> <adminsid/CONSOLE> <duration> <reason> <name if needed")]
    public void OnRGagCommand(CCSPlayerController? controller, CommandInfo info)
    {
        var args = GetArgsFromCommandLine(info.GetCommandString);
        string sid = args[1];
        string adminSid = args[2];
        int duration = Int32.Parse(args[3]);
        string reason = args[4];
        string name = "Undefined";
        string adminName = GetAdminBySid(adminSid) != null ? GetAdminBySid(adminSid).Name : adminSid;

        CCSPlayerController? target = Utilities.GetPlayerFromSteamId(UInt64.Parse(sid));
        if (args.Count > 5)
        {
            name = args[5];
        }
        else if (target != null)
        {
            name = target.PlayerName;
        }
        if (Helper.CanExecute(adminSid, sid, "g", admins) == false && adminSid != "CONSOLE")
        {
            info.ReplyToCommand("[Iks_Admin] Selected Admin can't execute it For target!");
            return;
        }

        BanManager bm = new BanManager(_dbConnectionString);
        var sids = GetListSids();
        bool offline = target == null;
        string ip = target == null ? "Undefined" : target.IpAddress;
        Task.Run(async () =>
        {
            if (await bm.IsPlayerGaggedAsync(sid, Config))
            {
                Console.WriteLine("[Iks_Admin] Player Alredy gagged");
                return;
            }
            await bm.GagPlayer(name, sid, adminSid, duration, reason, Config);
            Console.WriteLine("[Iks_Admin] Player gagged!");
            await SetGaggedPlayers(sids);
            Server.NextFrame(() =>
            {
                UpdateChatColorsGagged();
                PrintGagMessage(name, adminName, duration, reason);
            });
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["GagMessage"], name, sid, ip, adminName, reason, duration, offline);
            }
        });

    }

    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [ConsoleCommand("css_rungag", "css_rungag <sid> <adminsid/CONSOLE>")]
    public void OnRUnGagCommand(CCSPlayerController? controller, CommandInfo info)
    {
        var args = GetArgsFromCommandLine(info.GetCommandString);
        string sid = args[1];
        string adminSid = args[2];
        Admin? admin = GetAdminBySid(adminSid);

        if (!Helper.AdminHaveFlag(adminSid, "g", admins) && adminSid != "CONSOLE")
        {
            info.ReplyToCommand("[Iks_Admin] Admin can't execute this command!");
            return;
        }

        BanManager bm = new BanManager(_dbConnectionString);
        BannedPlayer? bannedPlayer = null;
        var sids = GetListSids();
        Task.Run(async () =>
        {
            bannedPlayer = await bm.GetPlayerGag(sid, Config);
            await bm.UnGagPlayer(sid, adminSid, Config);
            await SetGaggedPlayers(sids);
            if (bannedPlayer != null && (admin != null || adminSid == "CONSOLE"))
            {
                Server.NextFrame(() =>
                {
                    PrintUnGagMessage(bannedPlayer.Name, admin != null ? admin.Name : adminSid);

                });
                if (Config.LogToVk)
                {
                    await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnGagMessage"], bannedPlayer.Name, sid, admin != null ? admin.Name : adminSid, bannedPlayer.Ip, true);
                }

            }
        });
        UpdateChatColorsGagged();

        info.ReplyToCommand("[Iks_Admin] Player Ungaged!");
    }

    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [ConsoleCommand("css_rmute", "css_rmute <sid> <adminsid/CONSOLE> <duration> <reason> <name if needed")]
    public void OnRMuteCommand(CCSPlayerController? controller, CommandInfo info)
    {
        var args = GetArgsFromCommandLine(info.GetCommandString);
        string sid = args[1];
        string adminSid = args[2];
        int duration = Int32.Parse(args[3]);
        string reason = args[4];
        string name = "Undefined";
        string adminName = GetAdminBySid(adminSid) != null ? GetAdminBySid(adminSid).Name : adminSid;

        CCSPlayerController? target = Utilities.GetPlayerFromSteamId(UInt64.Parse(sid));
        if (args.Count > 5)
        {
            name = args[5];
        }
        else if (target != null)
        {
            name = target.PlayerName;
        }
        if (Helper.CanExecute(adminSid, sid, "m", admins) == false && adminSid != "CONSOLE")
        {
            info.ReplyToCommand("[Iks_Admin] Selected Admin can't execute it For target!");
            return;
        }

        BanManager bm = new BanManager(_dbConnectionString);
        var sids = GetListSids();
        bool offline = target == null;
        string ip = target == null ? "Undefined" : target.IpAddress;
        Task.Run(async () =>
        {
            if (await bm.IsPlayerMutedAsync(sid, Config))
            {
                Console.WriteLine("[Iks_Admin] Player Alredy muted!");
                return;
            }
            await bm.MutePlayer(name, sid, adminSid, duration, reason, Config);
            Console.WriteLine("[Iks_Admin] Player muted!");
            await SetMutedPlayers(sids);
            Server.NextFrame(() =>
            {
                PrintMuteMessage(name, adminName, duration, reason);
            });
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["MuteMessage"], name, sid, ip, adminName, reason, duration, offline);
            }
        });

    }

    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [ConsoleCommand("css_runmute", "css_runmute <sid> <adminsid/CONSOLE>")]
    public void OnRUnMuteCommand(CCSPlayerController? controller, CommandInfo info)
    {
        var args = GetArgsFromCommandLine(info.GetCommandString);
        string sid = args[1];
        string adminSid = args[2];
        Admin? admin = GetAdminBySid(adminSid);

        if (!Helper.AdminHaveFlag(adminSid, "m", admins) && adminSid != "CONSOLE")
        {
            info.ReplyToCommand("[Iks_Admin] Admin can't execute this command!");
            return;
        }

        BanManager bm = new BanManager(_dbConnectionString);
        BannedPlayer? bannedPlayer = null;
        var sids = GetListSids();
        Task.Run(async () =>
        {
            bannedPlayer = await bm.GetPlayerMute(sid, Config);
            await bm.UnMutePlayer(sid, adminSid, Config);
            await SetMutedPlayers(sids);
            if (bannedPlayer != null && (admin != null || adminSid == "CONSOLE"))
            {
                Server.NextFrame(() =>
                {
                    PrintUnMuteMessage(bannedPlayer.Name, admin != null ? admin.Name : adminSid);
                });
            }
            if (Config.LogToVk)
            {
                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnMuteMessage"], bannedPlayer.Name, sid, admin != null ? admin.Name : adminSid, bannedPlayer.Ip, true);
            }
        });
        info.ReplyToCommand("[Iks_Admin] Player Unmuted!");
    }
    #endregion


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
        Task.Run(async () =>
        {
            await am.DeleteAdmin(sid);
            Server.NextFrame(() =>
            {
                ReloadAdmins(null);
            });
        });

        if (controller != null)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["AdminDeleted"]}");
        }
        Console.WriteLine("[Iks_Admin] Admin deleted!");
    }

    [ConsoleCommand("css_slay", "css_slay uid/sid")]
    public void OnSlayCommand(CCSPlayerController? controller, CommandInfo info)
    {
        Admin? admin = null;
        if (controller != null)
        {
            admin = GetAdminBySid(controller.SteamID.ToString());
        }
        CCSPlayerController? target = GetPlayerFromSidOrUid(info.GetArg(1));
        string adminName = controller == null ? "CONSOLE" : controller.PlayerName;
        if (target == null && controller == null)
        {
            Console.WriteLine($"[Iks_Admin] Player not finded!");
            return;
        }
        if (admin == null && controller != null)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
            return;
        }
        if (controller == null)
        {
            if (!Helper.AdminHaveFlag(controller.SteamID.ToString(), "s", admins))
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }

        }
        if (target == null && controller != null)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFinded"]}");
        }

        SlayFunc(target, controller);

    }

    [ConsoleCommand("css_kick", "css_kick uid/sid reason")]
    public void OnKickCommand(CCSPlayerController? controller, CommandInfo info)
    {
        List<string> args = GetArgsFromCommandLine(info.GetCommandString);
        string reason = args[2];
        string AdminName = "Console";
        Admin? admin = null;
        CCSPlayerController? target = GetPlayerFromSidOrUid(info.GetArg(1));
        if (target == null && controller == null)
        {
            Console.WriteLine($"[Iks_Admin] Player not finded!");
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
                .Replace("{reason}", reason)}");
        }
        string name = target.PlayerName;
        string sid = target.SteamID.ToString();
        string ip = "Undefined";
        if (target != null)
        {
            ip = target.IpAddress;
        }
        Task.Run(async () =>
        {
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["KickMessage"], name, sid, ip, AdminName, reason, 0, false);
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
        if (identity.Trim() == "")
        {
            return;
        }

        int time = Int32.Parse(info.GetArg(2));
        string reason = args[3];
        string name = "Undefined";
        string ip = "Undefined";
        string sid = identity.Length >= 17 ? identity : "Undefined";

        // Установка Name
        if (args.Count > 4)
        {
            if (args[4].Trim() != "")
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
            }
            else // Если игрок не админ: HaveNotAccess
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }

        }

        bool offline = target == null;

        Task.Run(async () =>
        {
            if (await bm.IsPlayerBannedAsync(identity, Config)) // Проверка есть ли бан по identity
            {
                Server.NextFrame(() =>
                {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }
            if (await bm.IsPlayerBannedAsync(ip, Config)) // Проверка есть ли бан по ip
            {
                Server.NextFrame(() =>
                {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }

            //Если всё нормально то баним
            await bm.BanPlayer(name, sid, ip, AdminSid, time, reason, Config);
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, time, offline);
            }
            Server.NextFrame(() =>
            {
                PrintBanMessage(name, AdminName, time, reason);
            });
        });



        // Кикаем игрока после бана
        if (target != null)
            NativeAPI.IssueServerCommand($"kickid {target.UserId}");
    }

    [ConsoleCommand("css_banip", "css_banip #uid/#sid/name/ip(if offline) duration reason <name if needed>")]
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
            if (args[4].Trim() != "")
            {
                name = args[4];
            }
        }


        CCSPlayerController? target = identity.StartsWith("#") ? null : GetPlayerFromSidOrUid(identity); // Проверка есть ли игрок которого банят на сервере

        if (target == null)
        {
            ip = identity.Replace("#", "");
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

        if (ip == "Undefined")
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
            }
            else // Если игрок не админ: HaveNotAccess
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }

        }
        bool offline = target == null;
        Task.Run(async () =>
        {
            if (await bm.IsPlayerBannedAsync(identity, Config)) // Проверка есть ли бан по identity
            {
                Server.NextFrame(() =>
                {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }
            if (await bm.IsPlayerBannedAsync(ip, Config)) // Проверка есть ли бан по ip
            {
                Server.NextFrame(() =>
                {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }

            //Если всё нормально то баним ПО АЙПИ
            await bm.BanPlayerIp(name, sid, ip, AdminSid, time, reason, Config);
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, time, offline);
            }
            Server.NextFrame(() =>
            {
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
        if (arg.Trim() == "")
        {
            return;
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
            if (!admin.Flags.Contains("z") && !admin.Flags.Contains("u"))
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
            adminSid = controller.SteamID.ToString();
            adminName = controller.PlayerName;
        }
        BannedPlayer? bannedPlayer = null;
        Task.Run(async () =>
        {
            bannedPlayer = await bm.GetPlayerBan(arg, Config);
            if (bannedPlayer != null)
            {
                Server.NextFrame(() =>
                {
                    PrintUnbanMessage(bannedPlayer.Name, adminName);
                });
            }
            await bm.UnBanPlayer(arg, adminSid, Config);
            if (Config.LogToVk)
            {
                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnBanMessage"], bannedPlayer.Name, arg, adminName, bannedPlayer.Ip, true);
            }
        });


    }


    public void PrintUnbanMessage(string playerName, string adminName)
    {
        foreach (var str in Localizer["UnBanMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", playerName)
                .Replace("{admin}", adminName)}");
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
                .Replace("{duration}", title)}");
        }
    }



    // GAG
    [ConsoleCommand("css_gag", "css_gag uid/sid duration reason <name if needed>")]
    public void OnGagCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);

        List<string> args = GetArgsFromCommandLine(info.GetCommandString);

        string identity = info.GetArg(1);
        if (identity.Trim() == "")
        {
            return;
        }
        int time = Int32.Parse(info.GetArg(2));
        string reason = args[3];
        string name = "Undefined";
        string sid = identity.Length >= 17 ? identity : "Undefined";

        // Установка Name
        if (args.Count > 4)
        {
            if (args[4].Trim() != "")
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
            }
            else // Если игрок не админ: HaveNotAccess
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }

        }
        List<string> sids = GetListSids();
        bool offline = target == null;
        string ip = "Undefined";
        if (target != null)
        {
            ip = target.IpAddress;
        }
        Task.Run(async () =>
        {
            if (await bm.IsPlayerGaggedAsync(sid, Config)) // Проверка есть ли бан по identity
            {
                Server.NextFrame(() =>
                {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }

            //Если всё нормально то баним
            await bm.GagPlayer(name, sid, AdminSid, time, reason, Config);
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["GagMessage"], name, sid, ip, AdminName, reason, time, offline);
            }
            await SetGaggedPlayers(sids);

            Server.NextFrame(() =>
            {
                UpdateChatColorsGagged();
                PrintGagMessage(name, AdminName, time, reason);
            });
        });

    }

    [ConsoleCommand("css_ungag", "css_ungag sid/uid")]
    public void OnUnGagCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        var args = GetArgsFromCommandLine(info.GetCommandString);
        string identity = args[1];
        if (identity.Trim() == "")
        {
            return;
        }

        string sid = identity;

        CCSPlayerController? target = GetPlayerFromSidOrUid(identity);

        if (target != null) sid = target.SteamID.ToString();

        if (sid.Length != 17)
        {
            Console.WriteLine($"[Iks_Admin] sid.Length = {sid.Length}!");
            return;
        }

        Admin? admin = null;
        if (controller != null)
        {
            admin = GetAdminBySid(controller.SteamID.ToString());
        }
        if (admin != null)
        {
            if (!Helper.AdminHaveFlag(controller.SteamID.ToString(), "g", admins))
            {
                controller.PrintToChat($" {Localizer["PlguinTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
        }

        var sids = GetListSids();

        BannedPlayer? mutedPlayer = null;

        string adminName = controller == null ? "CONSOLE" : controller.PlayerName;
        string adminSid = controller == null ? "CONSOLE" : controller.SteamID.ToString();

        string ip = target != null ? target.IpAddress : "Undefined";
        bool offline = target == null;

        Task.Run(async () =>
        {
            if (await bm.IsPlayerGaggedAsync(sid, Config) == false)
            {
                Console.WriteLine("[Iks_Admin] Player not gaged!");
                return;
            }
            mutedPlayer = await bm.GetPlayerGag(sid, Config);
            if (mutedPlayer != null)
            {
                Server.NextFrame(() =>
                {
                    PrintUnGagMessage(mutedPlayer.Name, adminName);
                });
            }

            await bm.UnGagPlayer(sid, adminSid, Config);
            Console.WriteLine("[Iks_Admin] Player ungaged!");
            if (Config.LogToVk)
            {
                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnGagMessage"], mutedPlayer.Name, sid, adminName, ip, offline);
            }
            await SetMutedPlayers(sids);
        });


    }


    public void PrintUnGagMessage(string playerName, string adminName)
    {
        foreach (var str in Localizer["UnGagMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", playerName)
                .Replace("{admin}", adminName)}");
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
                .Replace("{duration}", title)}");
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
        if (identity.Trim() == "")
        {
            return;
        }
        int time = Int32.Parse(info.GetArg(2));
        string reason = args[3];
        string name = "Undefined";
        string sid = identity.Length >= 17 ? identity : "Undefined";

        // Установка Name        
        if (args.Count > 4)
        {
            if (args[4].Trim() != "")
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
            }
            else // Если игрок не админ: HaveNotAccess
            {
                controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }

        }
        List<string> sids = GetListSids();
        bool offline = target == null;
        string ip = "Undefined";
        if (target != null)
        {
            ip = target.IpAddress;
        }
        Task.Run(async () =>
        {
            if (await bm.IsPlayerMutedAsync(sid, Config)) // Проверка есть ли бан по identity
            {
                Server.NextFrame(() =>
                {
                    NotifyIfPlayerAlredyBanned(name, AdminSid);
                });
                return;
            }

            //Если всё нормально то баним
            await bm.MutePlayer(name, sid, AdminSid, time, reason, Config);
            if (Config.LogToVk)
            {
                await vkLog.sendPunMessage(Config.LogToVkMessages["MuteMessage"], name, sid, ip, AdminName, reason, time, offline);
            }
            await SetMutedPlayers(sids);
            Server.NextFrame(() =>
            {
                PrintMuteMessage(name, AdminName, time, reason);
            });
        });

    }

    [ConsoleCommand("css_unmute", "css_unmute sid/uid")]
    public void OnUnMuteCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        var args = GetArgsFromCommandLine(info.GetCommandString);
        string identity = args[1];
        if (identity.Trim() == "")
        {
            return;
        }

        string sid = identity;

        CCSPlayerController? target = GetPlayerFromSidOrUid(identity);

        if (target != null)
        {
            sid = target.SteamID.ToString();
        }

        if (sid.Length != 17)
        {
            Console.WriteLine($"[Iks_Admin] sid.Length = {sid.Length}!");
            return;
        }

        Admin? admin = null;
        if (controller != null)
        {
            admin = GetAdminBySid(controller.SteamID.ToString());
        }
        if (admin != null)
        {
            if (!Helper.AdminHaveFlag(controller.SteamID.ToString(), "m", admins))
            {
                controller.PrintToChat($" {Localizer["PlguinTag"]} {Localizer["HaveNotAccess"]}");
                return;
            }
        }

        var sids = GetListSids();

        BannedPlayer? mutedPlayer = null;

        string adminName = controller == null ? "CONSOLE" : controller.PlayerName;
        string adminSid = controller == null ? "CONSOLE" : controller.SteamID.ToString();
        string ip = target != null ? target.IpAddress : "Undefined";
        bool offline = target == null;
        Task.Run(async () =>
        {
            if (await bm.IsPlayerMutedAsync(sid, Config) == false)
            {
                Console.WriteLine("[Iks_Admin] Player not muted!");
                return;
            }
            mutedPlayer = await bm.GetPlayerMute(sid, Config);
            if (mutedPlayer != null)
            {
                Server.NextFrame(() =>
                {
                    PrintUnMuteMessage(mutedPlayer.Name, adminName);
                });
            }

            await bm.UnMutePlayer(sid, adminSid, Config);
            Console.WriteLine("[Iks_Admin] Player unmuted!");
            if (Config.LogToVk)
            {
                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnMuteMessage"], mutedPlayer.Name, sid, adminName, ip, offline);
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
                .Replace("{admin}", adminName)}");
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
                .Replace("{duration}", title)}");
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
        if (arg.StartsWith("#"))
        {
            arg = arg.Replace("#", "");
            if (arg.Length < 17)
            {
                int uid;
                if (Int32.TryParse(arg, out uid))
                {
                    foreach (var p in Helper.GetOnlinePlayers())
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
            }

            if (arg.Length == 17)
            {
                ulong sid;
                if (UInt64.TryParse(arg, out sid))
                {
                    if (Utilities.GetPlayerFromSteamId(sid) != null)
                    {
                        player = Utilities.GetPlayerFromSteamId(sid);
                        return player;
                    }
                }
            }
        }
        if (!arg.StartsWith("#"))
            return Helper.GetOnlinePlayers().FirstOrDefault(u => u.PlayerName.Contains(arg));
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
                        .Replace("{unbannedBy}", UbanAdminName)}");
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
        foreach (Match match in matches)
        {
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

        MutedSids = await bm.GetMutedPlayers(sids, Config);

        Server.NextFrame(() =>
        {
            SetMuteForPlayers();
        });

    }

    public void SetMuteForPlayers()
    {
        foreach (var p in Helper.GetOnlinePlayers())
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
        foreach (var p in Helper.GetOnlinePlayers())
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

        GaggedSids = await bm.GetGaggedPlayers(sids, Config);

        Server.NextFrame(() =>
        {
            UpdateChatColorsGagged();
        });

    }
    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
    public void ReloadAdmins(string? sid)
    {
        Console.WriteLine("[Iks_Admin] Admins reloaded!");
        AdminManager am = new AdminManager(_dbConnectionString);
        string server_id = Config.ServerId;
        Task.Run(async () =>
        {
            admins = await am.GetAllAdmins(server_id);
            Server.NextFrame(() =>
            {
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
            if (controller != null)
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

        // Устанавливаем CSS
        foreach (var admin in admins)
        {
            SteamID asid = new SteamID(UInt64.Parse(admin.SteamId));
            // Устанавливаем иммунитет CSS и группы
            cssAdminManager.AdminManager.SetPlayerImmunity(asid, (uint)admin.Immunity);
            cssAdminManager.AdminManager.AddPlayerToGroup(asid, $"#css/{admin.GroupName}");


            foreach (var Flag in Config.ConvertedFlags)
            {
                if (admin.Flags.Contains(Flag.Key))
                {
                    Console.WriteLine(Flag.Key);
                    SteamID steamID = new SteamID(UInt64.Parse(admin.SteamId));
                    cssAdminManager.AdminManager.AddPlayerPermissions(steamID, Flag.Value.ToArray());
                }
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
                        if (ActionTargets[i].IsValid == false || ActionTargets[i] == null)
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            break;
                        }
                        NativeAPI.IssueServerCommand($"kickid {ActionTargets[i].UserId}");
                        string name = ActionTargets[i].PlayerName;
                        string sid = ActionTargets[i].SteamID.ToString();
                        string ip = ActionTargets[i].IpAddress;
                        string adminName = OwnReasonsTyper[i].PlayerName;
                        string reason = info.GetArg(1).Replace("!", "");
                        Task.Run(async () =>
                        {
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["KickMessage"], name, sid, ip, adminName, reason, 0, false);
                            }
                        });
                        foreach (var str in Localizer["KickMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargets[i].PlayerName)
                                .Replace("{admin}", controller.PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))}");
                        }
                        break;
                    case "banTime":
                        if (ActionTargets[i].IsValid == false || ActionTargets[i] == null)
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            break;
                        }
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
                        if (ActionTargets[i].IsValid == false || ActionTargets[i] == null)
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            break;
                        }
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
                        if (ActionTargets[i].IsValid == false || ActionTargets[i] == null)
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            break;
                        }
                        title = $"{ActionTimes[i]}{Localizer["min"]}";
                        if (ActionTimes[i] == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        BanManager bm = new BanManager(_dbConnectionString);

                        name = ActionTargets[i].PlayerName;
                        sid = ActionTargets[i].SteamID.ToString();
                        ip = ActionTargets[i].IpAddress;
                        string adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        int Btime = ActionTimes[i];
                        reason = info.GetArg(1);
                        string AdminName = OwnReasonsTyper[i].PlayerName;
                        Task.Run(async () =>
                        {
                            await bm.BanPlayer(name, sid, ip, adminsid, Btime, reason, Config);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, Btime, false);
                            }
                        });
                        NativeAPI.IssueServerCommand($"kickid {ActionTargets[i].UserId}");

                        foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargets[i].PlayerName)
                                .Replace("{admin}", OwnReasonsTyper[i].PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))
                                .Replace("{duration}", title)}");
                        }

                        break;
                    case "banReasonDisconnected":
                        title = $"{ActionTimes[i]}{Localizer["min"]}";
                        if (ActionTimes[i] == 0)
                        {
                            title = Localizer["Options.Infinity"].ToString();
                        }
                        bm = new BanManager(_dbConnectionString);

                        name = ActionTargetsDisconnected[i].Name;
                        sid = ActionTargetsDisconnected[i].Sid;
                        ip = ActionTargetsDisconnected[i].Ip;
                        adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        Btime = ActionTimes[i];
                        reason = info.GetArg(1).Replace("!", "");
                        AdminName = OwnReasonsTyper[i].PlayerName;

                        Task.Run(async () =>
                        {
                            await bm.BanPlayer(name, sid, ip, adminsid, Btime, reason, Config);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, Btime, true);
                            }
                        });

                        foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargetsDisconnected[i].Name)
                                .Replace("{admin}", OwnReasonsTyper[i].PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))
                                .Replace("{duration}", title)}");
                        }

                        break;
                    case "muteTime":
                        if (ActionTargets[i].IsValid == false || ActionTargets[i] == null)
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            break;
                        }
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
                        if (ActionTargets[i].IsValid == false || ActionTargets[i] == null)
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            break;
                        }
                        title = $"{ActionTimes[i]}{Localizer["min"]}";
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
                        AdminName = OwnReasonsTyper[i].PlayerName;
                        ip = ActionTargets[i].IpAddress;



                        Task.Run(async () =>
                        {
                            await bm.MutePlayer(name, sid, adminsid, Btime, reason, Config);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["MuteMessage"], name, sid, ip, AdminName, reason, Btime, false);
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
                                .Replace("{duration}", title)}");
                        }

                        break;
                    case "gagTime":
                        if (ActionTargets[i].IsValid == false || ActionTargets[i] == null)
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            break;
                        }
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
                        if (ActionTargets[i].IsValid == false || ActionTargets[i] == null)
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            break;
                        }
                        title = $"{ActionTimes[i]}{Localizer["min"]}";
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
                        AdminName = OwnReasonsTyper[i].PlayerName;
                        ip = ActionTargets[i].IpAddress;
                        Task.Run(async () =>
                        {
                            await bm.GagPlayer(name, sid, adminsid, Btime, reason, Config);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["GagMessage"], name, sid, ip, AdminName, reason, Btime, false);
                            }
                        });
                        List<string> gagCheckSids = GetListSids();
                        Task.Run(async () =>
                        {
                            await SetGaggedPlayers(gagCheckSids);
                            Server.NextFrame(() =>
                            {
                                UpdateChatColorsGagged();
                            });
                        });



                        foreach (var str in Localizer["GagMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", ActionTargets[i].PlayerName)
                                .Replace("{admin}", OwnReasonsTyper[i].PlayerName)
                                .Replace("{reason}", info.GetArg(1).Replace("!", ""))
                                .Replace("{duration}", title)}");
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
        if (@event.Userid.IsBot || !@event.Userid.IsValid || @event.Userid == null) return HookResult.Continue;
        int? uid = @event.Userid.UserId;
        string sid = @event.Userid.SteamID.ToString();
        string ip = @event.Userid.IpAddress;
        Task.Run(async () =>
        {
            if (await bm.IsPlayerBannedAsync(sid, Config))
            {
                Server.NextFrame(() =>
                {
                    Kick(uid);
                });
            }
            if (await bm.IsPlayerBannedAsync(ip, Config))
            {
                Server.NextFrame(() =>
                {
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
        // Установка флагов CSS

        Admin? admin = GetAdminBySid(controller.SteamID.ToString());
        if (admin != null)
        {
            foreach (var Flag in Config.ConvertedFlags)
            {
                if (admin.Flags.Contains(Flag.Key))
                {
                    Console.WriteLine(Flag.Key);
                    cssAdminManager.AdminManager.AddPlayerPermissions(controller, Flag.Value.ToArray());
                }
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
        List<string> sids = GetListSids();

        Task.Run(async () =>
        {
            await SetGaggedPlayers(sids);
        });
        Task.Run(async () =>
        {
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
        if (admin.Flags.Contains("z") || admin.Flags.Contains("s") || admin.Flags.Contains("t"))
        {
            menu.AddMenuOption($" {Localizer["Options.Others"]}", (controller, option) =>
            {
                ChatMenus.OpenMenu(controller, OthersMenuConstructor(controller));
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

        foreach (var player in Helper.GetOnlinePlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid || player == null) continue;
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
                        string ip = player.IpAddress;
                        Task.Run(async () =>
                        {
                            if (Config.LogToVk)
                            {
                                await vkLog.sendPunMessage(Config.LogToVkMessages["KickMessage"], name, sid, ip, adminName, reason, 0, false);
                            }
                        });
                        foreach (var str in Localizer["KickMessage"].ToString().Split("\n"))
                        {
                            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                                .Replace("{name}", player.PlayerName)
                                .Replace("{admin}", controller.PlayerName)
                                .Replace("{reason}", reason)}");
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

        foreach (var player in Helper.GetOnlinePlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid || player == null) continue;
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

        foreach (var player in DisconnectedPlayers)
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

    public ChatMenu BanMenuDisconnectedPlayersReasonsConstructor(CCSPlayerController ccsPlayerController, DisconnectedPlayer player, int time, string title)
    {
        ChatMenu BanMenuReasons =
            new ChatMenu($" {Localizer["PluginTag"]} {Localizer["BanMenuReasonsTitle"]}");
        foreach (var reason in Config.BanReasons)
        {
            BanMenuReasons.AddMenuOption($"{reason}", (playerController, menuOption) =>
            {
                BanManager bm = new BanManager(_dbConnectionString);

                string adminsid = ccsPlayerController.SteamID.ToString();
                string AdminName = ccsPlayerController.PlayerName;
                Task.Run(async () =>
                {
                    await bm.BanPlayer(player.Name, player.Sid, player.Ip, adminsid, time, reason, Config);
                    if (Config.LogToVk)
                    {
                        await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], player.Name, player.Sid, player.Ip, AdminName, reason, time, true);
                    }
                });

                foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                {
                    Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                        .Replace("{name}", player.Name)
                        .Replace("{admin}", ccsPlayerController.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{duration}", title)}");
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
                    Actions[i] = "banReasonDisconnected";
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
                if (player.IsValid == false || player == null || player == null)
                {
                    playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                    return;
                }

                string name = player.PlayerName;
                string sid = player.SteamID.ToString();
                string adminsid = ccsPlayerController.SteamID.ToString();
                string AdminName = ccsPlayerController.PlayerName;

                string? ip = player.IpAddress;

                Task.Run(async () =>
                {
                    await bm.BanPlayer(name, sid, ip, adminsid, time, reason, Config);
                    if (Config.LogToVk)
                    {
                        await vkLog.sendPunMessage(Config.LogToVkMessages["BanMessage"], name, sid, ip, AdminName, reason, time, false);
                    }
                });
                NativeAPI.IssueServerCommand($"kickid {player.UserId}");

                foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                {
                    Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                        .Replace("{name}", player.PlayerName)
                        .Replace("{admin}", ccsPlayerController.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{duration}", title)}");
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

        foreach (var player in Helper.GetOnlinePlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid || player == null) continue;
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
                if (player.IsValid == false || player == null)
                {
                    playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                    return;
                }
                if (MutedSids.Contains(player.SteamID.ToString()))
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
                    await bm.MutePlayer(name, sid, adminsid, time, reason, Config);
                    if (Config.LogToVk)
                    {
                        await vkLog.sendPunMessage(Config.LogToVkMessages["MuteMessage"], name, sid, ip, AdminName, reason, time, false);
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
                        .Replace("{duration}", title)}");
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


        foreach (var player in Helper.GetOnlinePlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid || player == null) continue;
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
                if (player.IsValid == false || player == null)
                {
                    playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                    return;
                }
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
                    await bm.GagPlayer(name, sid, adminsid, time, reason, Config);
                    if (Config.LogToVk)
                    {
                        await vkLog.sendPunMessage(Config.LogToVkMessages["GagMessage"], name, sid, ip, AdminName, reason, time, false);
                    }
                });
                List<string> gagCheckSids = GetListSids();
                Task.Run(async () =>
                {
                    await SetGaggedPlayers(gagCheckSids);
                    Server.NextFrame(() =>
                    {
                        UpdateChatColorsGagged();
                    });
                });


                foreach (var str in Localizer["GagMessage"].ToString().Split("\n"))
                {
                    Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                        .Replace("{name}", player.PlayerName)
                        .Replace("{admin}", ccsPlayerController.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{duration}", title)}");
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

        foreach (var player in Helper.GetOnlinePlayers())
        {
            if (player.IsBot) continue;
            if (!player.IsValid || player == null) continue;
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
                        if (player.IsValid == false || player == null)
                        {
                            playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            return;
                        }
                        BanManager bm = new BanManager(_dbConnectionString);
                        string adminName = playerController.PlayerName;
                        Task.Run(async () =>
                        {
                            await bm.UnGagPlayer(player.SteamID.ToString(), playerController.SteamID.ToString(), Config);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnGagMessage"], player.PlayerName, player.SteamID.ToString(), adminName, player.IpAddress, false);
                            }
                        });
                        List<string> gagCheckSids = GetListSids();
                        Task.Run(async () =>
                        {
                            await SetGaggedPlayers(gagCheckSids);
                            Server.NextFrame(() =>
                            {
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
                        if (player.IsValid == false || player == null)
                        {
                            playerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerNotFound"]}");
                            return;
                        }
                        BanManager bm = new BanManager(_dbConnectionString);
                        string adminName = playerController.PlayerName;
                        string sid = playerController.SteamID.ToString();
                        string adminSid = player.SteamID.ToString();
                        Task.Run(async () =>
                        {
                            await bm.UnMutePlayer(adminSid, sid, Config);
                            if (Config.LogToVk)
                            {
                                await vkLog.sendUnPunMessage(Config.LogToVkMessages["UnMuteMessage"], player.PlayerName, player.SteamID.ToString(), adminName, player.IpAddress, false);
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


    public ChatMenu OthersMenuConstructor(CCSPlayerController activator)
    {
        ChatMenu OthersMenu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["Options.Others"]}");

        OthersMenu.AddMenuOption($" {Localizer["Options.Back"]}", (activator, option) =>
        {
            ChatMenus.OpenMenu(activator, AdminMenuConstructor(GetAdminBySid(activator.SteamID.ToString())));
        });
        // Закрыть меню
        OthersMenu.AddMenuOption($" {Localizer["Options.Exit"]}", (activator, option) =>
        {
            activator.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });


        if (Helper.AdminHaveFlag(activator.SteamID.ToString(), "s", admins))
        {
            OthersMenu.AddMenuOption(Localizer["Options.Slay"], (controller, info) =>
            {
                ChatMenus.OpenMenu(activator, SlayMenu(activator));
                return;
            });
        }


        return OthersMenu;
    }


    public ChatMenu SlayMenu(CCSPlayerController activator)
    {
        ChatMenu SlayMenu = new ChatMenu($" {Localizer["PluginTag"]} {Localizer["Options.Others"]}");

        SlayMenu.AddMenuOption($" {Localizer["Options.Back"]}", (activator, option) =>
        {
            ChatMenus.OpenMenu(activator, OthersMenuConstructor(activator));
        });
        // Закрыть меню
        SlayMenu.AddMenuOption($" {Localizer["Options.Exit"]}", (activator, option) =>
        {
            activator.PrintToChat($" {Localizer["PluginTag"]} {Localizer["Options.ExitMessage"]}");
        });



        foreach (var p in Helper.GetOnlinePlayers())
        {
            if (p.IsBot || !p.IsValid || p == null) continue;
            if (p != activator)
            {
                if (Helper.AdminHaveBiggerImmunity(activator.SteamID.ToString(), p.SteamID.ToString(), admins))
                {
                    continue;
                }
            }


            SlayMenu.AddMenuOption(p.PlayerName, (controller, info) =>
            {
                SlayFunc(p, activator);
            });

        }


        return SlayMenu;
    }
    public void SlayFunc(CCSPlayerController target, CCSPlayerController? admin)
    {
        string adminName = "CONSOLE";
        if (admin != null)
        {
            adminName = admin.PlayerName;
        }
        target.CommitSuicide(true, true);

        Server.PrintToChatAll($" {Localizer["PluginTag"]} {Localizer["SlayMessage"].ToString()
        .Replace("{name}", target.PlayerName)
        .Replace("{admin}", adminName)}");

        string name = target.PlayerName;
        string sid = target.SteamID.ToString();
        string ip = target.IpAddress;

        if (Config.LogToVk)
        {
            Task.Run(async () =>
            {
                await vkLog.sendPunMessage(Config.LogToDiscordMessages["SlayMessage"], name, sid, ip, adminName, "Undefined", 0, false);
            });
        }

        Console.Write($"[Iks_Admin] {target.PlayerName} was killed by the admin {adminName}");
    }







    #endregion

}