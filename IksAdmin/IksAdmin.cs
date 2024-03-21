using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using IksAdmin.Commands;
using IksAdmin.Menus;
using IksAdminApi;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Group = IksAdminApi.Group;
namespace IksAdmin;

public class IksAdmin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "IksAdmin";
    public override string ModuleVersion => "2.0.0";
    public override string ModuleAuthor => "iks__";

    public static IIksAdminApi? Api;
    private static string _dbConnectionString = "";
    private readonly PluginCapability<IIksAdminApi> _pluginCapability  = new("iksadmin:core");
    public PluginConfig Config { get; set; } = new();
    public static PluginConfig ConfigNow = new();
    private static BasePlugin _plugin;

    private static readonly Dictionary<SteamID, List<string>> ConvertedFlags = new();
    private static readonly Dictionary<SteamID, string> ConvertedGroups = new();
    private static readonly Dictionary<SteamID, uint> ConvertedImmunity = new();
    
    public void OnConfigParsed(PluginConfig config)
    {
        _dbConnectionString = "Server=" + config.Host + ";Database=" + config.Database
                             + ";port=" + config.Port + ";User Id=" + config.User + ";password=" + config.Password;
        Config = config;
        ConfigNow = config;
    }

    public override void Load(bool hotReload)
    {
        _plugin = this;
        Api = new PluginApi(this, Config, Localizer, _dbConnectionString);
        Capabilities.RegisterPluginCapability(_pluginCapability, () => Api);
        BaseCommands.Config = Config;
        InitializeCommands();
        InitializeMessages();
        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSay);
        AddCommandListener("jointeam", (p, _) =>
        {
            if (BaseCommands.HidenPlayers.Contains(p!))
            {
                BaseCommands.HidenPlayers.Remove(p!);
            }
            return HookResult.Continue;
        });

        AddTimer(3, () =>
        {
            var allAdmins = Api.AllAdmins;
            foreach (var admin in allAdmins)
            {
                if (admin.End != 0 && admin.End < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    Task.Run(async () =>
                    {
                        await Api.DelAdmin(admin.SteamId);
                    });
                }
            }
            var players = XHelper.GetOnlinePlayers();
            foreach (var p in players)
            {
                if (!XHelper.IsControllerValid(p)) continue;
                UpdatePlayerComms(p.AuthorizedSteamID!.SteamId64.ToString());
            }
        }, TimerFlags.REPEAT);
    }

    private void InitializeMessages()
    {
        Api!.OnAddGag += comm =>
        {
            Api.SendMessageToAll(ReplaceComm(Localizer["SERVER_OnGag"], comm));
        };
        Api.OnAddMute += comm =>
        {
            Api.SendMessageToAll(ReplaceComm(Localizer["SERVER_OnMute"], comm));
        };
        Api.OnAddBan += ban =>
        {
            Api.SendMessageToAll(ReplaceBan(Localizer["SERVER_OnBan"], ban));
        };
        Api.OnUnGag += (comm, adminsSid) =>
        {
            comm.UnbannedBy = adminsSid;
            Api.SendMessageToAll(ReplaceComm(Localizer["SERVER_OnUnGag"], comm, adminsSid));
        };
        Api.OnUnMute += (comm, adminsSid) =>
        {
            comm.UnbannedBy = adminsSid;
            Api.SendMessageToAll(ReplaceComm(Localizer["SERVER_OnUnMute"], comm, adminsSid));
        };
        Api.OnUnBan += (ban, adminsSid) =>
        {
            ban.UnbannedBy = adminsSid;
            Api.SendMessageToAll(ReplaceBan(Localizer["SERVER_OnUnBan"], ban, adminsSid));
        };
        Api.OnKick += (adminSid, target, reason) =>
        {
            Api.SendMessageToAll(Localizer["SERVER_OnKick"].Value
                .Replace("{name}", target.PlayerName)
                .Replace("{sid}", target.SteamId.SteamId64.ToString())
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{reason}", reason)
            );
        };
        Api.OnSlay += (adminSid, target) =>
        {
            Api.SendMessageToAll(Localizer["SERVER_OnSlay"].Value
                .Replace("{name}", target.PlayerName)
                .Replace("{sid}", target.SteamId.SteamId64.ToString())
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
            );
        };
        Api.OnSwitchTeam += (adminSid, target, oldTeam, newTeam) =>
        {
            Api.SendMessageToAll(Localizer["SERVER_SwitchTeam"].Value
                .Replace("{oldTeam}", XHelper.GetStringFromTeam(oldTeam))
                .Replace("{newTeam}",  XHelper.GetStringFromTeam(newTeam))
                .Replace("{name}", target.PlayerName)
                .Replace("{sid}", target.SteamId.SteamId64.ToString())
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
            );
        };
        Api.OnChangeTeam += (adminSid, target, oldTeam, newTeam) =>
        {
            Api.SendMessageToAll(Localizer["SERVER_ChangeTeam"].Value
                .Replace("{oldTeam}", XHelper.GetStringFromTeam(oldTeam))
                .Replace("{newTeam}",  XHelper.GetStringFromTeam(newTeam))
                .Replace("{name}", target.PlayerName)
                .Replace("{sid}", target.SteamId.SteamId64.ToString())
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
            );
        };
        Api.OnRename += (adminSid, target, oldName, newName) =>
        {
            Api.SendMessageToAll(Localizer["SERVER_Rename"].Value
                .Replace("{oldName}", oldName)
                .Replace("{newName}",  newName)
                .Replace("{sid}", target.SteamId.SteamId64.ToString())
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
            );
        };
    }

    private void InitializeCommands()
    {
        Api!.AddNewCommand(
            "reload_infractions", 
            "reload player infractions", 
            "css_reload_infractions <sid>",
            1,
            "adminManage",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.ReloadInfractions
        );
        Api.AddNewCommand(
            "who", 
            "print info about admin in chat", 
            "css_who <#uid/#sid/name>",
            1,
            "who",
            "b",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Who
        );
        Api.AddNewCommand(
            "rcon", 
            "execute command from server", 
            "css_rcon <command>",
            1,
            "rcon",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Rcon
        );
        Api.AddNewCommand(
            "adminadd", 
            "add the admin", 
            "css_adminadd <sid> <name> <flags/-> <immunity> <group_id> <time> <server_id/ - (ALL SERVERS)>",
            6,
            "adminManage",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.AdminAdd
        );
        Api.AddNewCommand(
            "admindel", 
            "delete the admin", 
            "css_admindel <sid>",
            1,
            "adminManage",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.AdminDel
        );
        Api.AddNewCommand(
            "reload_admins", 
            "reload admins", 
            "css_reload_admins",
            0,
            "adminManage",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.ReloadAdmins
        );
        Api.AddNewCommand(
            "admin", 
            "open admin menu", 
            "css_admin",
            0,
            "admin",
            "bkmgstu",
            CommandUsage.CLIENT_ONLY,
            BaseCommands.Admin
        );
        
        Api.AddNewCommand(
            "ban",
            "ban the player",
            "css_ban <#uid/#sid/name> <duration> <reason> <name if needed>",
            3,
            "ban",
            "b",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Ban
            );
        Api.AddNewCommand(
            "gag",
            "gag the player",
            "css_gag <#uid/#sid/name> <duration> <reason> <name if needed>",
            3,
            "gag",
            "g",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Gag
        );
        Api.AddNewCommand(
            "mute",
            "mute the player",
            "css_mute <#uid/#sid/name> <duration> <reason> <name if needed>",
            3,
            "mute",
            "m",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Mute
        );
        Api.AddNewCommand(
            "ungag",
            "ungag the player",
            "css_ungag <#uid/#sid/name>",
            1,
            "ungag",
            "g",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.UnGag
        );
        Api.AddNewCommand(
            "unmute",
            "unmute the player",
            "css_unmute <#uid/#sid/name>",
            1,
            "unmute",
            "m",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.UnMute
        );
        Api.AddNewCommand(
            "unban",
            "unban the player",
            "css_unban <sid>",
            1,
            "unban",
            "u",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.UnBan
        );
        Api.AddNewCommand(
            "kick",
            "kick the player",
            "css_kick <#uid/#sid/name> <reason>",
            2,
            "kick",
            "k",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Kick
        );
        Api.AddNewCommand(
            "slay",
            "slay the player",
            "css_slay <#uid/#sid/name>",
            2,
            "slay",
            "s",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Slay
        );
        Api.AddNewCommand(
            "switchteam",
            "switch the player team",
            "css_switchteam <#uid/#sid/name> <ct/t>",
            2,
            "switchteam",
            "s",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.SwitchTeam
        );
        Api.AddNewCommand(
            "changeteam",
            "change the player team",
            "css_changeteam <#uid/#sid/name> <ct/t/spec>",
            2,
            "changeteam",
            "s",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.ChangeTeam
        );
        Api.AddNewCommand(
            "rename",
            "rename the player",
            "css_rename <#uid/#sid/name> <new name>",
            2,
            "rename",
            "s",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Rename
        );
        Api.AddNewCommand(
            "hide",
            "hide yourself",
            "css_hide",
            0,
            "hide",
            "bkmg",
            CommandUsage.CLIENT_ONLY,
            BaseCommands.Hide
        );
        Api.AddNewCommand(
            "map",
            "set map",
            "css_map <id> <(Workshop Map?) true/false>",
            1,
            "map",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Map
        );

        // Rcon commands
        Api.AddNewCommand(
            "rban",
            "ban from rcon",
            "css_rban <sid> <ip/-(Auto)> <adminSid/CONSOLE> <duration> <reason> <BanType (0 - default / 1 - ip> <name>",
            7,
            "rcon",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.RBan
        );
        Api.AddNewCommand(
            "rgag",
            "gag from rcon",
            "css_rgag <sid> <adminSid/CONSOLE> <duration> <reason> <name>",
            5,
            "rcon",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.RGag
        );
        Api.AddNewCommand(
            "rmute",
            "mute from rcon",
            "css_rmute <sid> <adminSid/CONSOLE> <duration> <reason> <name>",
            5,
            "rcon",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.RMute
        );
        Api.AddNewCommand(
            "rungag",
            "gag from rcon",
            "css_rungag <sid> <adminSid/CONSOLE>",
            2,
            "rcon",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.RUnGag
        );
        Api.AddNewCommand(
            "runban",
            "ban from rcon",
            "css_runban <sid> <adminSid/CONSOLE>",
            2,
            "rcon",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.RUnBan
        );
    }
    private string ReplaceComm(string message, PlayerComm comm, string unbannedBy = "")
    {
        return message
            .Replace("{name}", comm.Name)
            .Replace("{reason}", comm.Reason)
            .Replace("{duration}", GetTime(comm.Time))
            .Replace("{admin}", AdminName(comm.AdminSid))
            .Replace("{adminSid}", comm.AdminSid)
            .Replace("{sid}", comm.Sid)
            .Replace("{unbannedBy}", AdminName(unbannedBy))
            .Replace("{unbannedBySid}", unbannedBy)
            .Replace("{serverId}", comm.ServerId)
            .Replace("{end}", XHelper.GetDateStringFromUtc(comm.End))
            .Replace("{created}", XHelper.GetDateStringFromUtc(comm.Created));
    }
    private string ReplaceBan(string message, PlayerBan ban, string unbannedBy = "")
    {
        return message
            .Replace("{name}", ban.Name)
            .Replace("{reason}", ban.Reason)
            .Replace("{unbannedBy}", AdminName(unbannedBy))
            .Replace("{unbannedBySid}", unbannedBy)
            .Replace("{duration}", GetTime(ban.Time))
            .Replace("{admin}", AdminName(ban.AdminSid))
            .Replace("{adminSid}", ban.AdminSid)
            .Replace("{sid}", ban.Sid)
            .Replace("{ip}", ban.Ip)
            .Replace("{serverId}", ban.ServerId)
            .Replace("{banType}", ban.BanType switch{ 0 => "Normal", 1 => "Ip", _ => "Normal" })
            .Replace("{end}", XHelper.GetDateStringFromUtc(ban.End))
            .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created));
    }

    private string GetTime(int time)
    {
        time = time / 60;
        if (!Config.Times.ContainsValue(time))
            return $"{time}{Localizer["HELPER_Min"]}";
        return Config.Times.First(x => x.Value == time).Key;
    }

    private string AdminName(string adminSid)
    {
        var admin = Api!.ThisServerAdmins.FirstOrDefault(x => x.SteamId == adminSid);
        string adminName = adminSid.ToLower() == "console" ? "CONSOLE" :
            admin == null ? "~Deleted Admin~" : admin.Name;
        return adminName;
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return HookResult.Continue;
        var playerSid = player.AuthorizedSteamID!.SteamId64.ToString();
        var message = info.GetArg(1);

        var existingGag =
            Api!.OnlineGaggedPlayers.FirstOrDefault(x => x.Sid == playerSid);
        if (message.StartsWith("!"))
        {
            var command = message.Remove(0, 1);
            if (Api.NextCommandAction.ContainsKey(player))
            {
                Api.NextCommandAction[player].Invoke(command);
                Api.NextCommandAction.Remove(player);
            }
            if (existingGag != null)
            {
                player.ExecuteClientCommandFromServer(command);
                player.ExecuteClientCommandFromServer("css_" + command);
                player.ExecuteClientCommandFromServer("mm_" + command);
                return HookResult.Stop;
            }
        }
        if (existingGag != null)
        {
            player.PrintToChat($" {Localizer["PluginTag"]} {Localizer["TARGET_ChatWhenGagged"]}");
            return HookResult.Stop;
        }

        if (message.StartsWith("@"))
        {
            var messageToAdmins = message.Remove(0, 1);
            if (messageToAdmins.Trim() == "")
            {
                return HookResult.Stop;
            }
            var existingAdmin = Api.ThisServerAdmins.FirstOrDefault(x => x.SteamId == playerSid);
            var players = XHelper.GetOnlinePlayers();
            if (existingAdmin == null)
            {
                player.PrintToChat(Localizer["OTHER_ToAdmins"].ToString()
                    .Replace("{message}", messageToAdmins)
                    .Replace("{name}", player.PlayerName)
                );
            }
            foreach (var p in players)
            {
                var playerAdmin =
                    Api.ThisServerAdmins.FirstOrDefault(x => x.SteamId == p.AuthorizedSteamID!.SteamId64.ToString());
                if (playerAdmin != null)
                {
                    p.PrintToChat(Localizer["OTHER_AdminsSee"].ToString()
                        .Replace("{name}", player.PlayerName)
                        .Replace("{message}", messageToAdmins)
                    );
                }
            }

            return HookResult.Stop;
        }
        
        return HookResult.Continue;
    }



    [GameEventHandler]
    public HookResult OnPlayerConnected(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!XHelper.IsControllerValid(player)) return HookResult.Continue;
        string sid64 = player.AuthorizedSteamID!.SteamId64.ToString();
        string ip = player.IpAddress!;
        var disconnectedPlayer =
            Api!.DisconnectedPlayers.FirstOrDefault(x => x.SteamId.SteamId64 == player.AuthorizedSteamID.SteamId64);
        Api.DisconnectedPlayers.Remove(disconnectedPlayer!);
        Task.Run(async () =>
        {
            await ReloadPlayerInfractions(sid64, true, ip);
            ConvertAll();
        });
        return HookResult.Continue;
    }

    public static void ConvertAll()
    {
        Server.NextFrame(() =>
        {
            // Удаляем все установленные флаги
            foreach (var target in ConvertedFlags)
            {
                AdminManager.RemovePlayerPermissions(target.Key, target.Value.ToArray());
            }
            ConvertedFlags.Clear();
            // Устанавливаем иммунитет на прошлый
            foreach (var target in ConvertedImmunity)
            {
                AdminManager.SetPlayerImmunity(target.Key, target.Value);
            }
            ConvertedImmunity.Clear();
            // Удаляем из группы
            foreach (var target in ConvertedGroups)
            {
                AdminManager.RemovePlayerFromGroup(target.Key, true, target.Value);
            }
            ConvertedGroups.Clear();
            // Устанавливаем флаги для текущих админов
            var admins = Api!.ThisServerAdmins;
            Server.ExecuteCommand("css_admins_reload");
            _plugin.AddTimer(3, () =>
            {
                foreach (var admin in admins)
                {
                    try
                    {
                        var steamId = new SteamID(ulong.Parse(admin.SteamId));
                        if (admin.GroupName.Trim() != "")
                        {
                            var group = $"#css/{admin.GroupName}";
                            AdminManager.AddPlayerToGroup(steamId, new []{group});
                            ConvertedGroups.Add(steamId, group);
                            Console.WriteLine("Group " + group + " converted");
                        }
                        if (admin.Immunity > 0)
                        {
                            ConvertedImmunity.Add(steamId, AdminManager.GetPlayerImmunity(steamId));
                            AdminManager.SetPlayerImmunity(steamId, (uint)admin.Immunity);
                        }
                        var finalFlags = new List<string>();
                        foreach (var flag in ConfigNow.ConvertedFlags)
                        {
                            if (admin.Flags.Contains(flag.Key))
                            {
                                foreach (var cssFlag in flag.Value)
                                {
                                    AdminManager.AddPlayerPermissions(steamId, cssFlag);
                                    finalFlags.Add(cssFlag);
                                }
                            }
                        }
                        ConvertedFlags.Add(steamId, finalFlags);
                        Console.WriteLine($"[IksAdmin] Admin {admin.Name} | #{admin.SteamId} -> flags, immunity and group converted!");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[IksAdmin] Error cannot convert admin {admin.Name}! Error: {e}");
                    }
                }
            });
            
        });
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!XHelper.IsControllerValid(player)) return HookResult.Continue;
        Api!.DisconnectedPlayers.Add(XHelper.CreateInfo(player));
        return HookResult.Continue;
    }
    

    private static void KickPlayer(string sid)
    {
        Server.NextFrame(() =>
        {
            var player = XHelper.GetPlayerFromArg("#" + sid);
            if (player != null)
            {
                Server.ExecuteCommand($"kickid {player.UserId}");
            }
        });
    }

    private static void UpdatePlayerComms(string sid)
    {
        Server.NextFrame(() =>
        {
            var player = XHelper.GetPlayerFromArg("#" + sid);
            var mute = Api!.OnlineMutedPlayers.FirstOrDefault(x => x.Sid == sid);
            var gag = Api.OnlineGaggedPlayers.FirstOrDefault(x => x.Sid == sid);
            var timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (gag != null && gag.End < timeNow && gag.Time != 0)
            {
                Api.OnlineGaggedPlayers.Remove(gag);
            }
            if (player != null && mute != null && mute.Time != 0)
            {
                if (mute.End < timeNow)
                {
                    Api.OnlineMutedPlayers.Remove(mute);
                    return;
                }
                player.VoiceFlags = VoiceFlags.Muted;
            }
        });
    }

    private static async Task<bool> PlayerBanned(string sid, string ip)
    {
        var existingBan = await Api!.GetBan(sid);
        if (existingBan != null) return true;
        var existingBanIp = await Api.GetBan(ip);
        if (existingBanIp != null) return true;
        return false;
    }
    public static async Task ReloadPlayerInfractions(string sid64, bool checkBan = false, string ip = "127.0.0.1")
    {
        if (checkBan)
        {
            if (await PlayerBanned(sid64, ip))
            {
                KickPlayer(sid64);
                return;
            }
        }
        
        var existingMute = await Api!.GetMute(sid64);
        if (existingMute != null)
        {
            Api.OnlineMutedPlayers.Add(existingMute);
        }
        else
        {
            var muteOnServer = Api.OnlineMutedPlayers.FirstOrDefault(x => x.Sid == sid64);
            Api.OnlineMutedPlayers.Remove(muteOnServer!);
        }
        var existingGag = await Api.GetGag(sid64);
        if (existingGag != null)
        {
            Api.OnlineGaggedPlayers.Add(existingGag);
        }
        else
        {
            var gagOnServer = Api.OnlineGaggedPlayers.FirstOrDefault(x => x.Sid == sid64);
            Api.OnlineGaggedPlayers.Remove(gagOnServer!);
        }
        UpdatePlayerComms(sid64);
        
        var existingAdmin = await Api.GetAdmin(sid64);
        if (existingAdmin != null)
        {
            var adminInAllAdmins = Api.AllAdmins.FirstOrDefault(x => x.SteamId == sid64);
            var adminInThisServerAdmins = Api.ThisServerAdmins.FirstOrDefault(x => x.SteamId == sid64);
            if (adminInAllAdmins != null) Api.AllAdmins.Remove(adminInAllAdmins);
            if (adminInThisServerAdmins != null) Api.ThisServerAdmins.Remove(adminInThisServerAdmins);
            Api.AllAdmins.Add(existingAdmin);
            var serverId = existingAdmin.ServerId;
            if (serverId.Contains(ConfigNow.ServerId)  || serverId.Trim() == ""){
                Api.ThisServerAdmins.Add(existingAdmin);
            }
        }
    }
}

