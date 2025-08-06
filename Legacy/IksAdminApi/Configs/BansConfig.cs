using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IksAdminApi;


public class BansConfig : PluginCFG<BansConfig>, IPluginCFG
{
    public static BansConfig Config = new BansConfig();
    public bool TitleToTextInReasons {get; set;} = true;
    public string[] BlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public List<BanReason> Reasons { get; set; } = new ()
    {
        new BanReason("Example reason title 1", "Another text for reason", 0, 30, null, false),
        new BanReason("Example reason title 2", banOnAllServers: true, duration: 0),
    };
    public bool BanOnAllServers {get; set;} = false;
    public bool AlwaysBanForIpAndSteamId {get; set;} = false; // Банить ли всегда по айпи и стим айди одновременно

    public Dictionary<int, string> Times {get; set;} = new()
    {
        {1, "1 мин"},
        {60, "1 час"},
        {60 * 24, "1 день"},
        {60 * 24 * 7, "1 неделя"},
        {60 * 24 * 30, "1 месяц"},        
        {0, "Навсегда"}        
    };
    
    public static bool HasReason(string reason)
    {
        return Config.Reasons.Any(x => x.Title.ToLower() == reason.ToLower() || x.Text.ToLower() == reason.ToLower());
    }
    public static bool HasTime(int time)
    {
        return Config.Times.Any(x => x.Key == time);
    }

    public void Set()
    {
        Config = ReadOrCreate<BansConfig>(AdminUtils.CoreInstance.ModuleDirectory + "/../../configs/plugins/IksAdmin/bans.json", Config);
        AdminUtils.LogDebug("Bans config loaded ✔");
        AdminUtils.LogDebug("Reasons count " + Config.Reasons.Count);
    }
}