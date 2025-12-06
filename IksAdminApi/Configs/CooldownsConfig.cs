namespace IksAdminApi;

public class CooldownForRound
{
    public int UsesForRound { get; set; }
    public string[] ImmunityFlags { get; set; }

    public CooldownForRound(int usesForRound, string[] immunityFlags)
    {
        UsesForRound = usesForRound;
        ImmunityFlags = immunityFlags;
    }
}

public class CooldownForTime
{
    public int Time { get; set; }
    public string[] ImmunityFlags { get; set; }

    public CooldownForTime(int time, string[] immunityFlags)
    {
        Time = time;
        ImmunityFlags = immunityFlags;
    }
}

public class CooldownsConfig : PluginCFG<CoreConfig>, IPluginCFG
{
    public Dictionary<string, CooldownForRound> ForRound { get; set; } = new ();
    public Dictionary<string, CooldownForTime> ForTime { get; set; } = new ();
}