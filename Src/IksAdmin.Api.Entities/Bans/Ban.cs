namespace IksAdmin.Api.Entities.Bans;

public class Ban
{
    public int Id { get; set; }
    
    public ulong? SteamId { get; set; }
    
    public string? Ip { get; set; }
    
    public string? PlayerName { get; set; }
    
    public int Duration { get; set; }
    
    public required string Reason { get; set; }
    
    public BanType BanType { get; set; }
    
    public int? ServerId { get; set; }
    
    public int AdminId { get; set; }
    
    public int? UnbannedBy { get; set; }
    
    public string? UnbanReason { get; set; }
    
    public long EndAt { get; set; }
    
    public long CreatedAt { get; set; }
    
    public long UpdatedAt { get; set; }
    
    public long? DeletedAt { get; set; }
}