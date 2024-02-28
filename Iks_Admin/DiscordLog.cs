using Discord.Webhook;

namespace Iks_Admin;

public class DiscordLog
{
    public PluginConfig Config;

    public DiscordLog(PluginConfig config)
    {
        Config = config;
    }

    public async Task sendPunMessage(string message, string name, string sid, string ip, string adminName, string reason, int time, bool offline)
    {
        if (!Config.LogToDiscord)
        {
            return;
        }
        string status = offline ? Config.LogToVkMessages["OfflineOption"] : Config.LogToVkMessages["OnlineOption"];
        message = message
                .Replace("{name}", name)
                .Replace("{admin}", adminName)
                .Replace("{reason}", reason)
                .Replace("{sid}", sid)
                .Replace("{ip}", ip)
                .Replace("{duration}", time.ToString())
                .Replace("{server_id}", Config.ServerId)
                .Replace("{profile}", $"https://steamcommunity.com/profiles/{sid}")
                .Replace("{status}", status)
                .Replace("{server}", Config.ServerName);

        await sendMessage(message, new DColor(255, 0, 0));

    }

    public async Task sendUnPunMessage(string message, string name, string sid, string adminName, string ip, bool offline)
    {
        if (!Config.LogToDiscord)
        {
            return;
        }
        string status = offline ? Config.LogToVkMessages["OfflineOption"] : Config.LogToVkMessages["OnlineOption"];

        message = message.Replace("{admin}", adminName)
                .Replace("{name}", name)
                .Replace("{sid}", sid)
                .Replace("{server_id}", Config.ServerId)
                .Replace("{profile}", $"https://steamcommunity.com/profiles/{sid}")
                .Replace("{server}", Config.ServerName)
                .Replace("{status}", status)
                .Replace("{ip}", ip);
        
        await sendMessage(message, new DColor(0, 255, 0));

    }


    public async Task sendMessage(string message, DColor color)
    {
        if (!Config.LogToDiscord)
        {
            return;
        }
        var webhookObject = new WebhookObject();
        webhookObject.AddEmbed(builder =>
        {
            builder.WithTitle(Config.ServerName)
                .WithColor(color)
                .WithDescription(message);
        });
        await new Webhook(Config.WebHookUrl).SendAsync(webhookObject);
    }
}