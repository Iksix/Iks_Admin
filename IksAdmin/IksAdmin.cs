using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using Dapper;
using IksAdmin.Commands;
using IksAdminApi;
using MenuManager;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Group = IksAdminApi.Group;

namespace IksAdmin;

public class IksAdmin : BasePlugin, IPluginConfig<PluginConfig>
{
    // ToDo:
    // - Рефакторинг кода команд
    // - Вынести все функции использующиеся для создания команд в API
    // - Привести базу данных в порядок, используя SQL от Flames
    // - Использовать JOIN в sql
    
    public override string ModuleName => "IksAdmin";
    public override string ModuleVersion => "2.1.9.1";
    public override string ModuleAuthor => "iks__";

    private static List<int> _kickSlots = new();

    public static PluginApi? Api;
    private static string _dbConnectionString = "";
    private readonly PluginCapability<IIksAdminApi> _pluginCapability  = new("iksadmin:core");
    public PluginConfig Config { get; set; } = new();
    public static PluginConfig ConfigNow = new();
    private static BasePlugin? _plugin;

    private static readonly Dictionary<SteamID, List<string>> ConvertedFlags = new();
    private static readonly Dictionary<SteamID, string> ConvertedGroups = new();
    private static readonly Dictionary<SteamID, uint> ConvertedImmunity = new();
    public static IMenuApi? MenuManager;
    private static readonly PluginCapability<IMenuApi?> PluginMenuCapability = new("menu:nfcore");   
    
    public void OnConfigParsed(PluginConfig config)
    {
        config = ConfigManager.Load<PluginConfig>(ModuleName);
        var dbConnectionBuilder = new MySqlConnectionStringBuilder();
        dbConnectionBuilder.Password = config.Password;
        dbConnectionBuilder.Server = config.Host;
        dbConnectionBuilder.Database = config.Database;
        dbConnectionBuilder.UserID = config.User;
        dbConnectionBuilder.Port = uint.Parse(config.Port);

        _dbConnectionString = dbConnectionBuilder.ConnectionString;
        Config = config;
        ConfigNow = config;
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        MenuManager = PluginMenuCapability.Get();
    }

