
namespace Iks_Admin;

public class Admin
{
    public string Name;
    public string SteamId;
    public string Flags;
    public int Immunity;
    public int End;
    public string GroupName;
    public int? GroupId;
    public string ServerId;

    public Admin(string name, string steamId, string flags, int immunity, int end, string groupName, int? groupId, string serverId) // For set Admin
    {
        Name = name;
        SteamId = steamId;
        Flags = flags;
        Immunity = immunity;
        End = end;
        GroupName = groupName;
        GroupId = groupId;
        ServerId = serverId;
    }
    
}