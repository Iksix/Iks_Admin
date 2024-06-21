using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using IksAdminApi;

namespace IksAdmin;

public class PluginConfig : BasePluginConfig, IPluginCfg
{
    public string ServerId { get; set; } = "1";
    public string Host { get; set; } = "host";
    public string Database { get; set; } = "Database";
    public string User { get; set; } = "User";
    public string Password { get; set; } = "Password";
    public string Port { get; set; } = "3306";
    public string MenuType { get; set; } = "nickfox"; // chat, html, nickfox
    public int NotAuthorizedKickTime { get; set; } = 15; // If 0 then off
    public bool UpdateNames { get; set; } = false; 

    public bool BanOnAllServers { get; set; } = true;

    public bool HasAccessIfImmunityIsEqual { get; set; } = false; // Give access to command above the target if immunity == caller
    public Dictionary<string, string> Flags { get; set; } = new Dictionary<string, string>()
    {
        {"adminManage", "z"}
    };

    public List<string> BlockMassTargets { get; set; } = new()
    {
        "ban", "mute", "gag" 
    };

    public string[] AllServersBanReasons { get; set; } = new[] { "Cheats", "Pidoras" };
    
    public List<Reason> BanReasons { get; set; } = new()
    {
        new Reason("Cheats", 0),
        new Reason("AHk", 1440),
        new Reason("Other", null),
        new Reason("Own reason", -1)
    };
    
    public List<Reason> GagReasons { get; set; } = new()
    {
        new Reason("Flood", 0),
        new Reason("Ad", 1440),
        new Reason("Other", null),
        new Reason("Own reason", -1)
    };
    public List<Reason> MuteReasons { get; set; } = new()
    {
        new Reason("VoiceMod", 30),
        new Reason("Music", 60),
        new Reason("Other", null),
        new Reason("Own reason", -1)
    };
    public List<string> KickReasons { get; set; } = new()
    {
        "Afk",
        "$Own reason" // There own reason if starts with "$"
    };
    
    public Dictionary<string, int> Times { get; set; } = new Dictionary<string, int>()
    {
        {"60 minutes", 60},
        {"30 minutes", 30},
        {"15 minutes", 15},
        {"5 minutes", 5},
        {"Infinity", 0},
        {"Own time", -1}
    };
    
    public Dictionary<string, List<string>> ConvertedFlags { get; set; } = new()
    {
        {"z", new() {"@css/root", "@css/Owner"}},
        {"b", new() {"@css/ban"}},
        {"6", new() {"@css/someTag"}}
    };
    
    public List<Map> Maps { get; set; } = new()
    {
        new Map("Mirage", "de_mirage", false),
        new Map("Awp Lego", "3146105097", true)
    };
}
