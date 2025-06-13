using CounterStrikeSharp.API.ValveConstants.Protobuf;

namespace IksAdminApi;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Localization;

public interface IIksAdminApi
{
    // GLOBALS ===
    public List<PlayerInfo> DisconnectedPlayers {get; set;}
    public List<PlayerComm> Comms {get; set;}
    public List<Warn> Warns {get; set;}

    public CoreConfig Config { get; set; }
    public IStringLocalizer Localizer { get; set; }
    public BasePlugin Plugin { get; set; } 
    public string ModuleDirectory { get; set; }
    public Dictionary<string, SortMenu[]> SortMenus { get; set; }
    public Admin ConsoleAdmin {get; set;}
    public Dictionary<ulong, Admin> ServerAdmins { get; }
    public List<Admin> AllAdmins { get; set; }
    public List<ServerModel> AllServers { get; set; }
    public List<AdminToServer> AdminsToServer {get; set;}
    public ServerModel ThisServer { get; set; }
    public List<Group> Groups {get; set;}
    public List<GroupLimitation> GroupLimitations {get; set;}
    public Dictionary<string, Dictionary<string, string>> RegistredPermissions { get; set; }
    public string DbConnectionString {get; set;}
    public Dictionary<CCSPlayerController, Action<string>> NextPlayerMessage {get;}
    public Task SendRconToAllServers(string command, bool ignoreSelf = false);
    public Task<string> SendRconToServer(ServerModel server, string command);
    public ServerModel? GetServerById(int serverId);
    public ServerModel? GetServerByIp(string ip);
    public Dictionary<string, List<CommandModel>> RegistredCommands {get; set;}
    public List<AdminModule> LoadedModules {get; set;}

