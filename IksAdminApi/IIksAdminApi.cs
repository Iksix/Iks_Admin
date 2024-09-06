using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using MenuManager;
using Microsoft.Extensions.Localization;

namespace IksAdminApi;

public interface IIksAdminApi
{
    public enum UsedMenuType
    {
        Chat,
        Html,
        Button
    }
    
    public IPluginCfg Config { get; set; }
    // Fields
    public List<AdminMenuOption> ModulesOptions { get; set; }
    public List<PlayerComm> OnlineMutedPlayers { get; set; }
    public List<PlayerComm> OnlineGaggedPlayers { get; set; }
    public Dictionary<string, PlayerInfo> DisconnectedPlayers { get; set; } // steamid -> info
    public MenuType MenuType { get; set; }
    public string DbConnectionString { get; set; }
    public List<Admin> AllAdmins { get; set; }
    public List<Admin> ThisServerAdmins { get; set; }
    public Dictionary<CCSPlayerController, Admin> TimeAdmins { get; set; }
    public IStringLocalizer Localizer { get; set; }
    public BasePlugin Plugin { get; }
    
    // Functions
    public Task AddGroup(Group group);
    public Task<Group?> GetGroup(string name);
    public Task DeleteGroup(string name);
    [Obsolete]
    public IBaseMenu CreateMenu(CCSPlayerController caller, Action<CCSPlayerController, Admin?, IMenu> onOpen);
    public void SendMessageToPlayer(CCSPlayerController? controller, string message);
    public void SendMessageToAll(string message);
    public Task ReloadInfractions(string sid, bool checkBan = true);
    

    
    public Task<PlayerBan?> GetBan(string arg);
    public Task<PlayerComm?> GetMute(string sid);
    public Task<PlayerComm?> GetGag(string sid);
    public Task AddAdmin(Admin admin);
    public Task<bool> DelAdmin(string sid);
    public Task<Admin?> GetAdmin(string sid);
    public Task<List<Admin>> GetAllAdmins();
    public Task<List<Group>> GetAllGroups();
    public Task ReloadAdmins();

    public void AddNewCommand(string command, string description, string commandUsage, int minArgs, string flagAccess,
        string flagDefault, CommandUsage whoCanExecute,
        Action<CCSPlayerController, Admin?, List<string>, CommandInfo> onCommandExecute);

    public bool HasAccess(string adminSid, CommandUsage commandUsage, string flagsAccess,
        string flagsDefault);
    
    [Obsolete]
    public bool HasPermisions(string adminSid, string flagsAccess, string flagsDefault);
    public bool HasMoreImmunity(string adminSid, string targetSid);
    public void ConvertAll();

    /// <summary>
    /// Identities: #uid/#sid/name
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    public CCSPlayerController? GetPlayerFromIdentity(string identity);
    
    // Events
    event Action<string, IMenu, CCSPlayerController> OnMenuOpen; // menu index -> menu -> player
    event Action<Admin> OnAddAdmin;
    event Action<Admin> OnDelAdmin;
    event Action<PlayerBan> OnAddBan;
    event Action<PlayerComm> OnAddMute;
    event Action<PlayerComm> OnAddGag;
    event Action<PlayerBan> OnUnBan;
    event Action<PlayerComm> OnUnMute;
    event Action<PlayerComm> OnUnGag;

    event Action<string, PlayerInfo, string> OnKick; // AdminSid -> Target info
    event Action<string, PlayerInfo, string, string> OnRename; // AdminSid -> Target info -> OldName -> newName
    event Action<string, PlayerInfo> OnSlay; // AdminSid -> Target info
    event Action<string, PlayerInfo, CsTeam, CsTeam> OnSwitchTeam; // AdminSid -> Target Info -> Old Team -> New Team 
    event Action<string, PlayerInfo, CsTeam, CsTeam> OnChangeTeam; // AdminSid -> Target Info -> Old Team -> New Team 
    event Action<string, Map> OnChangeMap;
    event Action<List<Admin>> OnReloadAdmins;
    event Action<CCSPlayerController?, CommandInfo> OnCommandUsed;
    
    /// <summary>
    /// Player, Ban, Mute, Gag
    /// </summary>
    event Action<PlayerInfo, Admin?, PlayerBan?, PlayerComm?, PlayerComm?> OnPlayerConnected; // info -> ban -> mute -> gag

    #region Event Calling
    
    public delegate Task<HookResult> PreBanEventHandler(PlayerBan info);
    public event PreBanEventHandler PreBan;
    public event PreBanEventHandler PreUnBan;
    public delegate Task<HookResult> PreCommEventHandler(PlayerComm info);
    public event PreCommEventHandler PreMute;
    public event PreCommEventHandler PreUnMute;
    public event PreCommEventHandler PreGag;
    public event PreCommEventHandler PreUnGag;


