namespace IksAdminApi;

public class CommReason : Reason
{
    public CommReason(string title, string? text = null, int minTime = 0, int maxTime = 0, int? duration = null, bool banOnAllServers = false, bool hideFromMenu = false )
    {
        Title = title;
        if (text == null)
            Text = title;
        else Text = text;
        MinTime = minTime;
        MaxTime = maxTime;
        Duration = duration;
        BanOnAllServers = banOnAllServers;
        HideFromMenu = hideFromMenu;
    }
}