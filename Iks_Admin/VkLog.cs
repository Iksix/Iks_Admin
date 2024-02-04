using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Config;
using Microsoft.VisualBasic;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace Iks_Admin;

public class VkLog
{

    private string Token = "";
    private long ChatId;
    private PluginConfig Config;
    public VkLog(string token, string chatId, PluginConfig config)
    {
        Token = token;
        ChatId = Int64.Parse(chatId);
        Config = config;
    }
    public async Task sendPunMessage(string message, string name, string sid, string ip, string adminName, string reason, int time, bool offline)
    {
        var apiAuthParams = new ApiAuthParams
        {
            AccessToken = Token,
            Settings = Settings.Messages
        };
        sid = sid.Replace("#", "");
        var api = new VkApi();
        await api.AuthorizeAsync(apiAuthParams);
        string status = offline ? Config.LogToVkMessages["OfflineOption"] : Config.LogToVkMessages["OnlineOption"];
        string ed_message = message
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


        try
        {
            await api.Messages.SendAsync(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                PeerId = ChatId,
                Message = ed_message
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Iks_Admin] Vk Message error: {ex.Message}");
        }

    }
    public async Task sendUnPunMessage(string message, string name, string sid, string adminName, string ip, bool offline)
    {
        var apiAuthParams = new ApiAuthParams
        {
            AccessToken = Token,
            Settings = Settings.Messages
        };
        var api = new VkApi();
        await api.AuthorizeAsync(apiAuthParams);
        string status = offline ? Config.LogToVkMessages["OfflineOption"] : Config.LogToVkMessages["OnlineOption"];

        string ed_message = message.Replace("{admin}", adminName)
                .Replace("{name}", name)
                .Replace("{sid}", sid)
                .Replace("{server_id}", Config.ServerId)
                .Replace("{profile}", $"https://steamcommunity.com/profiles/{sid}")
                .Replace("{server}", Config.ServerName)
                .Replace("{status}", status)
                .Replace("{ip}", ip);




        try
        {
            await api.Messages.SendAsync(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                PeerId = ChatId,
                Message = ed_message
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Iks_Admin] Vk Message error: {ex.Message}");
        }

    }
}