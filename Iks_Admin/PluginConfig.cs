using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Iks_Admin;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("Host")] public string Host { get; set; } = "localhost";
    [JsonPropertyName("Port")] public int Port { get; set; } = 3306;
    [JsonPropertyName("Name")] public string Name { get; set; } = "dbname";
    [JsonPropertyName("Login")] public string Login { get; set; } = "dblogin";
    [JsonPropertyName("Password")] public string Password { get; set; } = "dbpassword";
}