using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Iks_Admin;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("HaveIksChatColors")] public bool HaveIksChatColors { get; set; } = true;
    [JsonPropertyName("Host")] public string Host { get; set; } = "localhost";
    [JsonPropertyName("Port")] public int Port { get; set; } = 3306;
    [JsonPropertyName("Name")] public string Name { get; set; } = "dbname";
    [JsonPropertyName("Login")] public string Login { get; set; } = "dblogin";
    [JsonPropertyName("Password")] public string Password { get; set; } = "dbpassword";
    
    [JsonPropertyName("KickReasons")] public string[] KickReasons { get; set; } = new [] {"Афк", "Мешает играть", "Игнор админа"};
    [JsonPropertyName("BanReasons")] public string[] BanReasons { get; set; } = new [] {"Читы", "Оск. родных", "Отказ от проверки"};
    [JsonPropertyName("MuteReason")] public string[] MuteReason { get; set; } = new [] {"Саундпад", "Воисмод", "Спам"};
    [JsonPropertyName("GagReason")] public string[] GagReason { get; set; } = new [] {"Реклама", "Оскорбления", "Спам"};
    
    [JsonPropertyName("Times")] public int[] Times { get; set; } = new [] {120, 60, 30, 15, 0};

    
}