public class PluginApi : IIksAdminApi
{
    public string DbConnectionString { get; set; }
    public IPluginCfg Config { get; set; }
    public List<Admin> AllAdmins { get; set; } = new();
    public List<Admin> ThisServerAdmins { get; set; } = new();
    public Dictionary<CCSPlayerController, Admin> OnlineAdmins { get; set; } = new();
    public IStringLocalizer Localizer { get; set; }
    public List<AdminMenuOption> ModulesOptions { get; set; } = new ();
    public List<PlayerComm> OnlineMutedPlayers { get; set; } = new ();
    public List<PlayerComm> OnlineGaggedPlayers { get; set; } = new();
    public List<PlayerInfo> DisconnectedPlayers { get; set; } = new();
    public Dictionary<CCSPlayerController, Action<string>> NextCommandAction { get; set; } = new();
    public IIksAdminApi.UsedMenuType MenuType { get; set; } 

    public BasePlugin Plugin { get; }
    


    public PluginApi(BasePlugin plugin, PluginConfig config, IStringLocalizer localizer, string dbConnectionString)
    {
        Localizer = localizer;
        DbConnectionString = dbConnectionString;
        Config = config;
        Plugin = plugin;
        MenuType = Config.UseHtmlMenu ? IIksAdminApi.UsedMenuType.Html : IIksAdminApi.UsedMenuType.Chat;
        Task.Run(async () =>
        {
            await CreateTables();
            await ReloadAdmins();
        });
    }