    public List<Admin> HidenAdmins { get; set; }
    public void ReloadConfigs();
    // MENU ===
    /// <summary>
    /// Creates admin
    /// </summary>
    public Task<DBResult> CreateAdmin(Admin actioneer, Admin admin, int? serverId);
    public Task<DBResult> DeleteAdmin(Admin actioneer, Admin admin, bool announce = true);
    public Task<DBResult> UpdateAdmin(Admin actioneer, Admin admin);
    public Task<DBResult> UpdateAdmin(Admin actioneer, Admin admin, bool updateOnServers);
    public Task<List<Admin>> GetAdminsBySteamId(string steamId, bool ignoreDeleted = true);
    /// <summary>
    /// Adds server id for admin
    /// </summary>
    public Task AddServerIdToAdmin(int adminId, int? serverId);
    /// <summary>
    /// Removes server id from admin
    /// </summary>
    public Task RemoveServerIdFromAdmin(int adminId, int serverId);
    /// <summary>
    /// Removes all server ids from admin
    /// </summary>
    public Task RemoveServerIdsFromAdmin(int adminId);
    public IDynamicMenuOption CreateMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, MenuColors? color = null, bool disabled = false, string viewFlags = "*");
    public IDynamicMenu CreateMenu(string id, string title, MenuType? type = null, MenuColors titleColor = MenuColors.Default, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IDynamicMenu? backMenu = null);
    public void CloseMenu(CCSPlayerController player);
    // FUNC ===
    public void ApplyCommForPlayer(PlayerComm comm);
    public void RemoveCommFromPlayer(PlayerComm comm);
    public Task ReloadInfractions(string steamId, string? ip = null, bool instantlyKick = false);
    
    /// <summary>
    /// Reload data from db
    /// </summary>
    /// <param name="sendRcon">Send rcon command to all servers?</param>
    public Task ReloadDataFromDb(bool sendRcon = true);
    public Task<PlayerSummaries?> GetPlayerSummaries(ulong steamId);
    /// <summary>
    /// Automatically selects all players according to their identity: Name, #SteamID, @all...
    /// </summary>
    /// <param name="action"></param>
    /// <param name="blockedArgs">blocks some identity</param>
    /// <param name="acceptNullSteamIdPlayer">Will action be called if the identifier type is Steam Id and the player is null</param>
    public void DoActionWithIdentity(CCSPlayerController? actioneer, string identity, Action<CCSPlayerController?, IdentityType> action, string[]? blockedArgs = null, bool acceptNullSteamIdPlayer = false);
    public void DisconnectPlayer(CCSPlayerController player, string reason, bool instantly = false,
        string? customMessageTemplate = null, Admin? admin = null, string? customByAdminTemplate = null,
        NetworkDisconnectionReason? disconnectionReason = null);
    public bool CanDoActionWithPlayer(string callerId, string targetId);
    public void SetCommandInititalizer(string moduleName);
    public void ClearCommandInitializer();
    public void RegisterPermission(string key, string defaultFlags);
    public string GetCurrentPermissionFlags(string key);
    public string GetCurrentPermissionFlags(string[] keys);
    public Task RefreshAdmins();
    public Task RefreshAdminsOnAllServers();
    public void HookNextPlayerMessage(CCSPlayerController player, Action<string> action);
    public void RemoveNextPlayerMessageHook(CCSPlayerController player);
    public void AddNewCommand(
        string command,
        string description,
        string permission,
        string usage,
        Action<CCSPlayerController, List<string>, CommandInfo> onExecute,
        CommandUsage whoCanExecute = CommandUsage.CLIENT_AND_SERVER,
        string? tag = null,
        string? hasNotPermissionsMessage = null,
        int minArgs = 0
    );
    // DATABASE/PUNISHMENTS FUNC ===
    /// <summary>
    /// return statuses: 0 - banned, 1 - already banned, 2 - stopped by limitations, -1 - other
    /// </summary>
    public Task<DBResult> AddBan(PlayerBan ban, bool announce = true);
    /// <summary>
    /// return statuses: 0 - unbanned, 1 - ban not finded, 2 - admin haven't permission, -1 - other
    /// </summary>
    public Task<DBResult> Unban(Admin admin, string steamId, string? reason, bool announce = true);
    /// <summary>
    /// return statuses: 0 - unbanned, 1 - ban not finded, 2 - admin haven't permission, -1 - other
    /// </summary>
    public Task<DBResult> UnbanIp(Admin admin, string steamId, string? reason, bool announce = true);
    public Task<PlayerBan?> GetActiveBan(string steamId);
    public Task<List<PlayerBan>> GetAllBans(string steamId);
    public Task<PlayerBan?> GetActiveBanIp(string ip);
    public Task<List<PlayerBan>> GetAllIpBans(string ip);
    public Task<List<PlayerBan>> GetLastBans(int time);

    /// <summary>
    /// return statuses: 0 - OK!, 1 - already banned, 2 - stopped by limitations, -1 - other
    /// </summary>
    public Task<DBResult> AddComm(PlayerComm ban, bool announce = true);
    /// <summary>
    /// return statuses: 0 - OK!, 1 - ban not finded, 2 - admin haven't permission, -1 - other
    /// </summary>
    public Task<DBResult> UnComm(Admin admin, PlayerComm comm, bool announce = true);
    public Task<List<PlayerComm>> GetActiveComms(string steamId);
    public Task<List<PlayerComm>> GetAllComms(string steamId);
    public Task<List<PlayerComm>> GetLastComms(int time);
    // EVENTS ===
    public delegate HookResult MenuOpenHandler(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu);
    public event MenuOpenHandler MenuOpenPre;
    public event MenuOpenHandler MenuOpenPost;
    public delegate HookResult OptionRenderHandler(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option);
    public event OptionRenderHandler OptionRenderPre;
    public event OptionRenderHandler OptionRenderPost;
    public delegate HookResult OptionExecuted(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option);
    public event OptionExecuted OptionExecutedPre;
    public event OptionExecuted OptionExecutedPost;

    public delegate HookResult OnCommandUsed(CCSPlayerController? caller, List<string> args, CommandInfo info);
    public event OnCommandUsed? OnCommandUsedPre;
    public event OnCommandUsed? OnCommandUsedPost;

    #region Punishment handlers
    
    public delegate HookResult DynamicEvent(EventData data);
    public event DynamicEvent OnDynamicEvent;
    public HookResult InvokeDynamicEvent(EventData data);

    public delegate HookResult BanHandler(PlayerBan ban, ref bool announce);
    public event BanHandler OnBanPre;
    public event BanHandler OnBanPost;
    
    /// <summary>
    /// arg is SteamID or IP
    /// </summary>
    public delegate HookResult UnBanHandler(Admin admin, ref string arg, ref string? reason, ref bool announce); 
    public event UnBanHandler OnUnBanPre;
    public event UnBanHandler OnUnBanPost;
    public event UnBanHandler OnUnBanIpPre;
    public event UnBanHandler OnUnBanIpPost;
    public event Action<Admin, PlayerBan> SuccessUnban; 
    
    public delegate HookResult CommHandler(PlayerComm comm, ref bool announce); 
    public event CommHandler OnCommPre;
    public event CommHandler OnCommPost;
    
    public delegate HookResult UnCommHandler(Admin admin, ref string steamId, ref string? reason, ref bool announce); 
    public event UnCommHandler OnUnCommPre;
    public event UnCommHandler OnUnCommPost;
    public event Action<Admin, PlayerComm> SuccessUnComm; 

    #endregion
    
    public event Action OnReady;
    public void EOnModuleLoaded(AdminModule module);
    public void EOnModuleUnload(AdminModule module);
    public event Action<AdminModule> OnModuleUnload;
    public event Action<AdminModule> OnModuleLoaded;
    
    /// <summary>
    /// Срабатывает после ReloadInfractions при входе игрока
    /// </summary>
    public event Action<string, string> OnFullConnect;
    #region PlayersManage
    public void Kick(Admin admin, CCSPlayerController player, string reason, bool announce = true);
    public void Slay(Admin admin, CCSPlayerController target, bool announce = true);
    public void ChangeTeam(Admin admin, CCSPlayerController player, int team, bool announce = true);
    public void SwitchTeam(Admin admin, CCSPlayerController player, int team, bool announce = true);
    public void Respawn(Admin admin, CCSPlayerController player, bool announce = true);
    public void Rename(Admin admin, CCSPlayerController player, string name, bool announce = true);
    #endregion
    // Groups
    public Task<DBResult> CreateGroup(Group group);
    public Task<DBResult> UpdateGroup(Group group);
    public Task<DBResult> DeleteGroup(Group group);
    public Task<List<Group>> GetAllGroups();
    
    // Warns
    public Task<DBResult> CreateWarn(Warn warn, bool announce = true);
    public Task<DBResult> UpdateWarn(Warn warn);
    public Task<DBResult> DeleteWarn(Admin admin, Warn warn, bool announce = true);
    public Task<List<Warn>> GetAllWarns();
    public Task<List<Warn>> GetAllWarnsByAdmin(Admin admin);
    public Task<List<Warn>> GetAllWarnsForAdmin(Admin admin);
}
