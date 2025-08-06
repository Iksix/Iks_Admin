namespace IksAdmin.Api.Entities.Admins;

public class Group
{
    public int Id { get; set; }
    
    /// <summary>
    /// Group name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Group flags
    /// </summary>
    public required string Flags { get; set; }
    
    /// <summary>
    /// Group Immunity
    /// </summary>
    public int Immunity { get; set; }
    
    /// <summary>
    /// Group comment
    /// </summary>
    public string? Comment { get; set; }
}