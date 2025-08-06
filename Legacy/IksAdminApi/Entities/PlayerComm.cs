using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IksAdminApi;

public class PlayerComm
{
    public enum MuteTypes
    {
        Mute = 0,
        Gag = 1,
        Silence = 2
    }
    public int Id {get; set;}
    public string SteamId {get; set;}
    public string? Ip {get; set;}
    public string? Name {get; set;}
    public int MuteType {get; set;}
    public string Reason {get; set;}
    public int Duration {get; set;}
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
    // used for getting from db
    public PlayerComm(int id, long steamId, string? ip, string? name, int muteType, int duration, string reason, int? serverId, int adminId, int? unbannedBy, string? unbanReason, int createdAt, int endAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId.ToString();
        Ip = ip;
        Name = name;
        MuteType = muteType;
        Duration = duration;
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
    public PlayerComm(string steamId, string? ip, string? name, MuteTypes type, string reason, int duration, int? serverId = null)
    {
        SteamId = steamId;
        Ip = ip;
        Name = name;
        Duration = duration * 60;
        MuteType = (int)type;
        SetEndAt();
        Reason = reason; 
        ServerId = serverId;
        if (AdminUtils.Config().MirrorsIp.Contains(Ip)) Ip = null;
    }

    public PlayerComm(PlayerInfo player, MuteTypes type, string reason, int duration, int? serverId = null)
    {
        SteamId = player.SteamId!;
        Ip = player.Ip;
        Name = player.PlayerName;
        MuteType = (int)type; 
        Duration = duration * 60;
        SetEndAt();
        Reason = reason; 
        ServerId = serverId;
        if (AdminUtils.Config().MirrorsIp.Contains(Ip)) Ip = null;
    }

    public void SetEndAt()
    {
        EndAt = Duration == 0 ? 0 : AdminUtils.CurrentTimestamp() + Duration;
    }
}