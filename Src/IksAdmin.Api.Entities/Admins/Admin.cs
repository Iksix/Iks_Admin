namespace IksAdmin.Api.Entities.Admins;

public class Admin
{
    public int Id { get; set; }
    
    /// <summary>
    /// Admin name in DataBase
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Admin SteamId64 <br/> If <c>null</c> then admin is <c>CONSOLE</c>
    /// </summary>
    public ulong? SteamId { get; set; }
    
    /// <summary>
    /// Admin Flags
    /// </summary>
    public required string Flags { get; set; }
    
    /// <summary>
    /// Admin Immunity
    /// </summary>
    public int Immunity { get; set; }
    
    /// <summary>
    /// Admin group id
    /// </summary>
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    
    /// <summary>
    /// Admin discord
    /// </summary>
    public string? Discord { get; set; }
    
    /// <summary>
    /// Admin VK
    /// </summary>
    public string? Vk { get; set; }
    
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Id of the servers that the admin has access to <br/>
    /// If Contains(null) => Admin has access to all servers
    /// </summary>
    public List<int?> ServerIds { get; set; } = [];
    
    /// <summary>
    /// The time until which the privilege admin will work
    /// </summary>
    public long? EndAt { get; set; }
    
    public long CreatedAt { get; set; }

    public long UpdatedAt { get; set; }
    
    /// <summary>
    /// Delete admin time in Unix format <br/>
    /// If null => Admin not deleted
    /// </summary>
    public long? DeletedAt { get; set; }
}