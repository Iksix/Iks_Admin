using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using CounterStrikeSharp.API;

namespace XUtils;

public static class ConfigUtils
{
    /// <summary>
    /// Creates or Read JsonConfig
    /// </summary>
    /// <param name="config">Base config instance</param>
    /// <param name="directory">Folder name in configs/plugins/{directory}</param>
    /// <param name="fileName">Config file name: {fileName}.json</param>
    /// <param name="plugin">Plugin instance</param>
    public static T CreateOrRead<T>(T config, string directory, string fileName)
    {
        var directoryPath = $"{Server.GameDirectory}/addons/counterstrikesharp/configs/plugins/{directory}";
        var filePath = $"{directoryPath}/{fileName}.json";
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Console.WriteLine($"[XUtils] Creating directory: {directoryPath}");
        }
        
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(config, options: new JsonSerializerOptions() { WriteIndented = true, AllowTrailingCommas = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.Cyrillic), ReadCommentHandling = JsonCommentHandling.Skip}));
            Console.WriteLine($"[XUtils] Config was created: {filePath}");
            return config;
        }
        
        using var streamReader = new StreamReader(filePath);
        
        var json = streamReader.ReadToEnd();
        
        config = JsonSerializer.Deserialize<T>(json, options: new JsonSerializerOptions() { WriteIndented = true, AllowTrailingCommas = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.Cyrillic), ReadCommentHandling = JsonCommentHandling.Skip})!;
        
        Console.WriteLine($"[XUtils] Config was read: {filePath}");
        return config;
    }
}