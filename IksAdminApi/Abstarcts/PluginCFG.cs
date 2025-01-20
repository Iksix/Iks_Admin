using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace IksAdminApi;

public abstract class PluginCFG<IPluginCFG>
{
    public IPluginCFG ReadOrCreate<IPluginCFG>(string path, IPluginCFG defaultConfig)
    {
        var filePath = path;
        if (!File.Exists(filePath))
        {
            AdminUtils.LogDebug("Creating config file for " + filePath);
            File.WriteAllText(filePath, JsonSerializer.Serialize(defaultConfig, options: new JsonSerializerOptions() { WriteIndented = true, AllowTrailingCommas = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.Cyrillic), ReadCommentHandling = JsonCommentHandling.Skip}));
        }
        using var streamReader = new StreamReader(filePath);
        var json = streamReader.ReadToEnd();
        AdminUtils.LogDebug("Deserialize config file for " + filePath);
        var config = JsonSerializer.Deserialize<IPluginCFG>(json, options: new JsonSerializerOptions() { WriteIndented = true, AllowTrailingCommas = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.Cyrillic), ReadCommentHandling = JsonCommentHandling.Skip});
        AdminUtils.LogDebug("Deserialized ✔");
        return config!;
    }
}