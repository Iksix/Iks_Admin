using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("DiscordWebHook")] public string DiscordWebHook { get; set; } = "";
    [JsonPropertyName("VkToken")] public string VkToken { get; set; } = "";
    [JsonPropertyName("VkChatID")] public long VkChatID { get; set; } = 20000001;
    [JsonPropertyName("LogToDiscord")] public bool LogToDiscord { get; set; } = false;
    [JsonPropertyName("LogToVk")] public bool LogToVk { get; set; } = false;
}