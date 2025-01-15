namespace IksAdminApi;
public class CoreConfig : PluginCFG<CoreConfig>, IPluginCFG
{
    public static CoreConfig Config = new CoreConfig();
    public int ServerId { get; set; } = 1;
    public string ServerIp {get; set;} = "0.0.0.0:27015"; // Указываете IP сервера
    public string ServerName {get; set;} = "Server name"; // Название сервера, если пусто то АВТО
    public string RconPassword {get; set;} = "12345";
    // DATABASE ===
    public string Host { get; set; } = "host";
    public string Database { get; set; } = "Database";
    public string User { get; set; } = "User";
    public string Password { get; set; } = "Password";
    public string Port { get; set; } = "3306";
    // ===
    public Dictionary<string, string[]> BlockedIdentifiers {get; set;} = new () { // Блок идентификаторов для команд с поддержкой @
        {"css_slay", [""]},
        {"css_kick", ["@all", "@players", "@bots", "@ct", "@t"]},
    };
    public Dictionary<string, string> CommandReplacement {get; set;} = new () { // Изменяет команды
        //{"css_respawn", "css_arespawn"} // -> Меняет css_respawn на css_arespawn к примеру
    };
    public int MaxWarns { get; set; } = 3; // Максимальное кол-во варнов для блокировки админки у игрока
    public string WebApiKey {get; set;} = ""; // Указываете API ключ для получения имени в оффлайн бане
    public bool AdvancedKick {get; set;} = true;
    public int AdvancedKickTime {get; set;} = 5;
    public bool DebugMode { get; set; } = true;
    public int MenuType { get; set; } = 2; // -1 = Default(Player select) [MM] | 0 = ChatMenu | 1 = ConsoleMenu | 2 = HtmlMenu | 3 = ButtonMenu [MM]
    public Dictionary<string, string> PermissionReplacement { get; set; } = new Dictionary<string, string>()
    {
        {"admins_manage_add", "z"} // Пример замены права управления админами на флаг z (Ну он и так z по дефолту, ну так что бы знали)
    };
    public string[] IgnoreCommandsRegistering {get; set;} = ["example_command"]; // Эти команды не будут инициализированны при добавлении через метод админки (пишем без префикса css_)
    public string[] MirrorsIp {get; set;} = ["0.0.0.0"]; // Эти айпи не возможно будет добавить в наказания (будет null) (рассчитано что тут будут айпи зеркал)
    public int LastPunishmentTime {get; set;} = 24*60*60*2; // Последние наказания за это время буду отображаться в снятии бана и оффлайн наказаний в ообщем (в секундах)
    public bool UseOnlineAdminsName {get; set;} = false; // Использовать ли текущий ник админа если он онлайн (Для Announce)
    public bool AutoUpdateDatabaseNames {get; set;} = false; // Обновлять ли ники админов в базе данных на текущие в стиме при подключении
    public void Set()
    {
        Config = ReadOrCreate("configs/core", Config);
        AdminUtils.LogDebug("Core config loaded ✔");
    }
}