    public void EKick(string adminSid, PlayerInfo target, string reason);
    public void ERename(string adminSid, PlayerInfo target, string oldName, string newName);
    public void ESlay(string adminSid, PlayerInfo target);
    public void ESwitchTeam(string adminSid, PlayerInfo target, CsTeam oldTeam, CsTeam newTeam);
    public void EChangeTeam(string adminSid, PlayerInfo target, CsTeam oldTeam, CsTeam newTeam);
    public void EChangeMap(string adminSid, Map newMap);

    #endregion
    

    #region Use for do a plugin

    public void ReplyToCommand(CommandInfo info, string reply, string? replyToConsole = null, string? customTag = null);
    public bool HasPermissions(string adminSid, string flagsAccess, string flagsDefault);
    public List<Admin> GetThisServerAdmins();
    public Admin? GetAdminBySid(string steamId);
    public Admin? GetAdmin(CCSPlayerController player);
    public Admin? GetAdmin(ulong steamId);
    public Dictionary<CCSPlayerController, Admin> GetOnlineAdmins();
    public IBaseMenu CreateMenu(Action<CCSPlayerController, Admin?, IMenu> onOpen);
    public void SendMessageToPlayer(CCSPlayerController? controller, string message, string? tag);
    public void SendMessageToAll(string message, string? tag);
    public Dictionary<CCSPlayerController, Action<string>> NextCommandAction { get; set; }


    public void EOnMenuOpen(string index, IMenu menu, CCSPlayerController player);
    #region Punishments without chat message
    
    public Task<bool> AddBanToDb(PlayerBan banInfo); 
    public Task<bool> AddMuteToDb(PlayerComm muteInfo); 
    public Task<bool> AddGagToDb(PlayerComm gagInfo);

    public Task<PlayerBan?> RemoveBan(string sid, string adminSid); 

    public Task<PlayerComm?> RemoveMute(string sid, string adminSid); 

    public Task<PlayerComm?> RemoveGag(string sid, string adminSid); 
    
    #endregion

    #region Punishments with chat message

    public Task<bool> AddBan(string adminSid, PlayerBan banInfo); // With chat message
    public Task<bool> AddMute(string adminSid, PlayerComm muteInfo); // With chat message
    public Task<bool> AddGag(string adminSid, PlayerComm gagInfo); // With chat message
    public Task<PlayerBan?> UnBan(string sid, string adminSid); // With chat message
    public Task<PlayerComm?> UnMute(string sid, string adminSid); // With chat message
    public Task<PlayerComm?> UnGag(string sid, string adminSid); // With chat message
    
    #endregion
    

    #endregion

}
public class Admin
{
    
    public string Name { get; set; }
    public string SteamId { get; set; }
    public string Flags { get; set; }
    public int Immunity { get; set; }
    public int End { get; set; }
    public string GroupName { get; set; } = "";
    public int GroupId { get; set; }
    public string ServerId { get; set; }

    public  Admin(string name, string steamId, string flags, int immunity, int end, int groupId, string serverId) // For set Admin
    {
        Name = name;
        SteamId = steamId;
        Flags = flags;
        Immunity = immunity;
        End = end;
        GroupId = groupId;
        ServerId = serverId;
    }
}

public class Group
{
    public int Id { get; set; }
    public string Flags { get; set; }
    public string Name { get; set; }
    public int Immunity { get; set; }

    public Group(string name, string flags, int immunity, int id = 1)
    {
        Flags = flags;
        Name = name;
        Immunity = immunity;
        Id = id;
    }
}

public class PlayerInfo
{
    public string IpAddress;
    public string PlayerName;
    public SteamID SteamId;
    public CCSPlayerController? Controller;
    public PlayerInfo(string name, ulong sid, string ip)
    {
        PlayerName = name;
        SteamId = new SteamID(sid);
        IpAddress = ip;
    }
}

public class PlayerBan
{
    public int? Id { get; set; }
    public string Name { get; set; }
    public string Sid { get; set; }
    public string Ip { get; set; }
    public string AdminSid { get; set; }
    public string AdminName { get; set; }
    public int Created { get; set; }
    public int Time { get; set; }
    public int End { get; set; }
    public string Reason { get; set; }
    public int BanType { get; set; }
    public int Unbanned { get; set; }
    public string? UnbannedBy { get; set; }
    public string ServerId { get; set; }

    public PlayerBan(
        string name, string sid, string ip, 
        string adminSid, string adminName, int created, int time, int end, 
        string reason, string serverId, int banType = 0, int unbanned = 0, string? unbannedBy = null, int? id = null)
    {
        Name = name;
        Sid = sid;
        Ip = ip;
        AdminSid = adminSid;
        AdminName = adminName;
        Created = created;
        Time = time;
        End = end;
        Reason = reason;
        ServerId = serverId;
        BanType = banType;
        Unbanned = unbanned;
        UnbannedBy = unbannedBy;
        Id = id;
    }
}

