using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IksAdminApi;


public class GagsConfig : PluginCFG<GagsConfig>, IPluginCFG
{
    public static GagsConfig Config = new GagsConfig();
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
        {60, "1 мин"},
        {3600, "1 час"},
        {3600 * 24, "1 день"},
        {3600 * 24 * 7, "1 неделя"},
        {3600 * 24 * 30, "1 месяц"},        
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
        Config = ReadOrCreate<GagsConfig>(AdminUtils.CoreInstance.ModuleDirectory + "/configs/gags.json", Config);
        AdminUtils.LogDebug("Gags config loaded ✔");
        AdminUtils.LogDebug("Reasons count " + Config.Reasons.Count);
    }
}