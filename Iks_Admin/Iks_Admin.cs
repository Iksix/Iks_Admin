using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;

namespace Iks_Admin;

// Создание БД админов
// CREATE TABLE `u2194959_FallowCS2`.`iks_admins` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `name` VARCHAR(32) NOT NULL , `flags` VARCHAR(32) NOT NULL , `immunity` INT NOT NULL , PRIMARY KEY (`id`), UNIQUE (`sid`), UNIQUE (`name`)) ENGINE = InnoDB;


public class Iks_Admin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName { get; } = "Iks_Admin";
    public override string ModuleVersion { get; } = "1.0.0";
    public override string ModuleAuthor { get; } = "iks";
    private string _dbConnectionString = "";

    private List<Admin> admins = new List<Admin>();

    public PluginConfig Config { get; set; }

    public void OnConfigParsed(PluginConfig config)
    {
        config = ConfigManager.Load<PluginConfig>(ModuleName);
        
        _dbConnectionString = "Server=" + config.Host + ";Database=" + config.Name
                              + ";port=" + config.Port + ";User Id=" + config.Login + ";password=" + config.Password;
        
        string sql =
            "CREATE TABLE IF NOT EXISTS `iks_admins` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `name` VARCHAR(32) NOT NULL , `flags` VARCHAR(32) NOT NULL , `immunity` INT NOT NULL, `end` INT NOT NULL , PRIMARY KEY (`id`), UNIQUE (`sid`), UNIQUE (`name`)) ENGINE = InnoDB;";
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

        sql = "CREATE TABLE IF NOT EXISTS `iks_bans` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB;";
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
        
        sql = "CREATE TABLE IF NOT EXISTS `iks_mutes` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB;";
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
        
        sql = "CREATE TABLE IF NOT EXISTS `iks_gags` ( `id` INT NOT NULL AUTO_INCREMENT , `sid` VARCHAR(32) NOT NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB;";
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
        
        ReloadAdmins();
        
        Config = config;
    }
    
    // COMMANDS
    [ConsoleCommand("css_reload_admins")]
    public void OnReloadAdminsCommand(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
        { 
            ReloadAdmins();  
            return;
        }

        Admin? admin = GetAdminBySid(controller.SteamID.ToString());

        if (admin == null || !admin.Flags.Contains("z"))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
            return;
        }
        
        ReloadAdmins();
        foreach (var str in Localizer["reload_admins"].ToString().Split("\n"))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {str}");
        }

        foreach (var a in admins)
        {
            controller.PrintToChat("=============");
            controller.PrintToChat($"Имя: {ChatColors.DarkBlue}{admin.Name}");
            controller.PrintToChat($"Стим айди: {ChatColors.DarkBlue}{admin.SteamId}");
            controller.PrintToChat($"Иммунитет: {ChatColors.DarkBlue}{admin.Immunity}");
            controller.PrintToChat($"Флаги: {ChatColors.DarkBlue}{admin.Flags}");
            controller.PrintToChat("=============");
        }
    }
    
    
    // FUNC
    public void ReloadAdmins()
    {
        AdminManager am = new AdminManager(_dbConnectionString);
        admins = am.ReloadAdmins();
        
        Console.WriteLine("[Iks_Admin] Admins reloaded!");
        Console.WriteLine("[Iks_Admin] Admins list:");
        foreach (var admin in admins)
        {
            Console.WriteLine("=============");
            Console.WriteLine(admin.Name);
            Console.WriteLine(admin.SteamId);
            Console.WriteLine(admin.Flags);
            Console.WriteLine(admin.Immunity);
            Console.WriteLine(admin.End);
            Console.WriteLine("=============");
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

}