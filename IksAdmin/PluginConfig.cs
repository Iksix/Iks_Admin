using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using IksAdminApi;

namespace IksAdmin;

public class PluginConfig : BasePluginConfig, IPluginCfg
{
    [JsonPropertyName("ServerId")] public string ServerId { get; set; } = "A";
    [JsonPropertyName("Host")] public string Host { get; set; } = "host";
    [JsonPropertyName("Database")] public string Database { get; set; } = "Database";
    [JsonPropertyName("User")] public string User { get; set; } = "User";
    [JsonPropertyName("Password")] public string Password { get; set; } = "Password";
    [JsonPropertyName("Port")] public string Port { get; set; } = "3306";
    [JsonPropertyName("UseHtmlMenu")] public bool UseHtmlMenu { get; set; } = false;

    [JsonPropertyName("HasAccessIfImmunityIsEqual")]
    public bool HasAccessIfImmunityIsEqual { get; set; } = false; // Give access to command above the target if immunity == caller
    [JsonPropertyName("Flags")] public Dictionary<string, string> Flags { get; set; } = new Dictionary<string, string>()
    {
        {"adminManage", "z"}
    };


    [JsonPropertyName("BanReasons")]
    public List<Reason> BanReasons { get; set; } = new()
    {
        new Reason("Cheats", 0),
        new Reason("AHk", 1440),
        new Reason("Other", null),
        new Reason("Own reason", -1)
    };
    [JsonPropertyName("GagReasons")]
    public List<Reason> GagReasons { get; set; } = new()
    {
        new Reason("Flood", 0),
        new Reason("Ad", 1440),
        new Reason("Other", null),
        new Reason("Own reason", -1)
    };
    [JsonPropertyName("MuteReasons")]
    public List<Reason> MuteReasons { get; set; } = new()
    {
        new Reason("VoiceMod", 30),
        new Reason("Music", 60),
        new Reason("Other", null),
        new Reason("Own reason", -1)
    };
    [JsonPropertyName("KickReasons")]
    public List<string> KickReasons { get; set; } = new()
    {
        "Afk",
        "$Own reason" // There own reason if starts with "$"
    };
    
    [JsonPropertyName("Times")] public Dictionary<string, int> Times { get; set; } = new Dictionary<string, int>()
    {
        {"60 minutes", 60},
        {"30 minutes", 30},
        {"15 minutes", 15},
        {"5 minutes", 5},
        {"Infinity", 0},
        {"Own time", -1}
    };

    [JsonPropertyName("ConvertedFlags")]
    public Dictionary<string, List<string>> ConvertedFlags { get; set; } = new()
    {
        {"z", new() {"@css/root", "@css/Owner"}},
        {"b", new() {"@css/ban"}},
        {"6", new() {"@css/someTag"}}
    };

    [JsonPropertyName("Maps")]
    public List<Map> Maps { get; set; } = new()
    {
        new Map("Mirage", "de_mirage", false),
        new Map("Awp Lego", "3146105097", true)
    };
}