    private async Task CreateTables()
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
            create table if not exists iks_admins (
                id INT NOT NULL AUTO_INCREMENT,
                sid VARCHAR(17) NOT NULL,
                name VARCHAR(32) NOT NULL,
                flags VARCHAR(64) NOT NULL,
                immunity INT NOT NULL,
                group_id INT NOT NULL DEFAULT '-1',
                end INT NOT NULL,
                server_id VARCHAR(64) NOT NULL,
                PRIMARY KEY (id)
            );
            ");

            await conn.QueryAsync(@"
            create table if not exists iks_bans (
                id INT NOT NULL AUTO_INCREMENT ,
                name VARCHAR(32) NOT NULL ,
                sid VARCHAR(32) NOT NULL,
                ip VARCHAR(32) NULL ,
                adminsid VARCHAR(32) NOT NULL ,
                created INT NOT NULL ,
                time INT NOT NULL ,
                end INT NOT NULL ,
                reason VARCHAR(255) NOT NULL,
                BanType INT(1) NOT NULL DEFAULT '0',
                Unbanned INT(1) NOT NULL DEFAULT '0',
                UnbannedBy VARCHAR(32) NULL ,
                server_id VARCHAR(1) NOT NULL DEFAULT '',
                PRIMARY KEY (id)
            );
            ");
            
            await conn.QueryAsync(@"
            create table if not exists iks_gags (
                id INT NOT NULL AUTO_INCREMENT ,
                name VARCHAR(32) NOT NULL ,
                sid VARCHAR(32) NOT NULL,
                adminsid VARCHAR(32) NOT NULL ,
                created INT NOT NULL ,
                time INT NOT NULL ,
                end INT NOT NULL ,
                reason VARCHAR(255) NOT NULL,
                Unbanned INT(1) NOT NULL DEFAULT '0',
                UnbannedBy VARCHAR(32) NULL ,
                server_id VARCHAR(1) NOT NULL DEFAULT '',
                PRIMARY KEY (id)
            );
            ");
            await conn.QueryAsync(@"
            create table if not exists iks_mutes (
                id INT NOT NULL AUTO_INCREMENT ,
                name VARCHAR(32) NOT NULL ,
                sid VARCHAR(32) NOT NULL,
                adminsid VARCHAR(32) NOT NULL ,
                created INT NOT NULL ,
                time INT NOT NULL ,
                end INT NOT NULL ,
                reason VARCHAR(255) NOT NULL,
                Unbanned INT(1) NOT NULL DEFAULT '0',
                UnbannedBy VARCHAR(32) NULL ,
                server_id VARCHAR(1) NOT NULL DEFAULT '',
                PRIMARY KEY (id)
            );
            ");

            await conn.QueryAsync(@"
            create table if not exists iks_groups (
                id INT NOT NULL AUTO_INCREMENT,
                flags VARCHAR(64) NOT NULL,
                name VARCHAR(32) NOT NULL,
                immunity INT NOT NULL,
                PRIMARY KEY (id)
            );
            ");
            
        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }
    }

    #region Admin Manage

    

    public async Task AddAdmin(Admin admin)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var existingAdmin = await GetAdmin(admin.SteamId);
            if (existingAdmin != null)
            {
                await DelAdmin(admin.SteamId);
            }
            await conn.QueryAsync(
                @"insert into iks_admins(sid, name, flags, immunity, group_id, end, server_id)
                values (@sid, @name, @flags, @immunity, @groupId, @end, @serverId)        
                ", new
                {
                    sid = admin.SteamId,
                    name = admin.Name,
                    flags = admin.Flags,
                    immunity = admin.Immunity,
                    groupId = admin.GroupId,
                    end = admin.End,
                    serverId = admin.ServerId
                });
            admin = await SetAdminGroup(admin);
            await ReloadAdmins();
            OnAddAdmin?.Invoke(admin);
        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }
    }

