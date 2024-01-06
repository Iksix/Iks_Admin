
namespace Iks_Admin;

public class Admin
{
    public string Name;
    public string SteamId;
    public string Flags;
    public int Immunity;
    public int End;

    public Admin(string name, string steamId, string flags, int immunity, int end) // For set Admin
    {
        Name = name;
        SteamId = steamId;
        Flags = flags;
        Immunity = immunity;
        End = end;
    }
    
}