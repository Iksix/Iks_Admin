namespace Iks_Admin;

public class BannedPlayer
{
    public string Name = "";
    public string Sid = "";
    public string Ip = "";
    public string BanReason = "";
    public int BanCreated = 0;
    public int BanTime = 0;
    public int BanTimeEnd = 0;
    public string AdminSid = "";
    public bool Unbanned = false;
    public string UnbannedBy = "";

    public BannedPlayer(string name, string sid, string ip, string banReason, int banCreated, int banTime, int banTimeEnd, string adminSid, int unbanned, string unbannedBy)
    {
        Name = name;
        Sid = sid;
        Ip = ip;
        BanReason = banReason;
        BanCreated = banCreated;
        BanTime = banTime;
        BanTimeEnd = banTimeEnd;
        AdminSid = adminSid;
        Unbanned = unbanned == 1;
        UnbannedBy = unbannedBy;
    }
}