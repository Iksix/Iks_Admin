using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CounterStrikeSharp.API.Core;

namespace IksAdminApi;

[Table("iks_admins")]
public class Admin 
{
    public delegate string GetCurrentFlagsMethod(Admin admin);
    public static GetCurrentFlagsMethod GetCurrentFlagsFunc = null!;
    public delegate int GetCurrentImmunityMethod(Admin admin);
    public static GetCurrentImmunityMethod GetCurrentImmunityFunc = null!;
    
    public int Id {get; set;}
    
    public string SteamId {get; set;}
    
    public string Name {get; set;}
    
    public string? Flags {get; set;}
    
    public int? Immunity {get; set;}
    
    public int? GroupId {get; set;}
    
    public string? Discord {get; set;}
    
    public string? Vk {get; set;}
    
    public int Disabled {get; set;}
    
    public int CreatedAt {get; set;}
    
    public int UpdatedAt {get; set;}
    
    public int? DeletedAt {get; set;}
    
    public int? EndAt {get; set;}
    public bool Online => PlayersUtils.GetControllerBySteamId(SteamId) != null;

    public string CurrentName {get {
        if (!CoreConfig.Config.UseOnlineAdminsName)
            return Name;
        var controller = PlayersUtils.GetControllerBySteamId(SteamId);
        if (controller == null)
            return Name;
        return controller.PlayerName;
    }}
    public string CurrentFlags => GetCurrentFlagsFunc(this);
    public int CurrentImmunity => GetCurrentImmunityFunc(this);
    public Group? Group => AdminUtils.GetGroup(GroupId);

    public bool IsDisabled => Disabled == 1 || IsDisabledByWarns || IsDisabledByEnd;

    public bool IsDisabledByWarns => Warns.Count >= AdminUtils.CoreApi.Config.MaxWarns;

    public bool IsDisabledByEnd => EndAt != null && EndAt < AdminUtils.CurrentTimestamp();

    public int?[] Servers { get  {
        var a = AdminUtils.CoreApi.AdminsToServer.Where(x => x.AdminId == Id).ToArray();
        List<int?> serverIds = new();
        foreach (var b in a)
        {
            serverIds.Add(b.ServerId);
        }
        return serverIds.ToArray();
    } }
    
    public bool OnAllServers => Servers.Contains(null);
    public CCSPlayerController? Controller => PlayersUtils.GetControllerBySteamId(SteamId);
    public bool IsConsole => Id == 1;

    public List<Warn> Warns {get {
        return AdminUtils.CoreApi.Warns.Where(x => x.TargetId == Id).ToList();
    }}

    // Limitations ===
    public int MinBanTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_ban_time");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxBanTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_ban_time");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MinGagTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_gag_time");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxGagTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_gag_time");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MinMuteTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "min_mute_time");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxMuteTime {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_mute_time");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxBansInDay {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_bans_in_day");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxGagsInDay {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_gags_in_day");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxMutesInDay {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_mutes_in_day");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxBansInRound {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_bans_in_round");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxGagsInRound {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_gags_in_round");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    public int MaxMutesInRound {get {
        if (Group == null) return 0;
        var limit = Group.Limitations.FirstOrDefault(x => x.LimitationKey == "max_mutes_in_round");
        if (limit == null) return 0;
        return int.Parse(limit.LimitationValue);
    }}
    /// <summary>
    /// For getting from db
    /// </summary>
    public Admin(int id, string steamId, string name, string? flags, int? immunity, int? groupId, string? discord, string? vk, int isDisabled, int? endAt, int createdAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        GroupId = groupId;
        Disabled = isDisabled;
        Discord = discord;
        Vk = vk;
        EndAt = endAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
    }
    /// <summary>
    /// For creating new admin
    /// </summary>
    public Admin(string steamId, string name, string? flags = null, int? immunity = null, int? groupId = null, string? discord = null, string? vk = null, int? endAt = null)
    {
        SteamId = steamId;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        GroupId = groupId;
        Discord = discord;
        Vk = vk;
        EndAt = endAt;
        CreatedAt = AdminUtils.CurrentTimestamp();
        UpdatedAt = AdminUtils.CurrentTimestamp();
    }
}