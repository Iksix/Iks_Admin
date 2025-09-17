using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using MenuManager;
using IksAdminApi;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin.Functions;
using CounterStrikeSharp.API;
using IksAdmin.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using IksAdmin.Menus;
using CoreConfig = IksAdminApi.CoreConfig;
namespace IksAdmin;

public class Main : BasePlugin
{
    public override string ModuleName => "IksAdmin";
    public override string ModuleVersion => "3.0 v19";
    public override string ModuleAuthor => "iks [Discord: iks__]";

    public static IMenuApi MenuApi = null!;
    private static readonly PluginCapability<IMenuApi?> MenuCapability = new("menu:nfcore");   
    public static AdminApi AdminApi = null!;
    private readonly PluginCapability<IIksAdminApi> _pluginCapability  = new("iksadmin:core");
    
    public static List<CCSPlayerController> BlockTeamChange = new();
    public static Dictionary<string, bool> KickOnFullConnect = new();
    public static Dictionary<string, string> KickOnFullConnectReason = new();

    // INSTANT PUNISHMENT ON CONNECT
    public static Dictionary<string, PlayerComm> InstantComm = new();
    public static List<PlayerInfo> LastClientVoices = new();
    public static Dictionary<string, int> LastClientVoicesTime = new();



    public static string MenuId(string id)
    {
        return $"iksadmin:menu:{id}";
    }
    public static string GenerateOptionId(string id)
    {
        return $"iksadmin:option:{id}";
    }
    public override void Load(bool hotReload)
    {
        AdminUtils.CoreInstance = this;
        AdminApi = new AdminApi(this, Localizer, ModuleDirectory);
        AdminModule.Api = AdminApi;
        AdminUtils.CoreApi = AdminApi;
        AdminApi.OnModuleLoaded += OnModuleLoaded;
        AdminApi.OnModuleUnload += OnModuleUnload;
        Capabilities.RegisterPluginCapability(_pluginCapability, () => AdminApi);
        Admin.GetCurrentFlagsFunc = UtilsFunctions.GetCurrentFlagsFunc;
        Admin.GetCurrentImmunityFunc = UtilsFunctions.GetCurrentImmunityFunc;
        AdminUtils.GetGroupFromIdFunc = UtilsFunctions.GetGroupFromIdFunc;
        AdminUtils.FindAdminByControllerMethod = UtilsFunctions.FindAdminByControllerMethod;
        AdminUtils.FindAdminByIdMethod = UtilsFunctions.FindAdminByIdMethod;
        AdminUtils.GetPremissions = UtilsFunctions.GetPermissions;
        AdminUtils.GetConfigMethod = UtilsFunctions.GetConfigMethod;
        AdminApi.ReloadConfigs();
        Helper.SetSortMenus();
        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSay);
        AddCommandListener("jointeam", OnJoinTeam);
        InitializePermissions();
        InitializeCommands();
        RegisterListener<Listeners.OnTick>(() =>
        {
            MessageOnTick();
        });
        RegisterListener<Listeners.OnClientAuthorized>(OnAuthorized);
        RegisterListener<Listeners.OnClientVoice>(OnClientVoice);
        AddTimer(5, () => {
            foreach (var comm in AdminApi.Comms.ToArray())
            {
                if (comm.EndAt != 0 && comm.EndAt < AdminUtils.CurrentTimestamp()) { 
                    AdminApi.RemoveCommFromPlayer(comm);
                }
            }
            foreach (var warn in AdminApi.Warns.ToArray())
            {
                if (warn.EndAt != 0 && warn.EndAt < AdminUtils.CurrentTimestamp()) { 
                    var admin = warn.TargetAdmin;
                    if (admin != null && admin.Controller != null) {
                        admin.Controller.Print(
                            Localizer["Message.WarnEnded"].AReplace(["id", "reason", "admin"], [warn.Id, warn.Reason, warn.Admin?.Name ?? "NOT FINDED"])
                        );
                    }
                    AdminApi.Warns.Remove(warn);
                }
            }
        }, TimerFlags.REPEAT);
    }

    private void OnClientVoice(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        if (player == null || player.AuthorizedSteamID == null) return;
        var info = new PlayerInfo(player);

        LastClientVoices.Remove(LastClientVoices.FirstOrDefault(x => x.SteamId == player.GetSteamId())!);
        LastClientVoices.Insert(0, info);
        LastClientVoicesTime.Remove(player.GetSteamId());
        LastClientVoicesTime.Add(player.GetSteamId(), AdminUtils.CurrentTimestamp());

        if (LastClientVoices.Count() > 10)
        {
            var v = LastClientVoices[LastClientVoices.Count() - 1];
            LastClientVoices.RemoveAt(LastClientVoices.Count() - 1);
            LastClientVoicesTime.Remove(v.SteamId!);
        }
        var mute = player.GetComms().GetMute();
        var silence = player.GetComms().GetSilence();
        if (mute != null)
        {
            player.Print(Localizer["Message.Muted"].AReplace(["time"], [Utils.GetDateString(mute.EndAt)]));
        }
        if (silence != null)
        {
            player.Print(Localizer["Message.Muted"].AReplace(["time"], [Utils.GetDateString(silence.EndAt)]));
        }
    }

    public static void MessageOnTick()
    {
        foreach (var msg in PlayersUtils.HtmlMessages)
        {
            var player = msg.Key;
            var message = msg.Value;
            if (player == null || !player.IsValid || player.IsBot) continue;
            if (message == "") continue;
            player.PrintToCenterHtml(message);
        }
    }

    private void OnAuthorized(int playerSlot, SteamID steamId)
    {
        var steamId64 = steamId.SteamId64.ToString();
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        var disconnected = AdminApi.DisconnectedPlayers.FirstOrDefault(x => x.SteamId == steamId64);
        AdminApi.DisconnectedPlayers.Remove(disconnected!);
        var ip = player!.GetIp();
        var comms = player!.GetComms();
        foreach (var comm in comms)
        {
            AdminApi.Comms.Remove(comm);
        }
        Task.Run(async () => {
            await AdminApi.ReloadInfractions(steamId64, ip, true);
            Server.NextWorldUpdate(() =>
            {
                AdminApi.OnFullConnectInvoke(steamId64, ip ?? "");
                var admin = player.Admin();
                if (admin == null) return;
                if (CoreConfig.Config.AutoUpdateDatabaseNames)
                {
                    var name = player!.PlayerName;
                    admin.Name = name;
                    Task.Run(async () => {
                        await AdminApi.UpdateAdmin(AdminApi.ConsoleAdmin, admin, false);
                    });
                }
                if (admin.Warns.Count >= AdminApi.Config.MaxWarns)
                {
                    player.Print(Localizer["ActionError.DisabledByWarns"]);
                }

                if (admin.HasPermissions("other.cs_votekick_immunity"))
                {
                    player!.CannotBeKicked = true;
                }
            });
        });
    }

    private HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (BlockTeamChange.Contains(player!)) return HookResult.Stop;
        return HookResult.Continue;
    }

    private void OnModuleLoaded(AdminModule module)
    {
        AdminApi.LoadedModules.Add(module);
    }

    private void OnModuleUnload(AdminModule module)
    {
        foreach (var commands in AdminApi.RegistredCommands)
        {
            if (commands.Key != module.ModuleName) continue;
            foreach (var command in commands.Value)
            {
                Console.WriteLine($"Removing command from {module.ModuleName} [{command.Command}]");
                CommandManager.RemoveCommand(command.Definition);
            }
            AdminApi.RegistredCommands.Remove(commands.Key);
        }
        AdminApi.LoadedModules.Remove(module);
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) return HookResult.Continue;
        bool toTeam = commandInfo.GetArg(0) == "say_team";
        var msg = commandInfo.GetCommandString;
        if (toTeam)
        {
            msg = msg.Remove(0, 9);
        } else {
            msg = msg.Remove(0, 4);
        }
        if (msg.StartsWith("\""))
        {
            msg = msg.Remove(0, 1);
            msg = msg.Remove(msg.Length - 1, 1);
        }
        AdminUtils.LogDebug($"{player.PlayerName} message: {msg}");
        if (msg.StartsWith("!") || msg.StartsWith("/")) {
            if (AdminApi.NextPlayerMessage.ContainsKey(player))
            {
                AdminUtils.LogDebug("Next player message: " + msg);
                AdminApi.NextPlayerMessage[player].Invoke(msg.Remove(0, 1));
                AdminApi.RemoveNextPlayerMessageHook(player);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        }
        
        var comm = player.GetComms();
        if (comm.HasGag())
        {
            var gag = comm.GetGag()!;
            Helper.Print(player, Localizer["Message.WhenGag"].Value
                .Replace("{date}", gag.EndAt == 0 ? Localizer["Other.Never"] : Utils.GetDateString(gag.EndAt))
            );
            return HookResult.Stop;
        }
        if (comm.HasSilence())
        {
            var silence = comm.GetSilence()!;
            Helper.Print(player, Localizer["Message.WhenSilence"].Value
                .Replace("{date}", silence.EndAt == 0 ? Localizer["Other.Never"] : Utils.GetDateString(silence.EndAt))
            );
            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private void InitializePermissions()
    {
        // Admin manage ===
        AdminApi.RegisterPermission("admins_manage.add", "z");
        AdminApi.RegisterPermission("admins_manage.delete", "z");
        AdminApi.RegisterPermission("admins_manage.edit", "z");
        AdminApi.RegisterPermission("admins_manage.list", "z");
        // Admin manage ===
        AdminApi.RegisterPermission("admins_manage.warn_add", "z");
        AdminApi.RegisterPermission("admins_manage.warn_delete", "z");
        AdminApi.RegisterPermission("admins_manage.warn_list", "z");
        // Groups manage ===
        AdminApi.RegisterPermission("groups_manage.add", "z");
        AdminApi.RegisterPermission("groups_manage.delete", "z");
        AdminApi.RegisterPermission("groups_manage.edit", "z");
        AdminApi.RegisterPermission("groups_manage.refresh", "z");

        // BAN ===
        AdminApi.RegisterPermission("blocks_manage.ban", "b");
        AdminApi.RegisterPermission("blocks_manage.own_ban_reason", "b"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины и возможность банить по кастомной причине через команду
        AdminApi.RegisterPermission("blocks_manage.own_ban_time", "b"); // С этим флагом у админа появляется пункт в меню для выбора собственного времени и банить через команду с сообственным временем
        AdminApi.RegisterPermission("blocks_manage.ban_ip", "b");
        AdminApi.RegisterPermission("blocks_manage.unban", "b");
        AdminApi.RegisterPermission("blocks_manage.unban_ip", "b");

        // MUTE
        AdminApi.RegisterPermission("comms_manage.mute", "m"); 
        AdminApi.RegisterPermission("comms_manage.unmute", "m"); 
        AdminApi.RegisterPermission("comms_manage.own_mute_reason", "m"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины
        AdminApi.RegisterPermission("comms_manage.own_mute_time", "m"); 
        // SILENCE
        AdminApi.RegisterPermission("comms_manage.silence", "mg"); 
        AdminApi.RegisterPermission("comms_manage.unsilence", "mg"); 
        AdminApi.RegisterPermission("comms_manage.own_silence_reason", "mg"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины
        AdminApi.RegisterPermission("comms_manage.own_silence_time", "mg"); 
        // GAG
        AdminApi.RegisterPermission("comms_manage.gag", "g"); 
        AdminApi.RegisterPermission("comms_manage.ungag", "g"); 
        AdminApi.RegisterPermission("comms_manage.own_gag_reason", "g"); // С этим флагом у админа появляется пункт в меню для выбора собственной причины
        AdminApi.RegisterPermission("comms_manage.own_gag_time", "g"); 
        // OTHER
        AdminApi.RegisterPermission("blocks_manage.remove_immunity", "i"); // Снять наказание выданное админом ниже по иммунитету
        AdminApi.RegisterPermission("blocks_manage.remove_all", "u"); // Снять наказание выданное кем угодно (кроме консоли)
        AdminApi.RegisterPermission("blocks_manage.remove_console", "c"); // Снять наказание выданное консолью
        // Players manage ===
        AdminApi.RegisterPermission("players_manage.kick", "k");
        AdminApi.RegisterPermission("players_manage.kick_own_reason", "k");
        AdminApi.RegisterPermission("players_manage.changeteam", "k");
        AdminApi.RegisterPermission("players_manage.switchteam", "k");
        AdminApi.RegisterPermission("players_manage.slay", "k");
        AdminApi.RegisterPermission("players_manage.respawn", "k");
        AdminApi.RegisterPermission("players_manage.rename", "k");
        AdminApi.RegisterPermission("players_manage.who", "b");
        // SERVERS MANAGE === 
        AdminApi.RegisterPermission("servers_manage.reload_data", "z");
        AdminApi.RegisterPermission("servers_manage.rcon", "z");
        AdminApi.RegisterPermission("servers_manage.list", "z");
        AdminApi.RegisterPermission("servers_manage.config", "z");
        // Other ===
        AdminApi.RegisterPermission("other.equals_immunity_action", "e"); // Разрешить взаймодействие с админами равными по иммунитету (Включая снятие наказаний если есть флаг blocks_manage.remove_immunity)
        AdminApi.RegisterPermission("other.reload_infractions", "z");
        AdminApi.RegisterPermission("other.cs_votekick_immunity", "b");
        AdminApi.RegisterPermission("other.hide", "b");
        AdminApi.RegisterPermission("other.status", "*");
        AdminApi.RegisterPermission("other.lvoices", "b");
    }
    private void InitializeCommands()
    {
        AdminApi.SetCommandInititalizer(ModuleName);
        #region ServersManage

        AdminApi.AddNewCommand(
            "am_servers",
            "Выводит список серверов",
            "servers_manage.list",
            "css_am_servers",
            CmdSm.Servers,
            minArgs: 0,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "am_config_reload",
            "Перезагружает все конфиги",
            "servers_manage.config",
            "css_am_config_reload",
            CmdSm.ConfigReload,
            minArgs: 0,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "rcon",
            "Отправить ркон команду",
            "servers_manage.rcon",
            "css_rcon <ServerID> <CMD>",
            CmdSm.Rcon,
            minArgs: 2,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );

        #endregion
        AdminApi.AddNewCommand(
            "status",
            "Выводит список игроков",
            "other.status",
            "css_status [json] [offline]",
            CmdBase.Status,
            minArgs: 0,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "lvoices",
            "Список последних говоривших игроков",
            "other.lvoices",
            "css_lvoices",
            CmdBase.LVoices,
            minArgs: 0,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "hide",
            "Скрывает админа в табе",
            "other.hide",
            "css_hide",
            CmdBase.Hide!,
            minArgs: 0,
            whoCanExecute: CommandUsage.CLIENT_ONLY
        );
        AdminApi.AddNewCommand(
            "reload_infractions",
            "Перезагрузить данные игрока",
            "other.reload_infractions",
            "css_reload_infractions <SteamID/IP(WITHOUT PORT)>",
            CmdBase.ReloadInfractions,
            minArgs: 1,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "am_warn",
            "Выдать варн",
            "admins_manage.warn_add",
            "css_am_warn <SteamID> <time> <reason>",
            CmdAdminManage.Warn,
            minArgs: 3,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "am_warns",
            "Выводит все варны админа",
            "admins_manage.warn_list",
            "css_am_warns <Admin ID>",
            CmdAdminManage.Warns,
            minArgs: 1,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "am_list",
            "Выводит всех админов",
            "admins_manage.list",
            "css_am_list [all]",
            CmdAdminManage.List,
            minArgs: 0,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "am_warn_remove",
            "Выводит все варны админа",
            "admins_manage.warn_delete",
            "css_am_warn_remove <Warn ID>",
            CmdAdminManage.WarnRemove,
            minArgs: 1,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "admin",
            "Открыть админ меню",
            ">*",
            "css_admin",
            CmdBase.AdminMenu,
            minArgs: 0,
            whoCanExecute: CommandUsage.CLIENT_ONLY
        );
        AdminApi.AddNewCommand(
            "am_reload",
            "Перезагружает данные с БД",
            "servers_manage.reload_data",
            "css_am_reload [all]",
            CmdBase.Reload,
            minArgs: 0,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "am_add",
            "Создать админа",
            "admins_manage.add",
            "css_am_add <steamId> <name> <time/0> <server_id/this/all> <groupName>\n" +
            "css_am_add <steamId> <name> <time/0> <server_id/this/all> <flags> <immunity>",
            CmdAdminManage.Add,
            minArgs: 5 
        );
        AdminApi.AddNewCommand(
            "am_add_server_id",
            "Добавить Server Id админу",
            "admins_manage.add",
            "css_am_add_server_id <AdminID> <server_id/this>",
            CmdAdminManage.AddServerId,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "am_addflag",
            "Добавить флаг админу",
            "admins_manage.edit",
            "css_am_addflag <steamId> <flagsToAdd>",
            CmdAdminManage.AddFlag,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "am_addflag_or_admin",
            "Добавить флаг админу или создать админа(В случае если такого админа нет)",
            "admins_manage.edit,admins_manage.add",
            "css_am_addflag_or_admin <steamId> <name> <time/0> <server_id/this> <flags> <immunity>",
            CmdAdminManage.AddFlagOrAdmin,
            minArgs: 6
        );
        AdminApi.AddNewCommand(
            "am_remove",
            "Удалить админа",
            "admins_manage.delete",
            "css_am_remove <id>",
            CmdAdminManage.RemoveAdmin,
            minArgs: 1
        );

        // BLOCKS MANAGE ====
        // BANS ===
        AdminApi.AddNewCommand(
            "ban",
            "Забанить игрока",
            "blocks_manage.ban",
            "css_ban <#uid/#steamId/name/@...> <time> <reason>",
            CmdBans.Ban,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "unban",
            "Разбанить игрока",
            "blocks_manage.unban",
            "css_unban <steamId> <reason>",
            CmdBans.Unban,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "unbanip",
            "Разбанить игрока",
            "blocks_manage.unban_ip",
            "css_unbanip <ip> <reason>",
            CmdBans.UnbanIp,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addban",
            "Забанить игрока по стим айди (оффлайн)",
            "blocks_manage.ban",
            "css_addban <steamId> <time> <reason>",
            CmdBans.AddBan,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "banip",
            "Забанить по айпи (онлайн)",
            "blocks_manage.ban_ip",
            "css_banip <#uid/#steamId/name/@...> <time> <reason>",
            CmdBans.BanIp,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "addbanip",
            "Забанить игрока по айпи (оффлайн)",
            "blocks_manage.ban_ip",
            "css_addbanip <ip> <time> <reason>",
            CmdBans.AddBanIp,
            minArgs: 3 
        );
        // GAG ===
        AdminApi.AddNewCommand(
            "gag",
            "Выдать гаг игроку (онлайн)",
            "comms_manage.gag",
            "css_gag <#uid/#steamId/name/@...> <time> <reason>",
            CmdGags.Gag,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "ungag",
            "Снять гаг с игрока (онлайн)",
            "comms_manage.ungag",
            "css_ungag <#uid/#steamId/name/@...> <reason>",
            CmdGags.Ungag,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addgag",
            "Выдать гаг игроку (оффлайн)",
            "comms_manage.gag",
            "css_addgag <steamId> <time> <reason>",
            CmdGags.AddGag,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "removegag",
            "Снять гаг с игрока (оффлайн)",
            "comms_manage.ungag",
            "css_removegag <steamId> <reason>",
            CmdGags.RemoveGag,
            minArgs: 2 
        );
        // MUTE ===
        AdminApi.AddNewCommand(
            "mute",
            "Выдать мут игроку (онлайн)",
            "comms_manage.mute",
            "css_mute <#uid/#steamId/name/@...> <time> <reason>",
            CmdMutes.Mute,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "unmute",
            "Снять мут с игрока (онлайн)",
            "comms_manage.unmute",
            "css_unmute <#uid/#steamId/name/@...> <reason>",
            CmdMutes.Unmute,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addmute",
            "Выдать мут игроку (оффлайн)",
            "comms_manage.mute",
            "css_addmute <steamId> <time> <reason>",
            CmdMutes.AddMute,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "removemute",
            "Снять мут с игрока (оффлайн)",
            "comms_manage.unmute",
            "css_removemute <steamId> <reason>",
            CmdMutes.RemoveMute,
            minArgs: 2 
        );
        // Silence ===
        AdminApi.AddNewCommand(
            "silence",
            "Выдать silence игроку (онлайн)",
            "comms_manage.silence",
            "css_silence <#uid/#steamId/name/@...> <time> <reason>",
            CmdSilences.Silence,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "unsilence",
            "Снять silence с игрока (онлайн)",
            "comms_manage.unsilence",
            "css_unsilence <#uid/#steamId/name/@...> <reason>",
            CmdSilences.UnSilence,
            minArgs: 2 
        );
        AdminApi.AddNewCommand(
            "addsilence",
            "Выдать silence игроку (оффлайн)",
            "comms_manage.silence",
            "css_addsilence <steamId> <time> <reason>",
            CmdSilences.AddSilence,
            minArgs: 3 
        );
        AdminApi.AddNewCommand(
            "removesilence",
            "Снять silence с игрока (оффлайн)",
            "comms_manage.unsilence",
            "css_removesilence <steamId> <reason>",
            CmdSilences.RemoveSilence,
            minArgs: 2 
        );
        // RCommands
        AdminApi.AddNewCommand(
            "rban",
            "",
            "servers_manage.rcon",
            "css_rban <steamId(admin)> <steamId(target)> <ip/-> <time> <type(0/1/2)> <reason> <announce(true/false)>",
            CmdBansCmdRCommands.RBan,
            minArgs: 7,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "rcomm",
            "",
            "servers_manage.rcon",
            "css_rcomm <steamId(admin)> <steamId(target)> <ip/-> <time> <type(0/1/2)> <reason> <announce(true/false)>",
            CmdBansCmdRCommands.RComm,
            minArgs: 7,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        // PlayersManage
        AdminApi.AddNewCommand(
            "kick",
            "Кикнуть игрока",
            "players_manage.kick",
            "css_kick <#uid/#steamId/name/@...> <reason>",
            CmdPm.Kick,
            minArgs: 2,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "rename",
            "Переименовать игрока",
            "players_manage.rename",
            "css_rename <#uid/#steamId/name/@...> <new name>",
            CmdPm.Rename,
            minArgs: 2,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "respawn",
            "Возродить игрока",
            "players_manage.respawn",
            "css_respawn <#uid/#steamId/name/@...>",
            CmdPm.Respawn,
            minArgs: 1,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "slay",
            "Убить игрока",
            "players_manage.slay",
            "css_slay <#uid/#steamId/name/@...>",
            CmdPm.Slay,
            minArgs: 1,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "changeteam",
            "Сменить команду игрока(с убийством)",
            "players_manage.changeteam",
            "css_changeteam <#uid/#steamId/name/@...> <ct/t/spec>",
            CmdPm.ChangeTeam,
            minArgs: 2,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "switchteam",
            "Сменить команду игрока(без убийства)",
            "players_manage.switchteam",
            "css_switchteam <#uid/#steamId/name/@...> <ct/t/spec>",
            CmdPm.SwitchTeam,
            minArgs: 2,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.AddNewCommand(
            "who",
            "Просмотреть информацию об игроке",
            "players_manage.who",
            "css_who <#uid/#steamId/name/@...>",
            CmdBase.Who,
            minArgs: 1,
            whoCanExecute: CommandUsage.CLIENT_AND_SERVER
        );
        AdminApi.ClearCommandInitializer();
    }
    
    [GameEventHandler(HookMode.Pre)]
    public HookResult OnChangeTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot || !CmdBase.HidenPlayers.Contains(player))
        {
            return HookResult.Continue;
        }
        @event.Silent = true;
        if (CmdBase.FirstMessage.Contains(player))
        {
            CmdBase.FirstMessage.Remove(player);
            return HookResult.Continue;
        }
        CmdBase.HidenPlayers.Remove(player);
        AdminApi.HidenAdmins.Remove(player.Admin()!);
        player.Print(Localizer["Message.Hide_off"]);
        return HookResult.Continue;
    }
    
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        try
        {
            MenuApi = MenuCapability.Get()!;
            if (MenuApi == null)
            {
                AdminUtils.LogDebug("Start without Menu Manager");
            }
        }
        catch (Exception)
        {
            AdminUtils.LogDebug("Start without Menu Manager");
        }
        
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        foreach (var action in MenuPM.OnRoundEndChangeTeam.ToList())
        {
            action.Value.Invoke();
            MenuPM.OnRoundEndChangeTeam.Remove(action.Key);
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        if (player.IsBot) return HookResult.Continue;
        if (player.AuthorizedSteamID == null) return HookResult.Continue;
        var steamId = player.AuthorizedSteamID!.SteamId64.ToString();
        if (KickOnFullConnect.ContainsKey(steamId))
        {
            var reason = KickOnFullConnectReason[steamId];
            bool instantlyKick = true;
            AdminApi.DisconnectPlayer(player, reason, instantlyKick, disconnectedBy: "ban");
            return HookResult.Continue;
        }
        if (InstantComm.TryGetValue(steamId, out var comm)) {
            AdminApi.ApplyCommForPlayer(comm);
            InstantComm.Remove(steamId);
        }
        return HookResult.Continue;
    }
    
    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        BlockTeamChange.Remove(player!);
        PlayersUtils.ClearHtmlMessage(player!);
        MenuPM.OnRoundEndChangeTeam.Remove(player!.Slot);
        if (player == null || player.IsBot || player.AuthorizedSteamID == null) return HookResult.Continue;
        AdminApi.DisconnectedPlayers.Insert(0, new PlayerInfo(player));
        KickOnFullConnect.Remove(player.GetSteamId());
        LastClientVoicesTime.Remove(player.GetSteamId());
        LastClientVoices.Remove(LastClientVoices.FirstOrDefault(x => x.SteamId == player.GetSteamId())!);
        KickOnFullConnectReason.Remove(player.GetSteamId());
        CmdBase.HidenPlayers.Remove(player);
        AdminApi.HidenAdmins.Remove(player.Admin()!);
        var comms = player.GetComms();
        foreach (var comm in comms)
        {
            AdminApi.Comms.Remove(comm);
        }
        return HookResult.Continue;
    }
    
    public override void Unload(bool hotReload)
    {
        RemoveCommandListener("say", OnSay, HookMode.Pre);
        RemoveCommandListener("say_team", OnSay, HookMode.Pre);
        foreach (var commands in AdminApi.RegistredCommands)
        {
            if (commands.Key != ModuleName) continue;
            foreach (var command in commands.Value)
            {
                CommandManager.RemoveCommand(command.Definition);
            }
        }
    }
}