    public async Task<bool> DelAdmin(string sid)
    {
        try
        {
            var admin = await GetAdmin(sid);
            if (admin == null) return false;
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync("delete from iks_admins where sid = @sid", new {sid});
            await ReloadAdmins();
            OnDelAdmin?.Invoke(admin);
        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }
        return true;
    }

    public async Task<Admin?> GetAdmin(string sid)
    {
        Admin? admin = null;
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            admin = await conn.QuerySingleOrDefaultAsync<Admin>(@"
            select 
            name as name,
            sid as steamId,
            flags as flags,
            immunity as immunity,
            end as end,
            group_id as groupId,
            server_id as serverId
            from iks_admins
            where sid = @sid
            ", new {sid});
            if (admin == null)
                return null;
            admin = await SetAdminGroup(admin);
        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }

        return admin;
    }

    public async Task<List<Admin>> GetAllAdmins()
    {
        var admins = new List<Admin>();
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            admins = (await conn.QueryAsync<Admin>(@"select 
            name as name,
            sid as steamId,
            flags as flags,
            immunity as immunity,
            end as end,
            group_id as groupId,
            server_id as serverId
            from iks_admins
            ")).ToList();
            var groups = await GetAllGroups();
            foreach (var admin in admins.ToList())
            {
                if (admin.ServerId.Trim() != "" && !admin.ServerId.Contains(Config.ServerId))
                {
                    admins.Remove(admin);
                }
                if (admin.GroupId != -1 && groups.Any(x => x.Id == admin.GroupId))
                {
                    var group = groups.First(x => x.Id == admin.GroupId);
                    admin.GroupName = group.Name;
                    admin.Flags = admin.Flags.Trim() == "" ? group.Flags : admin.Flags;
                    admin.Immunity = admin.Immunity == -1 ? group.Immunity : admin.Immunity;
                }
            }

        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }

        return admins;
    }

    public async Task<List<Group>> GetAllGroups()
    {
        var groups = new List<Group>();
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            groups = (await conn.QueryAsync<Group>(@"
            select 
            name as name,
            flags as flags,
            immunity as immunity,
            id as id
            from iks_groups
            ")).ToList();
        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }
        return groups;
    }

    /// <summary>
    /// Возвращает "Обработанного" админа в соотвествии с группой
    /// </summary>
    private async Task<Admin> SetAdminGroup(Admin admin)
    {
        var groups = await GetAllGroups();
        if (admin.GroupId != -1 && groups.Any(x => x.Id == admin.GroupId))
        {
            var group = groups.First(x => x.Id == admin.GroupId);
            admin.GroupName = group.Name;
            admin.Flags = admin.Flags.Trim() == "" ? group.Flags : admin.Flags;
            admin.Immunity = admin.Immunity == -1 ? group.Immunity : admin.Immunity;
        }
        return admin;
    }
    public async Task ReloadAdmins()
    {
        var admins = await GetAllAdmins();
        ThisServerAdmins = admins.Where(x => x.ServerId.Contains(Config.ServerId) || x.ServerId.Trim() == "").ToList();
        AllAdmins = admins;
        Plugin.Logger.LogInformation("Admins loaded");
        Plugin.Logger.LogInformation("ThisServerAdmins: " + ThisServerAdmins.Count );
        Plugin.Logger.LogInformation("admins: " + admins.Count);
        OnReloadAdmins?.Invoke(AllAdmins);
        IksAdmin.ConvertAll();
    }
    
    #endregion

    #region events

    

    public event Action<Admin>? OnAddAdmin;
    public event Action<Admin>? OnDelAdmin;
    public event Action<PlayerBan>? OnAddBan;
    public event Action<PlayerComm>? OnAddMute;
    public event Action<PlayerComm>? OnAddGag;
    public event Action<PlayerBan, string>? OnUnBan;
    public event Action<PlayerComm, string>? OnUnMute;
    public event Action<PlayerComm, string>? OnUnGag;
    public event Action<string, PlayerInfo, string>? OnKick;
    public event Action<string, PlayerInfo, string, string>? OnRename;
    public event Action<string, PlayerInfo>? OnSlay;
    public event Action<string, PlayerInfo, CsTeam, CsTeam>? OnSwitchTeam;
    public event Action<string, PlayerInfo, CsTeam, CsTeam>? OnChangeTeam;
    public event Action<string, Map>? OnChangeMap;
    public event Action<List<Admin>>? OnReloadAdmins;
    public event Action<CCSPlayerController?, CommandInfo>? OnCommandUsed;

    #endregion

    #region Commands helpers
    
    public void AddNewCommand(string command, string description, string commandUsage, int minArgs, string flagAccess,
        string flagDefault, CommandUsage whoCanExecute,
        Action<CCSPlayerController, Admin?, List<string>, CommandInfo> onCommandExecute)
    {
        Plugin.AddCommand("css_" + command, description, (player, info) =>
        {
            Admin? admin = null;
            var adminSid = player == null ? "console" : player.AuthorizedSteamID!.SteamId64.ToString();
            if (player != null)
            {
                admin = ThisServerAdmins.FirstOrDefault(x =>
                    x.SteamId == player.AuthorizedSteamID!.SteamId64.ToString());
            }
            
            if (!HasAccess(adminSid, whoCanExecute, flagAccess, flagDefault))
            {
                info.ReplyToCommand(player == null
                    ? $" [IksAdmin] You haven't access to this command"
                    : $" {Localizer["PluginTag"]} {Localizer["NOTIFY_HaveNotAccess"]}");
                return;
            }
            List<string> args = GetArgsFromCommandLine(info.GetCommandString);
            var tag = player == null ? "[IksAdmin] " : Localizer["PluginTag"];
            if (args.Count < minArgs)
            {
                info.ReplyToCommand($" {tag} Command usage:");
                foreach (var str in commandUsage.Split("\n"))
                {
                    info.ReplyToCommand($" {tag} {str}");
                }
                info.ReplyToCommand($" {tag} min args: {minArgs}");
                return;
            }

            try
            {
                onCommandExecute.Invoke(player!, admin, args, info);
            }
            catch (Exception)
            {
                info.ReplyToCommand($" {tag} There are mistakes in your command");
                info.ReplyToCommand($" {tag} Command usage:");
                foreach (var str in commandUsage.Split("\n"))
                {
                    info.ReplyToCommand($" {tag} {str}");
                }
            }
            OnCommandUsed?.Invoke(player, info);
        });
    }
    public bool HasAccess(string adminSid, CommandUsage commandUsage, string flagsAccess, string flagsDefault)
    {
        adminSid = adminSid.ToLower();
        if (commandUsage == CommandUsage.CLIENT_ONLY && adminSid == "console") return false;
        if (commandUsage == CommandUsage.SERVER_ONLY && adminSid != "console") return false;
        if (adminSid == "console" && commandUsage != CommandUsage.CLIENT_ONLY) return true;
        
        return HasPermisions(adminSid, flagsAccess, flagsDefault);
    }
    
    public bool HasPermisions(string adminSid, string flagsAccess, string flagsDefault)
    {
        adminSid = adminSid.ToLower();
        if (adminSid == "console") return true;
        var admin = ThisServerAdmins.FirstOrDefault(x =>
            x.SteamId == adminSid);
        var containsKey = Config.Flags.ContainsKey(flagsAccess);
        if (containsKey)
        {
            var flag = Config.Flags[flagsAccess];
            if (flag != "*")
            {
                if (admin == null) return false;
                if (!admin.Flags.Any(flag.Contains) && !admin.Flags.Contains("z")) return false;
            }
        }
        if (!containsKey)
        {
            if (flagsDefault != "*")
            {
                if (admin == null) return false;
                if (!admin.Flags.Any(flagsDefault.Contains) && !admin.Flags.Contains("z")) return false;
            }
        }
        return true;
    }
    
    public bool HasMoreImmunity(string adminSid, string targetSid)
    {
        if (adminSid.ToLower() == "console") return true;
        var targetAdmin = ThisServerAdmins.FirstOrDefault(x => x.SteamId == targetSid);
        if (targetAdmin == null) return true;
        var callerAdmin = ThisServerAdmins.FirstOrDefault(x => x.SteamId == adminSid);
        if (callerAdmin == null) return false;
        if (Config.HasAccessIfImmunityIsEqual)
        {
            if (callerAdmin.Immunity < targetAdmin.Immunity) return false;
        } else if (callerAdmin.Immunity <= targetAdmin.Immunity) return false;
        return true;
    }
    private static List<string> GetArgsFromCommandLine(string commandLine)
    {
        List<string> args = new List<string>();
        var regex = new Regex(@"(""((\\"")|([^""]))*"")|('((\\')|([^']))*')|(\S+)");
        var matches = regex.Matches(commandLine);
        foreach (Match match in matches)
        {
            var arg = match.Value;
            if (arg.StartsWith('"'))
            {
                arg = arg.Remove(0, 1);
                arg = arg.TrimEnd('"');
            }
            args.Add(arg);
        }
        args.RemoveAt(0);
        return args;
    }
    #endregion

    #region Punishments

    public async Task ReloadInfractions(string sid, bool checkBan = true)
    {
        await IksAdmin.ReloadPlayerInfractions(sid, checkBan);
    }

    public async Task<bool> AddBan(string adminSid, PlayerBan banInfo)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await GetBan(banInfo.Sid);
            if (ban != null)
                return false;
            await conn.QueryAsync("insert into iks_bans(name, sid, ip, adminsid, created, time, end, reason, BanType, server_id)" +
                                  "values (@name, @sid, @ip, @adminSid, @created, @time, @end, @reason, @banType, @serverId)",
                                  new
                                  {
                                      name = banInfo.Name,
                                      sid = banInfo.Sid,
                                      ip = banInfo.Ip,
                                      adminSid = banInfo.AdminSid,
                                      created = banInfo.Created,
                                      time = banInfo.Time,
                                      end = banInfo.End,
                                      reason = banInfo.Reason,
                                      banType = banInfo.BanType,
                                      serverId = banInfo.ServerId
                                  });
            await ReloadInfractions(banInfo.Sid);
            OnAddBan?.Invoke(banInfo);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return true;
    }

    public async Task<bool> AddMute(string adminSid, PlayerComm muteInfo)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await GetMute(muteInfo.Sid);
            if (ban != null)
                return false;
            await conn.QueryAsync("insert into iks_mutes(name, sid, adminsid, created, time, end, reason, server_id)" +
                                  "values (@name, @sid, @adminSid, @created, @time, @end, @reason, @serverId)",
                new
                {
                    name = muteInfo.Name,
                    sid = muteInfo.Sid,
                    adminSid = muteInfo.AdminSid,
                    created = muteInfo.Created,
                    time = muteInfo.Time,
                    end = muteInfo.End,
                    reason = muteInfo.Reason,
                    serverId = muteInfo.ServerId
                });
            OnAddMute?.Invoke(muteInfo);
            await ReloadInfractions(muteInfo.Sid, false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return true;
    }

    public async Task<bool> AddGag(string adminSid, PlayerComm gagInfo)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await GetGag(gagInfo.Sid);
            if (ban != null)
                return false;
            await conn.QueryAsync("insert into iks_gags(name, sid, adminsid, created, time, end, reason, server_id)" +
                                  "values (@name, @sid, @adminSid, @created, @time, @end, @reason, @serverId)",
                new
                {
                    name = gagInfo.Name,
                    sid = gagInfo.Sid,
                    adminSid = gagInfo.AdminSid,
                    created = gagInfo.Created,
                    time = gagInfo.Time,
                    end = gagInfo.End,
                    reason = gagInfo.Reason,
                    serverId = gagInfo.ServerId
                });
            OnAddGag?.Invoke(gagInfo);
            await ReloadInfractions(gagInfo.Sid, false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return true;
    }

    public async Task<PlayerBan?> UnBan(string sid, string adminSid)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await GetBan(sid);
            if (ban == null)
                return null;

            await conn.QueryAsync("update iks_bans set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = ban.Id, unbannedBy = adminSid });
            
            OnUnBan?.Invoke(ban, adminSid);
            return ban;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public async Task<PlayerComm?> UnMute(string sid, string adminSid)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var mute = await GetMute(sid);
            if (mute == null)
                return null;

            await conn.QueryAsync("update iks_mutes set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = mute.Id, unbannedBy = adminSid });
            OnUnMute!.Invoke(mute, adminSid);
            await ReloadInfractions(sid, false);
            return mute;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public async Task<PlayerComm?> UnGag(string sid, string adminSid)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var gag = await GetGag(sid);
            if (gag == null)
                return null;

            await conn.QueryAsync("update iks_gags set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = gag.Id, unbannedBy = adminSid });
            OnUnGag?.Invoke(gag, adminSid);
            await ReloadInfractions(sid, false);
            return gag;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public async Task<PlayerBan?> GetBan(string arg)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await conn.QueryFirstOrDefaultAsync<PlayerBan>(@"
            select 
            name as name,
            sid as sid,
            ip as ip,
            adminsid as adminSid,
            created as created,
            time as time,
            end as end,
            reason as reason,
            server_id as serverId,
            BanType as banType,
            Unbanned as unbanned,
            UnbannedBy as unbannedBy,
            id as id
            from iks_bans
            where (sid = @arg or (ip = @arg and BanType = 1)) and (end > @timeNow or time = 0) and Unbanned = 0
            ", new {arg, timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds()});
            return ban;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public async Task<PlayerComm?> GetMute(string sid)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var mute = await conn.QueryFirstOrDefaultAsync<PlayerComm>(@"
            select 
            name as name,
            sid as sid,
            adminsid as adminSid,
            created as created,
            time as time,
            end as end,
            reason as reason,
            server_id as serverId,
            Unbanned as unbanned,
            UnbannedBy as unbannedBy,
            id as id
            from iks_mutes
            where sid = @sid and (end > @timeNow or time = 0) and Unbanned = 0
            ", new {sid, timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds()});
            return mute;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }
    public async Task<PlayerComm?> GetGag(string sid)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var gag = await conn.QueryFirstOrDefaultAsync<PlayerComm>(@"
            select 
            name as name,
            sid as sid,
            adminsid as adminSid,
            created as created,
            time as time,
            end as end,
            reason as reason,
            server_id as serverId,
            Unbanned as unbanned,
            UnbannedBy as unbannedBy,
            id as id
            from iks_gags
            where sid = @sid and (end > @timeNow or time = 0) and Unbanned = 0 
            ", new {sid, timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds()});
            return gag;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }
    
    #endregion

    #region Other

    public IBaseMenu CreateMenu(CCSPlayerController caller, Action<CCSPlayerController, Admin?, IMenu> onOpen)
    {
        return new Menu(caller, onOpen);
    }

    public void SendMessageToPlayer(CCSPlayerController? controller, string message)
    {
        Server.NextFrame(() =>
        {
            if (message.Trim() == "") return;
            foreach (var str in message.Split("\n"))
            {
                if (controller == null)
                {
                    Console.WriteLine($"[IksAdmin] {str}");
                }
                else
                {
                    controller.PrintToChat($" {Localizer["PluginTag"]} {str}");
                }
            }
        });
    }

    public void SendMessageToAll(string message)
    {
        Server.NextFrame(() =>
        {
            if (message.Trim() == "") return;
            foreach (var str in message.Split("\n"))
            {
                Server.PrintToChatAll($" {Localizer["PluginTag"]} {str}");
            }
        });
    }

    #endregion

    #region Events callers

    public void EKick(string adminSid, PlayerInfo target, string reason)
    {
        OnKick?.Invoke(adminSid, target, reason);
    }
    public void ESlay(string adminSid, PlayerInfo target)
    {
        OnSlay?.Invoke(adminSid, target);
    }
    public void ESwitchTeam(string adminSid, PlayerInfo target, CsTeam oldTeam, CsTeam newTeam)
    {
        OnSwitchTeam?.Invoke(adminSid, target, oldTeam, newTeam);
    }
    public void EChangeTeam(string adminSid, PlayerInfo target, CsTeam oldTeam, CsTeam newTeam)
    {
        OnChangeTeam?.Invoke(adminSid, target, oldTeam, newTeam);
    }

    public void EChangeMap(string adminSid, Map newMap)
    {
        OnChangeMap?.Invoke(adminSid, newMap);
    }

    public void ERename(string adminSid, PlayerInfo target, string oldName, string newName)
    {
        OnRename?.Invoke(adminSid, target, oldName, newName);
    }

    #endregion
}
