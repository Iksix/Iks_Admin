using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IksAdminApi;


public class KicksConfig : PluginCFG<KicksConfig>, IPluginCFG
{
    public static KicksConfig Config = new KicksConfig();
    public bool TitleToTextInReasons {get; set;} = true; // При прописывании команды например: 'css_gag iks 0 читы', конечная причина будет заменена на Text причины с соотвествующим Title
    public string[] BlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public List<KickReason> Reasons { get; set; } = new ()
    {
        new KickReason("AFK", "АФК")
    };

    public static bool HasReason(string reason)
    {
        return Config.Reasons.Any(x => x.Title.ToLower() == reason.ToLower() || x.Text.ToLower() == reason.ToLower());
    }
    public void Set()
    {
        Config = ReadOrCreate<KicksConfig>(AdminUtils.CoreInstance.ModuleDirectory + "/../../configs/plugins/IksAdmin/kicks.json", Config);
        AdminUtils.LogDebug("Kicks config loaded ✔");
        AdminUtils.LogDebug("Reasons count " + Config.Reasons.Count);
    }
}