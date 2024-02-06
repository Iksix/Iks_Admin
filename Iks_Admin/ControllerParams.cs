using CounterStrikeSharp.API.Modules.Entities;

namespace Iks_Admin;

public class ControllerParams
{
    public string PlayerName;
    public string SteamID;
    public string IpAddress;


    public ControllerParams(string name, string sid, string ip)
    {
        PlayerName = name;
        SteamID = sid.ToString();
        IpAddress = ip;
    }
}