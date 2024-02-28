using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Iks_Admin;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("HaveIksChatColors")] public bool HaveIksChatColors { get; set; } = true;
    [JsonPropertyName("ServerId")] public string ServerId { get; set; } = "A"; // Просто указать букву СИНТАКСИС ВАЖЕН
    [JsonPropertyName("BanOnAllServers")] public bool BanOnAllServers { get; set; } = false; // Если false то бан будет только на том серевере где server_id как в бане
    [JsonPropertyName("ServerName")] public string ServerName { get; set; } = "Test Server"; // Название сервера
    [JsonPropertyName("Host")] public string Host { get; set; } = "localhost";
    [JsonPropertyName("Port")] public int Port { get; set; } = 3306;
    [JsonPropertyName("Name")] public string Name { get; set; } = "dbname";
    [JsonPropertyName("Login")] public string Login { get; set; } = "dblogin";
    [JsonPropertyName("Password")] public string Password { get; set; } = "dbpassword";

    [JsonPropertyName("KickReasons")] public string[] KickReasons { get; set; } = new[] { "Reason1", "Reason2", "Reason3" };
    [JsonPropertyName("BanReasons")] public string[] BanReasons { get; set; } = new[] { "Reason1", "Reason2", "Reason3" };
    [JsonPropertyName("MuteReason")] public string[] MuteReason { get; set; } = new[] { "Reason1", "Reason2", "Reason3" };
    [JsonPropertyName("GagReason")] public string[] GagReason { get; set; } = new[] { "Reason2", "Reason2", "Reason3" };
    [JsonPropertyName("SilenceReason")] public string[] SilenceReason { get; set; } = new[] { "Reason2", "Reason2", "Reason3" }; // [1.1.6]
    [JsonPropertyName("Maps")] public string[] Maps { get; set; } = new[] { "de_mirage", "de_dust2", "de_inferno" }; // [1.1.6]

    // [JsonPropertyName("Times")] public int[] Times { get; set; } = new[] { 120, 60, 30, 15, 0 };
    [JsonPropertyName("Times")] public Dictionary<string, int> Times { get; set; } = new() { // [1.1.6]
        {"1 year", 525600},
        {"1 month", 43200},
        {"1 week", 10080},
        {"1 day", 1440},
        {"1 hour", 60},
        {"30 minutes", 30},
    };
    
    // [1.1.6] Кастомные пункты меню, используются по принципу команды от имени админа/сервера
    // В будущем планируется глобальная переработка, ну пока так
    // {uid}, {steamid}, {name}
    [JsonPropertyName("CustomMenu")] public bool CustomMenu { get; set; } = true;
    [JsonPropertyName("Custom")] public List<CustomItem> Custom { get; set; } = new() { 
        {new CustomItem(
            "Открыть меню варнов",
            "b",
            "css_warn",
            false
        )}
    };


    [JsonPropertyName("ConvertedFlags")]
    public Dictionary<string, List<string>> ConvertedFlags { get; set; } = new Dictionary<string, List<string>>()
    {
        {"z", new List<string>(){
            "@css/root"
        }},
        {"b", new List<string>(){
            "@css/ban"
        }},
        {"m", new List<string>(){
            "@css/mute"
        }},
        {"l", new List<string>(){
            "@css/custom",
            "@css/custom2",
            "@css/custom3"
        }},
    };

    [JsonPropertyName("LogToVk")] public bool LogToVk { get; set; } = false;
    [JsonPropertyName("Token")] public string Token { get; set; } = "ваш токен";
    [JsonPropertyName("ChatId")] public string ChatId { get; set; } = "ваш чат айди";

    [JsonPropertyName("LogToVkMessages")]
    public Dictionary<string, string> LogToVkMessages { get; set; } = new Dictionary<string, string>()
    {
        {"SilenceMessage", "Админ {admin} заглушил игрока {name} на {duration}! \n Причина: {reason} \n {status}"}, // [1.1.6]
        {"ChangeMapMessage", "Админ {admin} сменил карту на {map}"}, // [1.1.6]

        {"BanMessage" , "Админ {admin} забанил игрока {name} на {duration}! \n Причина: {reason} \n {status}"},
        {"UnBanMessage", "Админ {admin} разбанил игрока {name}!"},
        {"MuteMessage", "Админ {admin} замутил игрока {name} на {duration}! \n Причина: {reason} \n {status}"},
        {"UnMuteMessage", "Админ {admin} размутил игрока {name}!"},
        {"GagMessage", "Админ {admin} гагнул игрока {name} на {duration}! \n Причина: {reason} \n {status}"},
        {"UnGagMessage", "Админ {admin} снял гаг игрока {name}!"},
        {"KickMessage", "Админ {admin} кикнул игрока {name}! \n Причина: {reason}"},
        {"SlayMessage", "Админ {admin} убил игрок {name}!"},
        {"HsayMessage", "Админ {admin} написал сообщение в худ! \n Текст: {text}"},
        {"SwitchTeamMessage", "Админ: {admin} \n Перемещение игрока {name} \n Прошлая команда: {oldteam} \n Новая команда: {newteam}!"},
        {"ChangeTeamMessage", "Админ: {admin} \n Смена команды игрока: {name} \n Прошлая команда: {oldteam} \n Новая команда: {newteam}"},

        {"OfflineOption", "Offline"},
        {"OnlineOption", "Online"}
    };

    [JsonPropertyName("LogToDiscord")] public bool LogToDiscord { get; set; } = false;
    [JsonPropertyName("WebHookUrl")] public string WebHookUrl { get; set; } = "";

    [JsonPropertyName("LogToDiscordMessages")]
    public Dictionary<string, string> LogToDiscordMessages { get; set; } = new Dictionary<string, string>()
    {
        {"SilenceMessage", "Админ {admin} заглушил игрока {name} на {duration}! \n Причина: {reason} \n {status}"}, // [1.1.6]
        {"ChangeMapMessage", "Админ {admin} сменил карту на {map}"}, // [1.1.6]

        {"BanMessage" , "Админ {admin} забанил игрока {name} на {duration}! \n Причина: {reason} \n {status}"},
        {"UnBanMessage", "Админ {admin} разбанил игрока {name}!"},
        {"MuteMessage", "Админ {admin} замутил игрока {name} на {duration}! \n Причина: {reason} \n {status}"},
        {"UnMuteMessage", "Админ {admin} размутил игрока {name}!"},
        {"GagMessage", "Админ {admin} гагнул игрока {name} на {duration}! \n Причина: {reason} \n {status}"},
        {"UnGagMessage", "Админ {admin} снял гаг игрока {name}!"},
        {"KickMessage", "Админ {admin} кикнул игрока {name}! \n Причина: {reason}"},
        {"SlayMessage", "Админ {admin} убил игрок {name}!"},
        {"HsayMessage", "Админ {admin} написал сообщение в худ! \n Текст: {text}"},
        {"SwitchTeamMessage", "Админ: {admin} \n Перемещение игрока {name} \n Прошлая команда: {oldteam} \n Новая команда: {newteam}!"},
        {"ChangeTeamMessage", "Админ: {admin} \n Смена команды игрока: {name} \n Прошлая команда: {oldteam} \n Новая команда: {newteam}"},

        {"OfflineOption", "Offline"},
        {"OnlineOption", "Online"}
    };


}