public class PlayerComm
{
    public int? Id { get; set; }
    public string Name { get; set; }
    public string Sid { get; set; }
    public string AdminSid { get; set; }
    public string AdminName { get; set; }
    public int Created { get; set; }
    public int Time { get; set; }
    public int End { get; set; }
    public string Reason { get; set; }
    public int Unbanned { get; set; }
    public string? UnbannedBy { get; set; }
    public string ServerId { get; set; }

    public PlayerComm(
        string name, string sid, string adminSid, string adminName,
        int created, int time, int end, 
        string reason, string serverId, int unbanned = 0, string? unbannedBy = null, int? id = null)
    {
        Name = name;
        Sid = sid;
        AdminSid = adminSid;
        AdminName = adminName;
        Created = created;
        Time = time;
        End = end;
        Reason = reason;
        ServerId = serverId;
        Unbanned = unbanned;
        UnbannedBy = unbannedBy;
        Id = id;
    }
}


public class AdminMenuOption
{
    public Action<CCSPlayerController, Admin?, IMenu> OnSelect;
    public string FlagsAccess;
    public string FlagsDefault;
    public string Title;
    /// <summary>
    /// Влияет на то где будет расположенно меню
    /// Несколько базовых параметров:
    ///  - Main
    ///  - ManagePlayers
    ///  - ManageBlocks
    ///  - ManageServer
    ///  - Others
    /// </summary>
    public string OptionLocation;
    public AdminMenuOption(string title, string flagsAccess, string flagsDefault, string optionLocation, Action<CCSPlayerController, Admin?, IMenu> onSelect)
    {
        FlagsAccess = flagsAccess;
        FlagsDefault = flagsDefault;
        OptionLocation = optionLocation;
        Title = title;
        OnSelect = onSelect;
    }
}

public interface IBaseMenu
{
    public event Action<CCSPlayerController, Admin?, IMenu>? OnOpen;
    public void Open(CCSPlayerController caller, string title, string? menuTag, IMenu? backMenu = null); 
    public void Open(CCSPlayerController caller, string title, IMenu? backMenu = null);
}

public class Reason
{
    public string Title { get; set; }
    /// <summary>
    /// >= 0 - instantly ban
    /// == null - Select time
    /// == -1 - Own reason and select time
    /// </summary>
    public int? Time { get; set; }
    
    public Reason(string title, int? time)
    {
        Title = title;
        Time = time;
    }
}

public class Map
{
    public string Title { get; set; }
    public string Id { get; set; }
    public bool Workshop { get; set; }
    
    public Map(string title, string id, bool workshop)
    {
        Title = title;
        Id = id;
        Workshop = workshop;
    }
}

public static class PlayerExtensions
{
    public static PlayerInfo GetInfo(this CCSPlayerController controller)
    {
        return new PlayerInfo(
            controller.PlayerName,
            controller.AuthorizedSteamID!.SteamId64,
            controller.IpAddress!
        );
    }

    public static string GetSteamId(this CCSPlayerController? controller)
    {
        return controller == null ? "CONSOLE" : controller.AuthorizedSteamID!.SteamId64.ToString();
    }
    public static string GetName(this CCSPlayerController? controller)
    {
        return controller == null ? "CONSOLE" : controller.PlayerName;
    }
    public static string GetIp(this CCSPlayerController? controller)
    {
        return controller == null ? "0.0.0.0" : controller.IpAddress!.Split(":")[0];
    }
    public static void Kick(this CCSPlayerController? controller, NetworkDisconnectionReason reason = NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED)   
    {
        if (controller == null) return;
        controller.Disconnect(reason);
    }
    
    public static int GetTime(int time) 
    {
        return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
    
}

public interface IPluginCfg
{
    public string ServerId { get; set; }
    public string Host { get; set; } 
    public string Database { get; set; } 
    public string Password { get; set; }
    public string Port { get; set; }
    public string MenuType { get; set; }
    public int NotAuthorizedKickTime { get; set; }
    
    public bool BanOnAllServers { get; set; }
    public bool UpdateNames { get; set; }
    public bool HasAccessIfImmunityIsEqual { get; set; }  // Give access to command above the target if immunity == caller
    public Dictionary<string, string> Flags { get; set; }
    public List<string> BlockMassTargets { get; set; }
    
    public string[] AllServersBanReasons { get; set; }
    public List<Reason> BanReasons { get; set; }
    public List<Reason> GagReasons { get; set; } 
    public List<Reason> MuteReasons { get; set; }
    public List<string> KickReasons { get; set; } 
    public Dictionary<string, int> Times { get; set; } 
    public Dictionary<string, List<string>> ConvertedFlags { get; set; } 
    public List<Map> Maps { get; set; } 
}

public class CommandConstructor
{
    /// <summary>
    /// Examples:
    /// "css_admin_add <identity:offline> <>"
    /// </summary>
    public string ConstructorString; 
    public string Description;
    public Action<CCSPlayerController, List<Object>> OnExecute;
    public CommandConstructor(string constructorString, string description, Action<CCSPlayerController, Object[]>onExecute)
    {
        ConstructorString = constructorString;
        Description = description;
    }
    
}