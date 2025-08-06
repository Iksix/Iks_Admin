using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IksAdminApi;

public class PlayerBan
{
    public int Id {get; set;}
    public string? SteamId {get; set;}
    public string? Ip {get; set;}
    public string? Name {get; set;}
    public string Reason {get; set;}
    public int Duration {get; set;}
    public sbyte BanType {get; set;} = 0;
    public int? ServerId {get; set;} = null;
    public int AdminId {get; set;}
    public int EndAt {get; set;}
    public int? UnbannedBy {get; set;}
    public string? UnbanReason {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;

    public bool IsExpired => EndAt != 0 && EndAt < AdminUtils.CurrentTimestamp();
    public bool IsUnbanned => UnbannedBy != null;

    public Admin? Admin {get {
        return AdminUtils.FindAdminByIdMethod(AdminId);
    }}
    public Admin? UnbannedByAdmin {get {
        if (UnbannedBy == null) return null;
        return AdminUtils.FindAdminByIdMethod((int)UnbannedBy);
    }}
    public ServerModel? Server {get {
        if (ServerId == null) return null;
        return AdminUtils.CoreApi.AllServers.FirstOrDefault(x => x.Id == ServerId);
    }}
    public string NameString => Name ?? "[NOT SETTED]";
    public string IpString => Ip ?? "[NOT SETTED]";
    // used for getting from db
    public PlayerBan(int id, long? steamId, string? ip, string? name, 
    int duration, string reason, sbyte banType, int? serverId, int adminId, 
    int? unbannedBy, string? unbanReason, int createdAt, int endAt, int 
    updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId?.ToString();
        Ip = ip;
        Name = name;
        Duration = duration;
        BanType = banType;
        Reason = reason; 
        ServerId = serverId;
        AdminId = adminId;
        UnbannedBy = unbannedBy;
        CreatedAt = createdAt;
        EndAt = endAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
        UnbanReason = unbanReason;
    }
    // creating ===
    public PlayerBan(string? steamId, string? ip, string? name, string reason, int duration, int? serverId = null, sbyte banType = 0)
    {
        SteamId = steamId;
        Ip = ip;
        Name = name;
        Duration = duration*60;
        SetEndAt();
        Reason = reason; 
        ServerId = serverId;
        BanType = banType;

        if (AdminUtils.Config().MirrorsIp.Contains(Ip)) Ip = null;
    }

    public PlayerBan(PlayerInfo player, string reason, int duration, int? serverId = null, sbyte banType = 0)
    {
        SteamId = player.SteamId;
        Ip = player.Ip;
        Name = player.PlayerName;
        Duration = duration*60;
        Reason = reason;
        SetEndAt();
        ServerId = serverId;
        BanType = banType;
        if (AdminUtils.Config().MirrorsIp.Contains(Ip)) Ip = null;
    }

    public void SetEndAt()
    {
        EndAt = Duration == 0 ? 0 : AdminUtils.CurrentTimestamp() + Duration;
    }
}