namespace IksAdmin.Api.Entities.Servers;

public class Server
{
    public int Id { get; set; }
    
    public required string Ip { get; set; }
    
    public required string Name { get; set; }
    
    public string? Rcon { get; set; }
    
    public long CreatedAt { get; set; }

    public long UpdatedAt { get; set; }
    
    /// <summary>
    /// Delete server time in Unix format <br/>
    /// If null then Server is not deleted
    /// </summary>
    public long? DeletedAt { get; set; }
}