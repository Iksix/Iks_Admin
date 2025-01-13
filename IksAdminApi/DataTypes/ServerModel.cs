using CounterStrikeSharp.API.Core;

namespace IksAdminApi;

public class ServerModel
{
    public int Id { get; set; }
    public string Ip { get; set; }
    public string Name { get; set; }
    public string? Rcon { get; set; }
    public int CreatedAt { get; set; } = AdminUtils.CurrentTimestamp();
    public int UpdatedAt { get; set; } = AdminUtils.CurrentTimestamp();
    public int? DeletedAt { get; set; }

    public ServerModel(int id, string ip, string name, int createdAt, int updatedAt, int? deletedAt, string? rcon = null)
    {
        Id = id;
        Ip = ip;
        Name = name;
        Rcon = rcon;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
    }
    public ServerModel(int id, string ip, string name, string? rcon = null)
    {
        Id = id;
        Ip = ip;
        Name = name;
        Rcon = rcon;
    }
}