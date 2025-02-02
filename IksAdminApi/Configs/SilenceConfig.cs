using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IksAdminApi;


public class SilenceConfig : PluginCFG<SilenceConfig>, IPluginCFG
{
    public static SilenceConfig Config = new SilenceConfig();
    public bool TitleToTextInReasons {get; set;} = true; // При прописывании команды например: 'css_gag iks 0 читы', конечная причина будет заменена на Text причины с соотвествующим Title
    public string[] BlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public string[] UnblockBlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public List<CommReason> Reasons { get; set; } = new ()
    {
        new CommReason("Example reason title 1", "Another text for reason", 0, 30, null, false),
        new CommReason("Example reason title 2", banOnAllServers: true, duration: 0),
    };

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
        Config = ReadOrCreate<SilenceConfig>(AdminUtils.CoreInstance.ModuleDirectory + "/../../configs/plugins/IksAdmin/silence.json", Config);
        AdminUtils.LogDebug("Silence config loaded ✔");
        AdminUtils.LogDebug("Reasons count " + Config.Reasons.Count);
    }
}