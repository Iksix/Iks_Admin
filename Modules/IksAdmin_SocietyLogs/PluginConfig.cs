using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using Discord.Webhook;
using IksAdmin_SocietyLogs;

public class PluginConfig : BasePluginConfig
{
    public string DiscordWebHook { get; set; } = "";
    public string DiscordAuthorName { get; set; } = "Название вашего сервера";
    public string VkToken { get; set; } = "";
    public long VkChatID { get; set; } = 20000001;
    public bool LogToDiscord { get; set; } = false;
    public bool LogToVk { get; set; } = false;

    public Dictionary<string, EmbedModel> DiscordMessages { get; set; } = new()
    {
        {
            "ban",
            new EmbedModel(
                "ban", 
                "```Block in game```",
                new ColorModel(255, 255, 255),
                new []{ new FieldModel("**Block in game**", "```New Ban Added```", true)}
                )
        }
    };



}