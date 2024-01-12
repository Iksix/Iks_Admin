namespace Iks_Admin;

public class DisconnectedPlayer
{
    public string Name;
    public string Sid;
    public string Ip;
    public long Date;


    public DisconnectedPlayer(string name, string sid, string? ip)
    {
        Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Name = name;
        Sid = sid;
        Ip = "Undefined";
        if (ip != null)
        {
            Ip = ip;
        }
    }
}