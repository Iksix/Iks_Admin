using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace IksAdminApi;

public interface IIksAdminApi
{
    public enum UsedMenuType
    {
        Chat,
        Html
    }
    // Fields
    public List<AdminMenuOption> ModulesOptions { get; set; }
    public List<PlayerComm> OnlineMutedPlayers { get; set; }
    public List<PlayerComm> OnlineGaggedPlayers { get; set; }
    public List<PlayerInfo> DisconnectedPlayers { get; set; }
    public Dictionary<CCSPlayerController, Action<string>> NextCommandAction { get; set; }
    public UsedMenuType MenuType { get; set; }
    public string DbConnectionString { get; set; }
    public List<Admin> AllAdmins { get; set; }
    public List<Admin> ThisServerAdmins { get; set; }
    public IStringLocalizer Localizer { get; set; }
    public BasePlugin Plugin { get; }
    
    // Functions
    public IBaseMenu CreateMenu(CCSPlayerController caller, Action<CCSPlayerController, Admin?, IMenu> onOpen);
    public void SendMessageToPlayer(CCSPlayerController? controller, string message);
    public void SendMessageToAll(string message);
    public Task ReloadInfractions(string sid, bool checkBan = true);
    public Task<bool> AddBan(string adminSid, PlayerBan banInfo);
    public Task<bool> AddMute(string adminSid, PlayerComm muteInfo);
    public Task<bool> AddGag(string adminSid, PlayerComm gagInfo);

    public Task<PlayerBan?> UnBan(string sid, string adminSid);
    public Task<PlayerComm?> UnMute(string sid, string adminSid);
    public Task<PlayerComm?> UnGag(string sid, string adminSid);
    
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
    
    public bool HasPermisions(string adminSid, string flagsAccess, string flagsDefault);
    public bool HasMoreImmunity(string adminSid, string targetSid);
    
    // Events
    event Action<Admin> OnAddAdmin;
    event Action<Admin> OnDelAdmin;
    event Action<PlayerBan> OnAddBan;
    event Action<PlayerComm> OnAddMute;
    event Action<PlayerComm> OnAddGag;
    event Action<PlayerBan, string> OnUnBan;
    event Action<PlayerComm, string> OnUnMute;
    event Action<PlayerComm, string> OnUnGag;

    event Action<string, PlayerInfo, string> OnKick; // AdminSid -> Target info
    event Action<string, PlayerInfo, string, string> OnRename; // AdminSid -> Target info -> OldName -> newName
    event Action<string, PlayerInfo> OnSlay; // AdminSid -> Target info
    event Action<string, PlayerInfo, CsTeam, CsTeam> OnSwitchTeam; // AdminSid -> Target Info -> Old Team -> New Team 
    event Action<string, PlayerInfo, CsTeam, CsTeam> OnChangeTeam; // AdminSid -> Target Info -> Old Team -> New Team 
    event Action<string, string> OnChangeMap;
    event Action<List<Admin>> OnReloadAdmins;
    event Action<CCSPlayerController?, CommandInfo> OnCommandUsed;
    
    // events callers
    public void EKick(string adminSid, PlayerInfo target, string reason);
    public void ERename(string adminSid, PlayerInfo target, string oldName, string newName);
    public void ESlay(string adminSid, PlayerInfo target);
    public void ESwitchTeam(string adminSid, PlayerInfo target, CsTeam oldTeam, CsTeam newTeam);
    public void EChangeTeam(string adminSid, PlayerInfo target, CsTeam oldTeam, CsTeam newTeam);
    public void EChangeMap(string adminSid, string newMap);

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
        string adminSid, int created, int time, int end, 
        string reason, string serverId, int banType = 0, int unbanned = 0, string? unbannedBy = null, int? id = null)
    {
        Name = name;
        Sid = sid;
        Ip = ip;
        AdminSid = adminSid;
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
    public int Created { get; set; }
    public int Time { get; set; }
    public int End { get; set; }
    public string Reason { get; set; }
    public int Unbanned { get; set; }
    public string? UnbannedBy { get; set; }
    public string ServerId { get; set; }

    public PlayerComm(
        string name, string sid, string adminSid,
        int created, int time, int end, 
        string reason, string serverId, int unbanned = 0, string? unbannedBy = null, int? id = null)
    {
        Name = name;
        Sid = sid;
        AdminSid = adminSid;
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
    public Action<CCSPlayerController, Admin?, IMenu>? OnSelect;
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
    public AdminMenuOption(string title, string flagsAccess, string flagsDefault, string optionLocation)
    {
        FlagsAccess = flagsAccess;
        FlagsDefault = flagsDefault;
        OptionLocation = optionLocation;
        Title = title;
    }
}

public interface IBaseMenu
{
    public event Action<CCSPlayerController, Admin?, IMenu>? OnOpen;
    public void Open(CCSPlayerController caller, string title, IMenu? backMenu = null);
}