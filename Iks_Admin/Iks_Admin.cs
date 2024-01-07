﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Menu;
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
    
    
    public CCSPlayerController?[] OwnReasonsTyper = new CCSPlayerController[64];
    public string?[] Actions = new String[64];
    public CCSPlayerController?[] ActionTargets = new CCSPlayerController[64];
    public int[] ActionTimes = new Int32[64];

    private List<string> GaggedSids = new List<string>();
    private List<string> MutedSids = new List<string>();

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

        sql = "CREATE TABLE IF NOT EXISTS `iks_bans` ( `id` INT NOT NULL AUTO_INCREMENT , `name` VARCHAR(32) NOT NULL ,`sid` VARCHAR(32) NOT NULL, `ip` VARCHAR(32) NULL , `adminsid` VARCHAR(32) NOT NULL , `created` INT NOT NULL , `time` INT NOT NULL , `end` INT NOT NULL , `reason` VARCHAR(255) NOT NULL, `Unbanned` INT(1) NOT NULL DEFAULT '0', `UnbannedBy` VARCHAR(32) NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB;";
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
        
        ReloadAdmins();
        
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSay);
    }

    // COMMANDS
    [ConsoleCommand("css_reload_admins")]
    public void OnReloadAdminsCommand(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
        { 
            OnConfigParsed(Config); 
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
    
    [ConsoleCommand("css_ban", "css_ban uid/sid duration reason")]
    public void OnBanCommand(CCSPlayerController? controller, CommandInfo info)
    {
        if (info.GetArg(1).Trim() == "")
        {
            return;
        }
        bool canBan = false;
        if (controller == null)
        {
            canBan = true;
        }
        Admin? admin = null;
        if (controller != null)
        {
            admin = GetAdminBySid(controller.SteamID.ToString());
        }
        if (admin == null && canBan == false)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
            return;
        }

        if (controller != null)
        {
            if (admin.Flags.Contains("z") || admin.Flags.Contains("b"))
            {
                canBan = true;
            }
        }

        if (!canBan)
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
            return;
        }
        
        CCSPlayerController? target = null;

        if (info.GetArg(1).Length < 17)
        {
            target = Utilities.GetPlayerFromUserid(Int32.Parse(info.GetArg(1)));
        }
        else
        {
            target = Utilities.GetPlayerFromSteamId(UInt64.Parse(info.GetArg(1)));
        }
        
        if (target == null && info.GetArg(1).Length < 17)
        {
            return;
        }
        BanManager bm = new BanManager(_dbConnectionString);

        if (target == null && info.GetArg(1).Length >= 17)
        {
            string dname = "UNDEFINED";
            string dsid = info.GetArg(1);
            string dip = "UNDEFINED";
            string dadminsid = controller == null ? "Console" : controller.SteamID.ToString();
            int dtime = Int32.Parse(info.GetArg(2));
            string dreason = info.GetArg(3);

            if (bm.IsPlayerBanned(dsid))
            {
                Console.WriteLine($"[IKS_ADMIN] PlayerSid: {dsid} ALREDY banned");
                return;
            }
            
            Task.Run(async () =>
            {
                await bm.BanPlayer(dname, dsid, dip, dadminsid, dtime, dreason);
            });
            Console.WriteLine($"[IKS_ADMIN] PlayerSid: {dsid} was banned");
            return;
        }

        string name = target.PlayerName;
        string sid = target.SteamID.ToString();
        string? ip = target.IpAddress;
        
        string adminsid = controller == null ? "Console" : controller.SteamID.ToString();
        int time = Int32.Parse(info.GetArg(2));
        string reason = info.GetArg(3);
        string adminName = controller == null ? "Console" : controller.PlayerName;
        if (bm.IsPlayerBanned(sid))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
            return;
        }
        Task.Run(async () =>
        {
            await bm.BanPlayer(name, sid, ip, adminsid, time, reason);
        });
        NativeAPI.IssueServerCommand($"kickid {target.UserId}");
        string title = $"{time}{Localizer["min"]}";
        if (time == 0)
        {
            title = $" {Localizer["Options.Infinity"]}";
        }
        foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
        {
            Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                .Replace("{name}", name)
                .Replace("{admin}", adminName)
                .Replace("{reason}", reason)
                .Replace("{duration}", title)
            }");
        }
    }

    [ConsoleCommand("css_unban", "css_ban sid")]
    public void OnUnBanCommand(CCSPlayerController? controller, CommandInfo info)
    {
        BanManager bm = new BanManager(_dbConnectionString);
        // Unban if CONSOLE
        string sid = info.GetArg(1);

        if (controller == null)
        {
            Task.Run(async () =>
            {
                await bm.UnBanPlayer(sid, "Console");
            });
            Console.WriteLine($"[IKS_ADMIN] Player: {info.GetArg(1)} was unbanned");
            return;
        }

        Admin? admin = GetAdminBySid(controller.SteamID.ToString());
        if (admin == null || !(admin.Flags.Contains("z") || admin.Flags.Contains("u")))
        {
            controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["HaveNotAccess"]}");
            return;
        }
        Task.Run(async () =>
        {
            await bm.UnBanPlayer(sid, "Console");
        });
        controller.PrintToChat($" {Localizer["PluginTag"]} {Localizer["UnBanMessage"]}");
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

        for (int i = 0; i < OwnReasonsTyper.Length; i++)
        {
            OwnReasonsTyper[i] = null;
            Actions[i] = null;
            ActionTargets[i] = null;
            ActionTimes[i] = 0;
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
            return HookResult.Continue;;
        }
        
        if (GaggedSids.Contains(controller.SteamID.ToString()))
        {
            return HookResult.Stop;
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

                        string name = ActionTargets[i].PlayerName;
                        string sid = ActionTargets[i].SteamID.ToString();
                        string? ip = ActionTargets[i].IpAddress;
                        string adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        int Btime = ActionTimes[i];
                        string reason = info.GetArg(1).Replace("!", "");
                
                        Task.Run(async () =>
                        {
                            await bm.BanPlayer(name, sid, ip, adminsid, Btime, reason);
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
                        if (bm.IsPlayerMuted(ActionTargets[i].SteamID.ToString()))
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                            break;
                        }

                        name = ActionTargets[i].PlayerName;
                        sid = ActionTargets[i].SteamID.ToString();
                        adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        Btime = ActionTimes[i];
                        reason = info.GetArg(1).Replace("!", "");
                
                        Task.Run(async () =>
                        {
                            await bm.MutePlayer(name, sid, adminsid, Btime, reason);
                            Server.NextFrame(() =>
                            {
                                SetMutedPlayers();
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
                        if (bm.IsPlayerGagged(ActionTargets[i].SteamID.ToString()))
                        {
                            OwnReasonsTyper[i].PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                            break;
                        }

                        name = ActionTargets[i].PlayerName;
                        sid = ActionTargets[i].SteamID.ToString();
                        adminsid = OwnReasonsTyper[i].SteamID.ToString();
                        Btime = ActionTimes[i];
                        reason = info.GetArg(1).Replace("!", "");
                
                        Task.Run(async () =>
                        {
                            await bm.GagPlayer(name, sid, adminsid, Btime, reason);
                            Server.NextFrame(() =>
                            {
                                SetGaggedPlayers();
                            });
                        });
                        NativeAPI.IssueServerCommand("css_chatcolors_setgaggedplayers");


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
                ActionTimes[i] = 0;
                return HookResult.Stop;
            }
        }
        return HookResult.Continue;
    }
    
    //Events

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        SetMutedPlayers();
        BanManager bm = new BanManager(_dbConnectionString);
        if (bm.IsPlayerBanned(@event.Userid.SteamID.ToString()))
        {
            NativeAPI.IssueServerCommand($"kickid {@event.Userid.UserId}");
        }
        return HookResult.Continue;
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
            }
        }
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        SetGaggedPlayers();
        SetMutedPlayers();
        return HookResult.Continue;
    }
    
    public void SetMutedPlayers()
    {
        MutedSids.Clear();
        BanManager bm = new BanManager(_dbConnectionString);
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsBot) continue;
            if (p.IsValid)
            {
                Console.WriteLine($"Check sid: {p.SteamID.ToString()} is mutted");

                if (bm.IsPlayerMuted(p.SteamID.ToString()))
                {
                    MutedSids.Add(p.SteamID.ToString());
                    p.VoiceFlags = VoiceFlags.Muted;
                    Console.WriteLine($"{p.PlayerName} {p.VoiceFlags}");
                    continue;
                }
                p.VoiceFlags = VoiceFlags.Normal;

            }
        }
    }
    public void SetGaggedPlayers()
    {
        GaggedSids.Clear();
        BanManager bm = new BanManager(_dbConnectionString);
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsBot) return;
            if (!p.IsValid) return;

            if (bm.IsPlayerGagged(p.SteamID.ToString()))
            {
                GaggedSids.Add(p.SteamID.ToString());
            } 
        }
        Console.WriteLine("Gag Players setted");
        
    }

    // Menu constructors
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
                string? ip = player.IpAddress;
                
                Task.Run(async () =>
                {
                    await bm.BanPlayer(name, sid, ip, adminsid, time, reason);
                });
                NativeAPI.IssueServerCommand($"kickid {player.UserId}");

                foreach (var str in Localizer["BanMessage"].ToString().Split("\n"))
                {
                    Server.PrintToChatAll($" {Localizer["PluginTag"]} {str
                        .Replace("{name}", player.PlayerName)
                        .Replace("{admin}", ccsPlayerController.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{time}", title)
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
                if (bm.IsPlayerBanned(player.SteamID.ToString()))
                {
                    ccsPlayerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                    return;
                }

                string name = player.PlayerName;
                string sid = player.SteamID.ToString();
                string adminsid = ccsPlayerController.SteamID.ToString();
                
                Task.Run(async () =>
                {
                    await bm.MutePlayer(name, sid, adminsid, time, reason);
                    Server.NextFrame(() =>
                    {
                        SetMutedPlayers();
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
                if (bm.IsPlayerBanned(player.SteamID.ToString()))
                {
                    ccsPlayerController.PrintToChat($" {Localizer["PluginTag"]} {Localizer["PlayerAlredyBanned"]}");
                    return;
                }

                string name = player.PlayerName;
                string sid = player.SteamID.ToString();
                string adminsid = ccsPlayerController.SteamID.ToString();
                
                Task.Run(async () =>
                {
                    await bm.GagPlayer(name, sid, adminsid, time, reason);
                    Server.NextFrame(() =>
                    {
                        SetGaggedPlayers();
                    });

                });
                
                NativeAPI.IssueServerCommand("css_chatcolors_setgaggedplayers");

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
            if (bm.IsPlayerGagged(player.SteamID.ToString()))
            {
                playerGagged = true;
            }
            if (bm.IsPlayerMuted(player.SteamID.ToString()))
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
                        Task.Run(async () =>
                        {
                            await bm.UnGagPlayer(player.SteamID.ToString(), playerController.SteamID.ToString());
                            Server.NextFrame(() =>
                            {
                                SetGaggedPlayers();
                            });

                        });
                        NativeAPI.IssueServerCommand("css_chatcolors_setgaggedplayers");

                        Server.PrintToChatAll($" {Localizer["PluginTag"]} {Localizer["UnGagMessage"].ToString()
                            .Replace("{name}", player.PlayerName)
                            .Replace("{admin}", activator.PlayerName)}");
                    });
                }
                if (playerMuted)
                {
                    playerMenu.AddMenuOption($" {Localizer["Options.UnMute"]}", (playerController, menuOption) =>
                    {
                        BanManager bm = new BanManager(_dbConnectionString);
                        Task.Run(async () =>
                        {
                            await bm.UnMutePlayer(player.SteamID.ToString(), playerController.SteamID.ToString());
                            Server.NextFrame(() =>
                            {
                                SetMutedPlayers();
                            });
                        });
                        Server.PrintToChatAll($" {Localizer["PluginTag"]} {Localizer["UnMuteMessage"].ToString()
                            .Replace("{name}", player.PlayerName)
                            .Replace("{admin}", activator.PlayerName)}");
                    });
                }

                ChatMenus.OpenMenu(activator, playerMenu);
            });


        }

        return UnMenu;
    }


}