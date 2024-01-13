using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Iks_Admin;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("HaveIksChatColors")] public bool HaveIksChatColors { get; set; } = true;
    [JsonPropertyName("ServerId")] public string ServerId { get; set; } = "A"; // Просто указать букву СИНТАКСИС ВАЖЕН
    [JsonPropertyName("ServerName")] public string ServerName { get; set; } = "Test Server"; // Название сервера
    [JsonPropertyName("Host")] public string Host { get; set; } = "localhost";
    [JsonPropertyName("Port")] public int Port { get; set; } = 3306;
    [JsonPropertyName("Name")] public string Name { get; set; } = "dbname";
    [JsonPropertyName("Login")] public string Login { get; set; } = "dblogin";
    [JsonPropertyName("Password")] public string Password { get; set; } = "dbpassword";
    
    [JsonPropertyName("KickReasons")] public string[] KickReasons { get; set; } = new [] {"Reason1", "Reason2", "Reason3"};
    [JsonPropertyName("BanReasons")] public string[] BanReasons { get; set; } = new [] {"Reason1", "Reason2", "Reason3"};
    [JsonPropertyName("MuteReason")] public string[] MuteReason { get; set; } = new [] {"Reason1", "Reason2", "Reason3"};
    [JsonPropertyName("GagReason")] public string[] GagReason { get; set; } = new [] {"Reason2", "Reason2", "Reason3"};
    
    [JsonPropertyName("Times")] public int[] Times { get; set; } = new [] {120, 60, 30, 15, 0};


    [JsonPropertyName("LogToVk")] public bool LogToVk { get; set; } = false;
    [JsonPropertyName("Token")] public string Token { get; set; } = "ваш токен";
    [JsonPropertyName("ChatId")] public string ChatId { get; set; } = "ваш чат айди";
    
    [JsonPropertyName("LogToVkMessages")] public Dictionary<string, string> LogToVkMessages { get; set; } = new Dictionary<string, string>()
    {
        {"BanMessage" , "Админ {admin} забанил игрока {name} на {duration}! \n Причина: {reason}"},
        {"UnBanMessage", "Админ {admin} разбанил игрока {name}!"},
        {"MuteMessage", "Админ {admin} замутил игрока {name} на {duration}! \n Причина: {reason}"},
        {"UnMuteMessage", "Админ {admin} размутил игрока {name}!"},
        {"GagMessage", "Админ {admin} гагнул игрока {name} на {duration}! \n Причина: {reason}"},
        {"UnGagMessage", "Админ {admin} снял гаг игрока {name}!"},
        {"KickMessage", "Админ {admin} кикнул игрока {name}! \n Причина: {reason}"}
    };

    [JsonPropertyName("LogToDiscord")] public bool LogToDiscord { get; set; } = false;
    [JsonPropertyName("WebHookUrl")] public string WebHookUrl { get; set; } = "ваш вебхук";
    
    [JsonPropertyName("LogToDiscordMessages")] public Dictionary<string, string> LogToDiscordMessages { get; set; } = new Dictionary<string, string>()
    {
        {"BanMessage" , "Админ {admin} забанил игрока {name} на {duration}! \n Причина: {reason}"},
        {"UnBanMessage", "Админ {admin} разбанил игрока {name}!"},
        {"MuteMessage", "Админ {admin} замутил игрока {name} на {duration}! \n Причина: {reason}"},
        {"UnMuteMessage", "Админ {admin} размутил игрока {name}!"},
        {"GagMessage", "Админ {admin} гагнул игрока {name} на {duration}! \n Причина: {reason}"},
        {"UnGagMessage", "Админ {admin} снял гаг игрока {name}!"},
        {"KickMessage", "Админ {admin} кикнул игрока {name}! \n Причина: {reason}"}
    };

    
}