    public override void Load(bool hotReload)
    {
        Api = new PluginApi(this, Config, Localizer, _dbConnectionString);
        Capabilities.RegisterPluginCapability(_pluginCapability, () => Api);
        BaseCommands.Config = Config;
        RegisterListener<Listeners.OnClientAuthorized>(OnAuthorized);
        _plugin = this;
        InitializeCommands();
        InitMessages();
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

        AddTimer(1, () =>
        {
            var allAdmins = Api!.AllAdmins;
            foreach (var admin in allAdmins.ToList())
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
                if (_kickSlots.Contains(p.Slot))
                    Server.ExecuteCommand("kickid " + p.UserId);
                if (!XHelper.IsControllerValid(p)) continue;
                UpdatePlayerComms(p.AuthorizedSteamID!.SteamId64.ToString());
            }
        }, TimerFlags.REPEAT);
    }

    private void InitMessages()
    {
        Api!.OnKick += (adminId, target, reason) =>
        {
            BaseMessages.SERVER_KICK(adminId, target, reason);
        };
        Api!.OnSwitchTeam += (adminId, target, oldTeam, newTeam) =>
        {
            BaseMessages.SERVER_SWITCHTEAM(adminId, target, oldTeam, newTeam);
        };
        Api!.OnChangeTeam += (adminId, target, oldTeam, newTeam) =>
        {
            BaseMessages.SERVER_CHANGETEAM(adminId, target, oldTeam, newTeam);
        };
        Api!.OnRename += (adminId, target, oldName, newName) =>
        {
            BaseMessages.SERVER_RENAME(adminId, target, oldName, newName);
        };
    }

    private void OnAuthorized(int slot, SteamID steamid)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        Console.WriteLine($"{steamid.SteamId64} CLIENT AUTHORIZED");
        string sid64 = steamid.SteamId64.ToString();
        string ip = player!.IpAddress!;
        string name = player.PlayerName;
        Console.WriteLine($"{name} name");
        Console.WriteLine($"{sid64} sid64");
        Console.WriteLine($"{ip} ip");
        if (Api!.DisconnectedPlayers.ContainsKey(sid64))
        {
            Api!.DisconnectedPlayers.Remove(sid64);
            Console.WriteLine($"Removed from disconnected players");
        }
        
        Task.Run(async () =>
        {
            await ReloadPlayerInfractions(sid64, true, ip, slot, name, true);
            ConvertAll();
        });
    }
    private void InitializeCommands()
    {
        Api!.AddNewCommand(
            "db_update",
            "update db structure",
            " ",
            0,
            "dbUpdate",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.DbUpdate
        );
        Api.AddNewCommand(
            "admin_reload_cfg",
            "reloads admin cfg",
            "css_admin_reload_cfg",
            0,
            "reload_cfg",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            ReloadConfig
        );
        Api.AddNewCommand(
            "group_add",
            "add group",
            "css_group_add <name> <flags> <immunity>",
            3,
            "groupManage",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.GroupAdd
        );
        Api.AddNewCommand(
            "group_del",
            "delete group",
            "css_group_del <name>",
            1,
            "groupManage",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.GroupDel
        );
        Api.AddNewCommand(
            "group_list",
            "view group list",
            "css_group_list",
            0,
            "groupManage",
            "z",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.GroupList
        );
        Api.AddNewCommand(
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
            "banip",
            "ban the player by IP",
            "css_banip <$ip/#uid/#sid/name> <duration> <reason> <name if needed>",
            3,
            "banip",
            "b",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.BanIp
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
            "silence",
            "silence the player",
            "css_silence <#uid/#sid/name> <duration> <reason> <name if needed>",
            3,
            "silence",
            "gm",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.Silence
        );
        Api.AddNewCommand(
            "unsilence",
            "unsilence the player",
            "css_unsilence <#uid/#sid/name>",
            1,
            "unsilence",
            "gm",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.UnSilence
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
            1,
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
            "t",
            CommandUsage.CLIENT_AND_SERVER,
            BaseCommands.SwitchTeam
        );
        Api.AddNewCommand(
            "changeteam",
            "change the player team",
            "css_changeteam <#uid/#sid/name> <ct/t/spec>",
            2,
            "changeteam",
            "t",
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

    private void ReloadConfig(CCSPlayerController arg1, Admin? arg2, List<string> arg3, CommandInfo info)
    {
        OnConfigParsed(Config);
        Api!.Config = Config;
        BaseCommands.Config = Config;
        Api.MenuType = Config.MenuType switch
        {
            "html" => MenuType.CenterMenu,
            "chat" => MenuType.ChatMenu,
            "button" => MenuType.ButtonMenu,
            _ => MenuType.Default
        };
        info.ReplyToCommand("Config reloaded");
    }

    

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;
        if (player.AuthorizedSteamID == null) return HookResult.Continue;
        var playerSid = player.AuthorizedSteamID.SteamId64.ToString();
        bool toTeam = info.GetArg(0) == "say_team";
        var message = info.GetArg(1);

        var existingGag =
            Api!.OnlineGaggedPlayers.FirstOrDefault(x => x.Sid == playerSid);
        if (message.StartsWith("!") || message.StartsWith("/"))
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
            var existingAdmin = Api.GetAdminBySid(playerSid);
            var players = XHelper.GetOnlinePlayers();
            if (existingAdmin != null)
            {
                if (!toTeam)
                {
                    Server.PrintToChatAll(Localizer["OTHER_FromAdminToAll"].ToString()
                        .Replace("{message}", messageToAdmins)
                        .Replace("{name}", player.PlayerName)
                    );
                    return HookResult.Handled;
                }
            }
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
                    Api.GetAdminBySid(p.AuthorizedSteamID!.SteamId64.ToString());
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
    public HookResult OnChangeTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        if (Localizer["OTHER_FakeLeaveWhenHide"].Value == "") return HookResult.Continue;
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        if (@event.Team != 1) return HookResult.Continue;
        if (!BaseCommands.HidenPlayers.Contains(player)) return HookResult.Continue;
        info.DontBroadcast = true;
        Server.PrintToChatAll(Localizer["OTHER_FakeLeaveWhenHide"].Value.Replace("{name}", player.PlayerName));
        return HookResult.Stop;
    }
    


    public static void ConvertAll()
    {
        Server.NextFrame(() =>
        {
            // Удаляем все установленные флаги
            foreach (var target in ConvertedFlags)
            {
                foreach (var flag in target.Value)
                {
                    AdminManager.RemovePlayerPermissions(target.Key, new []{ flag });
                }
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
            var admins = Api!.GetThisServerAdmins();
            _plugin!.AddTimer(3, () =>
            {
                foreach (var admin in admins)
                {
                    try
                    {
                        var steamId = new SteamID(ulong.Parse(admin.SteamId));
                        var adminData = AdminManager.GetPlayerAdminData(steamId);
                        if (admin.GroupName.Trim() != "")
                        {
                            var group = $"#css/{admin.GroupName}";
                            AdminManager.AddPlayerToGroup(steamId, new []{group});
                            adminData!.Groups.Add(group);
                            ConvertedGroups.Remove(steamId);
                            ConvertedGroups.Add(steamId, group);
                        }
                        if (admin.Immunity > 0)
                        {
                            ConvertedImmunity.Remove(steamId);
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
                        ConvertedFlags.Remove(steamId);
                        ConvertedFlags.Add(steamId, finalFlags);
                    }
                    catch (Exception _)
                    {
                        // ignore
                    }
                }
            });
            
        });
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        if (player.IsBot || !player.IsValid) return HookResult.Continue;
        if (_kickSlots.Contains(player.Slot))
            _kickSlots.Remove(player.Slot);
        if (Api!.TimeAdmins.ContainsKey(player))
            Api.TimeAdmins.Remove(player);
        if (player.AuthorizedSteamID == null) return HookResult.Continue;
        Api.DisconnectedPlayers.Add(player.AuthorizedSteamID.SteamId64.ToString(), XHelper.CreateInfo(player));
        Console.WriteLine($"Added to disconnected players");
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        if (player.AuthorizedSteamID != null || player.IsBot) return HookResult.Continue;

        if (Config.NotAuthorizedKickTime == 0) return HookResult.Continue;
        
        AddTimer(Config.NotAuthorizedKickTime, () =>
        {
            if (player.AuthorizedSteamID != null) return;
            Logger.LogWarning("Kick not authorized player! | name: " + player.PlayerName);
            Server.ExecuteCommand("kickid " + player.UserId);
        });
        
        return HookResult.Continue;
    }
    
    private static void KickPlayer(string sid)
    {
        Server.NextFrame(() =>
        {
            var player = XHelper.GetPlayerFromArg("#" + sid);
            if (player != null)
            {
                player.Disconnect(NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
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
            if (player != null && mute != null)
            {
                if (mute.End < timeNow && mute.Time != 0)
                {
                    Api.OnlineMutedPlayers.Remove(mute);
                    player.VoiceFlags = VoiceFlags.Normal;
                    return;
                }
                player.VoiceFlags = VoiceFlags.Muted;
            }

            if (player != null && mute == null)
            {
                player.VoiceFlags = VoiceFlags.Normal;
            }
            
        });
    }

    private static async Task<PlayerBan?> GetPlayerBan(string sid, string ip)
    {
        var existingBan = await Api!.GetBan(sid);
        if (existingBan != null) return existingBan;
        var existingBanIp = await Api.GetBan(ip.Split(":")[0]);
        if (existingBanIp != null) return existingBanIp;
        return null;
    }

    public static async Task ReloadPlayerInfractions(string sid64, bool checkBan = false, string ip = "127.0.0.1", int? slot = null, string? name = null, bool onConnected = false)
    {
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
            var adminInThisServerAdmins = Api.GetAdminBySid(sid64);
            if (adminInAllAdmins != null) Api.AllAdmins.Remove(adminInAllAdmins);
            if (adminInThisServerAdmins != null) Api.ThisServerAdmins.Remove(adminInThisServerAdmins);
            if (!string.IsNullOrEmpty(name))
            {
                existingAdmin.Name = name;
                await using var conn = new MySqlConnection(_dbConnectionString);
                await conn.OpenAsync();
                if (ConfigNow.UpdateNames)
                {
                    await conn.QueryAsync("update iks_admins set name = @name where sid = @sid;",
                        new { name, sid = sid64 });
                }
            }

            Api.AllAdmins.Add(existingAdmin);
            var serverId = existingAdmin.ServerId.Split(";");
            if (serverId.Contains(ConfigNow.ServerId) || existingAdmin.ServerId.Trim() == "")
            {
                Api.ThisServerAdmins.Add(existingAdmin);
            }
        }

        PlayerBan? existingBan = null;
        if (checkBan)
        {
            existingBan = await GetPlayerBan(sid64, ip);
            if (existingBan != null)
            {
                KickPlayer(sid64);
                if (slot != null)
                    _kickSlots.Add((int)slot);
            }
        }

        if (onConnected)
            Api.EOnPlayerConnected(
                new PlayerInfo(name!, ulong.Parse(sid64), ip),
                existingAdmin, 
                existingBan,
                existingMute, 
                existingGag);
    }
}

public class PluginApi : IIksAdminApi
{
    public string DbConnectionString { get; set; }
    public IPluginCfg Config { get; set; }
    public List<Admin> AllAdmins { get; set; } = new();
    public List<Admin> ThisServerAdmins { get; set; } = new();
    public Dictionary<CCSPlayerController, Admin> TimeAdmins { get; set; } = new();
    public IStringLocalizer Localizer { get; set; }
    public List<AdminMenuOption> ModulesOptions { get; set; } = new ();
    public List<PlayerComm> OnlineMutedPlayers { get; set; } = new ();
    public List<PlayerComm> OnlineGaggedPlayers { get; set; } = new();
    public Dictionary<string, PlayerInfo> DisconnectedPlayers { get; set; } = new();
    public Dictionary<CCSPlayerController, Action<string>> NextCommandAction { get; set; } = new();
    public MenuType MenuType { get; set; } 

    public BasePlugin Plugin { get; }
    public List<Admin> GetThisServerAdmins()
    {
        var admins = ThisServerAdmins;
        foreach (var a in TimeAdmins)
        {
            admins.Add(a.Value);
        }
        return admins;
    }

    public Admin? GetAdmin(CCSPlayerController player)
    {
        var exsistingAdmin =
            ThisServerAdmins.FirstOrDefault(x => x.SteamId == player.AuthorizedSteamID!.SteamId64.ToString());
        if (exsistingAdmin != null)
            return exsistingAdmin;
        if (TimeAdmins.ContainsKey(player))
            return TimeAdmins[player];
        return null;
    }

    public Admin? GetAdmin(ulong steamId)
    {
        var exsistingAdmin =
            ThisServerAdmins.FirstOrDefault(x => x.SteamId == steamId.ToString());
        if (exsistingAdmin != null)
            return exsistingAdmin;
        return null;
    }

    public Dictionary<CCSPlayerController, Admin> GetOnlineAdmins()
    {
        var admins = new Dictionary<CCSPlayerController, Admin>();
        var players = XHelper.GetOnlinePlayers();
        foreach (var player in players)
        {
            var admin = GetAdmin(player);
            if (admin == null) continue;
            admins.Add(player, admin);
        }

        return admins;
    }

    public Admin? GetAdminBySid(string steamId)
    {
        var exsistingAdmin =
            ThisServerAdmins.FirstOrDefault(x => x.SteamId == steamId);
        if (exsistingAdmin != null)
            return exsistingAdmin;
        exsistingAdmin =
            TimeAdmins.FirstOrDefault(x => x.Value.SteamId == steamId).Value;
        if (exsistingAdmin != null)
            return exsistingAdmin;
        return null;
    }


    public PluginApi(BasePlugin plugin, PluginConfig config, IStringLocalizer localizer, string dbConnectionString)
    {
        Localizer = localizer;
        DbConnectionString = dbConnectionString;
        Config = config;
        Plugin = plugin;
        MenuType = config.MenuType switch
        {
            "html" => MenuType.CenterMenu,
            "chat" => MenuType.ChatMenu,
            "button" => MenuType.ButtonMenu,
            _ => MenuType.Default
        };
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
                adminName VARCHAR(64) NOT NULL,
                created INT NOT NULL ,
                time INT NOT NULL ,
                end INT NOT NULL ,
                reason VARCHAR(255) NOT NULL,
                BanType INT(1) NOT NULL DEFAULT '0',
                Unbanned INT(1) NOT NULL DEFAULT '0',
                UnbannedBy VARCHAR(32) NULL ,
                server_id VARCHAR(128) NOT NULL DEFAULT '',
                PRIMARY KEY (id)
            );
            ");
            
            await conn.QueryAsync(@"
            create table if not exists iks_gags (
                id INT NOT NULL AUTO_INCREMENT ,
                name VARCHAR(32) NOT NULL ,
                sid VARCHAR(32) NOT NULL,
                adminsid VARCHAR(32) NOT NULL ,
                adminName VARCHAR(64) NOT NULL,
                created INT NOT NULL ,
                time INT NOT NULL ,
                end INT NOT NULL ,
                reason VARCHAR(255) NOT NULL,
                Unbanned INT(1) NOT NULL DEFAULT '0',
                UnbannedBy VARCHAR(32) NULL ,
                server_id VARCHAR(128) NOT NULL DEFAULT '',
                PRIMARY KEY (id)
            );
            ");
            await conn.QueryAsync(@"
            create table if not exists iks_mutes (
                id INT NOT NULL AUTO_INCREMENT ,
                name VARCHAR(32) NOT NULL ,
                sid VARCHAR(32) NOT NULL,
                adminsid VARCHAR(32) NOT NULL ,
                adminName VARCHAR(64) NOT NULL,
                created INT NOT NULL ,
                time INT NOT NULL ,
                end INT NOT NULL ,
                reason VARCHAR(255) NOT NULL,
                Unbanned INT(1) NOT NULL DEFAULT '0',
                UnbannedBy VARCHAR(32) NULL ,
                server_id VARCHAR(128) NOT NULL DEFAULT '',
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
    
    public async Task AddGroup(Group group)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var existingGroup = await GetGroup(group.Name);
            if (existingGroup != null)
            {
                await DeleteGroup(group.Name);
            }
            await conn.QueryAsync(
                @"insert into iks_groups(name, flags, immunity)
                values (@name, @flags, @immunity)        
                ", new
                {
                    name = group.Name,
                    flags = group.Flags,
                    immunity = group.Immunity
                });
        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }
    }
    public async Task<Group?> GetGroup(string name)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var group = await conn.QueryFirstOrDefaultAsync<Group>
            ("""
             select
             name as name,
             flags as flags,
             immunity as immunity,
             id as id
             from iks_groups
             where name = @name
             """, new {name});
            return group;
        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }
        return null;
    }
    public async Task DeleteGroup(string name)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync
            ("""
             delete
             from iks_groups
             where name = @name
             """, new {name});
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
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            
            var admins = (await conn.QueryAsync<Admin>(@"
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
            ", new {sid})).ToList();
            foreach (var admin in admins)
            {
                if (admin.ServerId.Split(";").Contains(Config.ServerId) || admin.ServerId == "")
                {
                    return await SetAdminGroup(admin);
                }
            }
        }
        catch (MySqlException e)
        {
            Plugin.Logger.LogError("DB ERROR: " + e);
        }

        return null;
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
        ThisServerAdmins = admins.Where(x => x.ServerId.Split(";").Contains(Config.ServerId) || x.ServerId.Trim() == "").ToList();
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
    public event Action<PlayerBan>? OnUnBan;
    public event Action<PlayerComm>? OnUnMute;
    public event Action<PlayerComm>? OnUnGag;
    public event Action<string, PlayerInfo, string>? OnKick;
    public event Action<string, PlayerInfo, string, string>? OnRename;
    public event Action<string, PlayerInfo>? OnSlay;
    public event Action<string, PlayerInfo, CsTeam, CsTeam>? OnSwitchTeam;
    public event Action<string, PlayerInfo, CsTeam, CsTeam>? OnChangeTeam;
    public event Action<string, Map>? OnChangeMap;
    public event Action<List<Admin>>? OnReloadAdmins;
    public event Action<CCSPlayerController?, CommandInfo>? OnCommandUsed;

    public void EOnPlayerConnected(PlayerInfo info, Admin? admin, PlayerBan? ban, PlayerComm? mute, PlayerComm? gag)
    {
        OnPlayerConnected?.Invoke(info, admin, ban, mute, gag);
    }
    public event Action<PlayerInfo, Admin?, PlayerBan?, PlayerComm?, PlayerComm?>? OnPlayerConnected;

    #endregion

    #region Commands helpers
    
    public void AddNewCommand(string command, string description, string commandUsage, int minArgs, string flagAccess,
        string flagDefault, CommandUsage whoCanExecute,
        Action<CCSPlayerController, Admin?, List<string>, CommandInfo> onCommandExecute)
    {
        Plugin.AddCommand("css_" + command, description, (player, info) =>
        {
            try
            {
                Admin? admin = null;
                if (player != null)
                {
                    if (player.AuthorizedSteamID == null)
                    {
                        Plugin.Logger.LogError($"INVALID STEAM_ID {info.GetCommandString}");
                        return;
                    }
                }
                
                var adminSid = player == null ? "console" : player.AuthorizedSteamID!.SteamId64.ToString();
                if (player != null)
                {
                    admin = GetAdminBySid(player.AuthorizedSteamID!.SteamId64.ToString());
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
                catch (Exception e)
                {
                    Plugin.Logger.LogError(e.ToString());
                    info.ReplyToCommand($" {tag} There are mistakes in your command");
                    info.ReplyToCommand($" {tag} Command usage:");
                    foreach (var str in commandUsage.Split("\n"))
                    {
                        info.ReplyToCommand($" {tag} {str}");
                    }
                }
                OnCommandUsed?.Invoke(player, info);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e.ToString());
            }
        });
    }
    public bool HasAccess(string adminSid, CommandUsage commandUsage, string flagsAccess, string flagsDefault)
    {
        adminSid = adminSid.ToLower();
        if (commandUsage == CommandUsage.CLIENT_ONLY && adminSid == "console") return false;
        if (commandUsage == CommandUsage.SERVER_ONLY && adminSid != "console") return false;
        if (adminSid == "console" && commandUsage != CommandUsage.CLIENT_ONLY) return true;
        
        return HasPermissions(adminSid, flagsAccess, flagsDefault);
    }

    public void ReplyToCommand(CommandInfo info, string reply, string? replyToConsole = null, string? customTag = null)
    {
        Server.NextFrame(() =>
        {
            var player = info.CallingPlayer;
            string message = player != null ? reply : replyToConsole == null ? reply : replyToConsole;
            var tag = customTag == null ? Localizer["PluginTag"] : customTag;
            foreach (var str in message.Split("\n"))
            {
                if (player == null)
                {
                    info.ReplyToCommand($" {tag} {str}");
                }
                else
                {
                    if (reply.Trim() == "") return;
                    player.PrintToChat($" {tag} {str}");
                }
            }
        });
    }
    
    public bool HasPermissions(string adminSid, string flagsAccess, string flagsDefault)
    { 
        return HasPermisions(adminSid, flagsAccess, flagsDefault);
    }

    public bool HasPermisions(string adminSid, string flagsAccess, string flagsDefault)
    {
        adminSid = adminSid.ToLower();
        if (adminSid == "console") return true;
        var admin = GetAdminBySid(adminSid);
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
        if (targetSid == adminSid) return true;
        if (adminSid.ToLower() == "console") return true;
        var targetAdmin = GetAdminBySid(targetSid);
        if (targetAdmin == null) return true;
        var callerAdmin = GetAdminBySid(adminSid);
        if (callerAdmin == null) return false;
        if (Config.HasAccessIfImmunityIsEqual)
        {
            if (callerAdmin.Immunity < targetAdmin.Immunity) return false;
        } else if (callerAdmin.Immunity <= targetAdmin.Immunity) return false;
        return true;
    }

    public void ConvertAll()
    {
        IksAdmin.ConvertAll();
    }

    public CCSPlayerController? GetPlayerFromIdentity(string identity)
    {
        var players = XHelper.GetOnlinePlayers();
        CCSPlayerController? player;
        if (identity.StartsWith("#"))
        {
            player = players.FirstOrDefault(u => u.SteamID.ToString() == identity.Replace("#", ""));

            if (player != null) return player;

            player = players.FirstOrDefault(u => u.UserId.ToString() == identity.Replace("#", ""));

            if (player != null) return player;
        }
        if (!identity.StartsWith("#"))
            return players.FirstOrDefault(u => u.PlayerName.Contains(identity));
        return null;
    }

    public void EOnMenuOpen(string index, IMenu menu, CCSPlayerController player)
    {
        OnMenuOpen?.Invoke(index, menu, player);
    }

    public event IIksAdminApi.PreBanEventHandler? PreBan;
    public event IIksAdminApi.PreBanEventHandler? PreUnBan;

    public async Task<bool> AddBanToDb(PlayerBan banInfo)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await GetBan(banInfo.Sid);
            if (ban != null)
                return false;
            
            var serverId = Config.BanOnAllServers ? "" : banInfo.ServerId;
            if (banInfo.ServerId == "-")
            {
                serverId = Config.ServerId;
            }
            if (Config.AllServersBanReasons.Contains(banInfo.Reason))
            {
                serverId = "";
            }
            banInfo.ServerId = serverId;
            
            // PreBan event
            if (PreBan != null)
            {
                foreach (IIksAdminApi.PreBanEventHandler handler in PreBan.GetInvocationList())
                {
                    HookResult result = await handler(banInfo);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Ban operation stopped by event handler.");
                        return false;
                    }
                }
            }
            
            await conn.QueryAsync("insert into iks_bans(name, sid, ip, adminsid, adminName, created, time, end, reason, BanType, server_id)" +
                                  "values (@name, @sid, @ip, @adminSid, @adminName, @created, @time, @end, @reason, @banType, @serverId)",
                                  new
                                  {
                                      name = banInfo.Name,
                                      sid = banInfo.Sid,
                                      ip = banInfo.Ip.Split(":")[0],
                                      adminSid = banInfo.AdminSid,
                                      adminName = banInfo.AdminName,
                                      created = banInfo.Created,
                                      time = banInfo.Time,
                                      end = banInfo.End,
                                      reason = banInfo.Reason,
                                      banType = banInfo.BanType,
                                      serverId
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

    public event IIksAdminApi.PreCommEventHandler? PreMute;
    public event IIksAdminApi.PreCommEventHandler? PreUnMute;

    public async Task<bool> AddMuteToDb(PlayerComm muteInfo)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await GetMute(muteInfo.Sid);
            if (ban != null)
                return false;
            
            var serverId = Config.BanOnAllServers ? "" : muteInfo.ServerId;
            
            if (muteInfo.ServerId == "-")
            {
                serverId = Config.ServerId;
            }
            muteInfo.ServerId = serverId;
            
            if (PreMute != null)
            {
                foreach (IIksAdminApi.PreCommEventHandler handler in PreMute.GetInvocationList())
                {
                    HookResult result = await handler(muteInfo);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Mute operation stopped by event handler.");
                        return false;
                    }
                }
            }
            await conn.QueryAsync("insert into iks_mutes(name, sid, adminsid, adminName, created, time, end, reason, server_id)" +
                                  "values (@name, @sid, @adminSid, @adminName, @created, @time, @end, @reason, @serverId)",
                new
                {
                    name = muteInfo.Name,
                    sid = muteInfo.Sid,
                    adminSid = muteInfo.AdminSid,
                    adminName = muteInfo.AdminName,
                    created = muteInfo.Created,
                    time = muteInfo.Time,
                    end = muteInfo.End,
                    reason = muteInfo.Reason,
                    serverId
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

    public event IIksAdminApi.PreCommEventHandler? PreGag;
    public event IIksAdminApi.PreCommEventHandler? PreUnGag;

    public async Task<bool> AddGagToDb(PlayerComm gagInfo)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await GetGag(gagInfo.Sid);
            if (ban != null)
                return false;
            var serverId = Config.BanOnAllServers ? "" : gagInfo.ServerId;
            if (gagInfo.ServerId == "-")
            {
                serverId = Config.ServerId;
            }

            gagInfo.ServerId = serverId;
            
            if (PreGag != null)
            {
                foreach (IIksAdminApi.PreCommEventHandler handler in PreGag.GetInvocationList())
                {
                    HookResult result = await handler(gagInfo);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Gag operation stopped by event handler.");
                        return false;
                    }
                }
            }
            await conn.QueryAsync("insert into iks_gags(name, sid, adminsid, adminName, created, time, end, reason, server_id)" +
                                  "values (@name, @sid, @adminSid, @adminName, @created, @time, @end, @reason, @serverId)",
                new
                {
                    name = gagInfo.Name,
                    sid = gagInfo.Sid,
                    adminSid = gagInfo.AdminSid,
                    adminName = gagInfo.AdminName,
                    created = gagInfo.Created,
                    time = gagInfo.Time,
                    end = gagInfo.End,
                    reason = gagInfo.Reason,
                    serverId
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

    public async Task<PlayerBan?> RemoveBan(string sid, string adminSid)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var ban = await GetBan(sid);
            if (ban == null)
                return null;
            ban.Unbanned = 1;
            ban.UnbannedBy = adminSid;
            if (PreUnBan != null)
            {
                foreach (IIksAdminApi.PreBanEventHandler handler in PreUnBan.GetInvocationList())
                {
                    var result = await handler(ban);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Un ban was stopped from handler");
                        return null;
                    }
                }
            }
            await conn.QueryAsync("update iks_bans set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = ban.Id, unbannedBy = adminSid });
            
            OnUnBan?.Invoke(ban);
            return ban;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public async Task<PlayerComm?> RemoveMute(string sid, string adminSid)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var mute = await GetMute(sid);
            if (mute == null)
                return null;
            mute.Unbanned = 1;
            mute.UnbannedBy = adminSid;
            if (PreUnMute != null)
            {
                foreach (IIksAdminApi.PreCommEventHandler handler in PreUnMute.GetInvocationList())
                {
                    var result = await handler(mute);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Un mute was stopped from handler");
                        return null;
                    }
                }
            }

            await conn.QueryAsync("update iks_mutes set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = mute.Id, unbannedBy = adminSid });
            
            var existingMute = OnlineMutedPlayers.FirstOrDefault(x => x.Sid == mute.Sid);
            if (existingMute != null)
            {
                OnlineMutedPlayers.Remove(existingMute);
            }
            
            OnUnMute?.Invoke(mute);
            await ReloadInfractions(sid, false);
            return mute;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public async Task<PlayerComm?> RemoveGag(string sid, string adminSid)
    {
        try
        {
            await using var conn = new MySqlConnection(DbConnectionString);
            await conn.OpenAsync();
            var gag = await GetGag(sid);
            if (gag == null)
                return null;
            gag.Unbanned = 1;
            gag.UnbannedBy = adminSid;

            if (PreUnGag != null)
            {
                foreach (IIksAdminApi.PreCommEventHandler handler in PreUnGag.GetInvocationList())
                {
                    var result = await handler(gag);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Un gag was stopped from handler");
                        return null;
                    }
                }
            }

            await conn.QueryAsync("update iks_gags set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = gag.Id, unbannedBy = adminSid });
            var existingGag = OnlineGaggedPlayers.FirstOrDefault(x => x.Sid == gag.Sid);
            if (existingGag != null)
            {
                OnlineGaggedPlayers.Remove(existingGag);
            }
            
            OnUnGag?.Invoke(gag);
            await ReloadInfractions(sid, false);
            return gag;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public event Action<string, IMenu, CCSPlayerController>? OnMenuOpen;

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
            
            var serverId = Config.BanOnAllServers ? "" : banInfo.ServerId;
            if (banInfo.ServerId == "-")
            {
                serverId = Config.ServerId;
            }
            if (Config.AllServersBanReasons.Contains(banInfo.Reason))
            {
                serverId = "";
            }
            banInfo.ServerId = serverId;
            
            // PreBan event
            if (PreBan != null)
            {
                foreach (IIksAdminApi.PreBanEventHandler handler in PreBan.GetInvocationList())
                {
                    HookResult result = await handler(banInfo);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Ban operation stopped by event handler.");
                        return false;
                    }
                }
            }
            
            await conn.QueryAsync("insert into iks_bans(name, sid, ip, adminsid, adminName, created, time, end, reason, BanType, server_id)" +
                                  "values (@name, @sid, @ip, @adminSid, @adminName, @created, @time, @end, @reason, @banType, @serverId)",
                                  new
                                  {
                                      name = banInfo.Name,
                                      sid = banInfo.Sid,
                                      ip = banInfo.Ip.Split(":")[0],
                                      adminSid = banInfo.AdminSid,
                                      adminName = banInfo.AdminName,
                                      created = banInfo.Created,
                                      time = banInfo.Time,
                                      end = banInfo.End,
                                      reason = banInfo.Reason,
                                      banType = banInfo.BanType,
                                      serverId
                                  });
            await ReloadInfractions(banInfo.Sid);
            OnAddBan?.Invoke(banInfo);
            BaseMessages.SERVER_BAN(banInfo);
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
            
            var serverId = Config.BanOnAllServers ? "" : muteInfo.ServerId;
            
            if (muteInfo.ServerId == "-")
            {
                serverId = Config.ServerId;
            }
            muteInfo.ServerId = serverId;
            
            if (PreMute != null)
            {
                foreach (IIksAdminApi.PreCommEventHandler handler in PreMute.GetInvocationList())
                {
                    HookResult result = await handler(muteInfo);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Mute operation stopped by event handler.");
                        return false;
                    }
                }
            }
            await conn.QueryAsync("insert into iks_mutes(name, sid, adminsid, adminName, created, time, end, reason, server_id)" +
                                  "values (@name, @sid, @adminSid, @adminName, @created, @time, @end, @reason, @serverId)",
                new
                {
                    name = muteInfo.Name,
                    sid = muteInfo.Sid,
                    adminSid = muteInfo.AdminSid,
                    adminName = muteInfo.AdminName,
                    created = muteInfo.Created,
                    time = muteInfo.Time,
                    end = muteInfo.End,
                    reason = muteInfo.Reason,
                    serverId
                });
            OnAddMute?.Invoke(muteInfo);
            await ReloadInfractions(muteInfo.Sid, false);
            BaseMessages.SERVER_MUTE(muteInfo);
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
            var serverId = Config.BanOnAllServers ? "" : gagInfo.ServerId;
            if (gagInfo.ServerId == "-")
            {
                serverId = Config.ServerId;
            }

            gagInfo.ServerId = serverId;
            
            if (PreGag != null)
            {
                foreach (IIksAdminApi.PreCommEventHandler handler in PreGag.GetInvocationList())
                {
                    HookResult result = await handler(gagInfo);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Gag operation stopped by event handler.");
                        return false;
                    }
                }
            }
            await conn.QueryAsync("insert into iks_gags(name, sid, adminsid, adminName, created, time, end, reason, server_id)" +
                                  "values (@name, @sid, @adminSid, @adminName, @created, @time, @end, @reason, @serverId)",
                new
                {
                    name = gagInfo.Name,
                    sid = gagInfo.Sid,
                    adminSid = gagInfo.AdminSid,
                    adminName = gagInfo.AdminName,
                    created = gagInfo.Created,
                    time = gagInfo.Time,
                    end = gagInfo.End,
                    reason = gagInfo.Reason,
                    serverId
                });
            OnAddGag?.Invoke(gagInfo);
            await ReloadInfractions(gagInfo.Sid, false);
            BaseMessages.SERVER_GAG(gagInfo);
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
            ban.Unbanned = 1;
            ban.UnbannedBy = adminSid;
            if (PreUnBan != null)
            {
                foreach (IIksAdminApi.PreBanEventHandler handler in PreUnBan.GetInvocationList())
                {
                    var result = await handler(ban);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Un ban was stopped from handler");
                        return null;
                    }
                }
            }
            await conn.QueryAsync("update iks_bans set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = ban.Id, unbannedBy = adminSid });
            
            OnUnBan?.Invoke(ban);
            BaseMessages.SERVER_UNBAN(ban);
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
            mute.Unbanned = 1;
            mute.UnbannedBy = adminSid;
            if (PreUnMute != null)
            {
                foreach (IIksAdminApi.PreCommEventHandler handler in PreUnMute.GetInvocationList())
                {
                    var result = await handler(mute);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Un mute was stopped from handler");
                        return null;
                    }
                }
            }

            await conn.QueryAsync("update iks_mutes set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = mute.Id, unbannedBy = adminSid });
            
            var existingMute = OnlineMutedPlayers.FirstOrDefault(x => x.Sid == mute.Sid);
            if (existingMute != null)
            {
                OnlineMutedPlayers.Remove(existingMute);
            }
            
            OnUnMute?.Invoke(mute);
            await ReloadInfractions(sid, false);
            BaseMessages.SERVER_UNMUTE(mute);
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
            gag.Unbanned = 1;
            gag.UnbannedBy = adminSid;

            if (PreUnGag != null)
            {
                foreach (IIksAdminApi.PreCommEventHandler handler in PreUnGag.GetInvocationList())
                {
                    var result = await handler(gag);
                    if (result is HookResult.Stop or HookResult.Handled)
                    {
                        Console.WriteLine("Un gag was stopped from handler");
                        return null;
                    }
                }
            }

            await conn.QueryAsync("update iks_gags set Unbanned = 1, UnbannedBy = @unbannedBy where id = @id",
                new { id = gag.Id, unbannedBy = adminSid });
            var existingGag = OnlineGaggedPlayers.FirstOrDefault(x => x.Sid == gag.Sid);
            if (existingGag != null)
            {
                OnlineGaggedPlayers.Remove(existingGag);
            }
            
            OnUnGag?.Invoke(gag);
            await ReloadInfractions(sid, false);
            BaseMessages.SERVER_UNGAG(gag);
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
            adminName as adminName,
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
            where (sid = @arg or (ip = @arg and BanType = 1)) and (end > @timeNow or time = 0) and Unbanned = 0 and (server_id = @server_id or server_id = '')
            ", new {arg, timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), server_id = Config.ServerId});
            
            if (ban != null)
            {
                if (ban.ServerId.Trim() != "" && ban.ServerId != Config.ServerId)
                {
                    return null;
                }
            }
            
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
            adminName as adminName,
            created as created,
            time as time,
            end as end,
            reason as reason,
            server_id as serverId,
            Unbanned as unbanned,
            UnbannedBy as unbannedBy,
            id as id
            from iks_mutes
            where sid = @sid and (end > @timeNow or time = 0) and Unbanned = 0 and (server_id = @server_id or server_id = '')
            ", new {sid, timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), server_id = Config.ServerId});

            if (mute != null)
            {
                if (mute.ServerId.Trim() != "" && mute.ServerId != Config.ServerId)
                {
                    return null;
                }
            }
            
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
            adminName as adminName,
            created as created,
            time as time,
            end as end,
            reason as reason,
            server_id as serverId,
            Unbanned as unbanned,
            UnbannedBy as unbannedBy,
            id as id
            from iks_gags
            where sid = @sid and (end > @timeNow or time = 0) and Unbanned = 0 and (server_id = @server_id or server_id = '')
            ", new {sid, timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), server_id = Config.ServerId});
            
            if (gag != null)
            {
                if (gag.ServerId.Trim() != "" && gag.ServerId != Config.ServerId)
                {
                    return null;
                }
            }
            
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

    [Obsolete]
    public IBaseMenu CreateMenu(CCSPlayerController caller, Action<CCSPlayerController, Admin?, IMenu> onOpen)
    {
        return new Menu(caller, onOpen);
    }
    public IBaseMenu CreateMenu(Action<CCSPlayerController, Admin?, IMenu> onOpen)
    {
        return new Menu(onOpen);
    }

    public void SendMessageToPlayer(CCSPlayerController? controller, string message, string? tag)
    {
        Server.NextFrame(() =>
        {
            var messagePrefix = tag == null ? Localizer["PluginTag"] : tag;
            if (tag == null && controller == null)
            {
                messagePrefix = "[IksAdmin]";
            }
            if (message.Trim() == "") return;
            foreach (var str in message.Split("\n"))
            {
                if (controller == null)
                {
                    Console.WriteLine($"{messagePrefix} {str}");
                }
                else
                {
                    controller.PrintToChat($" {messagePrefix} {str}");
                }
            }
        });
    }
    public void SendMessageToPlayer(CCSPlayerController? controller, string message)
    {
        Server.NextFrame(() =>
        {
            var messagePrefix = Localizer["PluginTag"].Value;
            if (controller == null)
            {
                messagePrefix = "[IksAdmin]";
            }
            if (message.Trim() == "") return;
            foreach (var str in message.Split("\n"))
            {
                if (controller == null)
                {
                    Console.WriteLine($"{messagePrefix} {str}");
                }
                else
                {
                    controller.PrintToChat($" {messagePrefix} {str}");
                }
            }
        });
    }

    public void SendMessageToAll(string message, string? tag)
    {
        Server.NextFrame(() =>
        {
            if (message.Trim() == "") return;
            var messagePrefix = tag == null ? Localizer["PluginTag"] : tag;
            foreach (var str in message.Split("\n"))
            {
                Server.PrintToChatAll($" {messagePrefix} {str}");
            }
        });
    }
    public void SendMessageToAll(string message)
    {
        Server.NextFrame(() =>
        {
            if (message.Trim() == "") return;
            var messagePrefix = Localizer["PluginTag"].Value;
            foreach (var str in message.Split("\n"))
            {
                Server.PrintToChatAll($" {messagePrefix} {str}");
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
