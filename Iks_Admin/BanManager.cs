using System.Runtime.Serialization;
using MySqlConnector;

namespace Iks_Admin;

public class BanManager
{
    private string _dbConnectionString;

    public BanManager(string dbConnStr)
    {
        _dbConnectionString = dbConnStr;
    }


    #region Tasks




    public async Task BanPlayer(string name, string sid, string ip, string adminsid, int time, string reason, PluginConfig? Config)
    {
        sid = sid.Replace("#", "");
        if (ip.Split(":").Length > 0)
            ip = ip.Split(":")[0];
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_bans (`name`, `sid`, `ip`, `adminsid`, `created`, `time`, `end`, `reason`) VALUES ('{name}', '{sid}', '{ip}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}')";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"INSERT INTO iks_bans (`name`, `sid`, `ip`, `adminsid`, `created`, `time`, `end`, `reason`, `server_id`) VALUES ('{name}', '{sid}', '{ip}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}', '{Config.ServerId}')";
                    }
                }


                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }

    public async Task BanPlayerIp(string name, string sid, string ip, string adminsid, int time, string reason, PluginConfig? Config)
    {
        if (ip.Split(":").Length > 0)
        {
            ip = ip.Split(":")[0];
        }

        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_bans (`name`, `sid`, `ip`, `adminsid`, `created`, `time`, `end`, `reason`, `BanType`) VALUES ('{name}', '{sid}', '{ip}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}', 1)";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"INSERT INTO iks_bans (`name`, `sid`, `ip`, `adminsid`, `created`, `time`, `end`, `reason`, `BanType`, `server_id`) VALUES ('{name}', '{sid}', '{ip}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}', 1, '{Config.ServerId}')";
                    }
                }


                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }

    public async Task rBanPlayer(string name, string sid, string ip, string adminsid, int time, string reason, int BanType, PluginConfig? Config)
    {
        if (ip == "-")
        {
            ip = "UNDEFINED";
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_bans (`name`, `sid`, `ip`, `adminsid`, `created`, `time`, `end`, `reason`, `BanType`) VALUES ('{name}', '{sid}', '{ip}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}', '{BanType}')";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"INSERT INTO iks_bans (`name`, `sid`, `ip`, `adminsid`, `created`, `time`, `end`, `reason`, `BanType`, `server_id`) VALUES ('{name}', '{sid}', '{ip}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}', '{BanType}', '{Config.ServerId}')";
                    }
                }

                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }
    public async Task UnBanPlayer(string arg, string adminsid, PluginConfig? Config)
    {
        arg = arg.Replace("#", "");
        if (arg == null || arg.ToLower() == "undefined")
        {
            return;
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"UPDATE iks_bans SET `Unbanned` = 1, `UnbannedBy` = '{adminsid}' WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0";
                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"UPDATE iks_bans SET `Unbanned` = 1, `UnbannedBy` = '{adminsid}' WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0 AND `server_id`='{Config.ServerId}'";
                    }
                }
                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"UPDATE iks_bans SET `Unbanned` = 1, `UnbannedBy` = '{adminsid}' WHERE ip='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned = 0 AND BanType=1";
                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"UPDATE iks_bans SET `Unbanned` = 1, `UnbannedBy` = '{adminsid}' WHERE ip='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned = 0 AND BanType=1 AND `server_id`='{Config.ServerId}'";
                    }
                }
                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }

    public async Task MutePlayer(string name, string sid, string adminsid, int time, string reason, PluginConfig Config)
    {
        sid = sid.Replace("#", "");
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_mutes (`name`, `sid`, `adminsid`, `created`, `time`, `end`, `reason`) VALUES ('{name}', '{sid}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}')";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"INSERT INTO iks_mutes (`name`, `sid`, `adminsid`, `created`, `time`, `end`, `reason`, `server_id`) VALUES ('{name}', '{sid}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}', '{Config.ServerId}')";
                    }
                }

                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }
    public async Task GagPlayer(string name, string sid, string adminsid, int time, string reason, PluginConfig? Config)
    {
        sid = sid.Replace("#", "");
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_gags (`name`, `sid`, `adminsid`, `created`, `time`, `end`, `reason`) VALUES ('{name}', '{sid}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}')";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"INSERT INTO iks_gags (`name`, `sid`, `adminsid`, `created`, `time`, `end`, `reason`, `server_id`) VALUES ('{name}', '{sid}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time * 60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time * 60}', '{reason}', '{Config.ServerId}')";
                    }
                }

                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }

    public async Task UnMutePlayer(string sid, string adminsid, PluginConfig Config)
    {
        sid = sid.Replace("#", "");
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"UPDATE iks_mutes SET Unbanned=1, UnbannedBy='{adminsid}' WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"UPDATE iks_mutes SET Unbanned=1, UnbannedBy='{adminsid}' WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0 AND `server_id` = '{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }

    public async Task UnGagPlayer(string sid, string adminsid, PluginConfig Config)
    {
        sid = sid.Replace("#", "");
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"UPDATE iks_gags SET `Unbanned` = 1, `UnbannedBy` = '{adminsid}' WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"UPDATE iks_gags SET Unbanned=1, UnbannedBy='{adminsid}' WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0 AND `server_id` = '{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);

                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }

    #endregion

    #region Checks

    public async Task<bool> IsPlayerBannedAsync(string? arg, PluginConfig? Config)
    {
        if (arg == null || arg.ToLower() == "undefined")
        {
            return false;
        }
        if (arg.Split(":").Length > 0)
        {
            arg = arg.Split(":")[0];
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_bans WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_bans WHERE sid='{arg}' AND server_id='{Config.ServerId}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND server_id='{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);
                if (await comm.ExecuteScalarAsync() != null)
                {
                    return true;
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_bans WHERE ip='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND BanType=1";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_bans WHERE ip='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND BanType=1 AND server_id='{Config.ServerId}'";
                    }
                }
                var comm = new MySqlCommand(sql, connection);
                if (await comm.ExecuteScalarAsync() != null)
                {
                    return true;
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return false;
    }

    public async Task<bool> IsPlayerMutedAsync(string sid, PluginConfig Config)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_mutes WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_mutes WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND server_id='{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);
                if (await comm.ExecuteScalarAsync() == null)
                {
                    return false;
                }
                return true;
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return false;
    }
    public async Task<List<string>> GetMutedPlayers(List<string> sids, PluginConfig Config)
    {
        List<string> muted = new List<string>();
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string combinedString = "";
                combinedString = "'";

                combinedString += string.Join("','", sids);

                combinedString += "'";

                string sql = $"SELECT * FROM iks_mutes WHERE sid IN ({combinedString}) AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_mutes WHERE sid IN ({combinedString}) AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND server_id='{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);
                var reader = await comm.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    muted.Add(reader.GetString("sid"));
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return muted;
    }
    public async Task<List<string>> GetGaggedPlayers(List<string> sids, PluginConfig Config)
    {
        List<string> muted = new List<string>();
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string combinedString = "";
                combinedString = "'";

                combinedString += string.Join("','", sids);

                combinedString += "'";

                string sql = $"SELECT * FROM iks_gags WHERE sid IN ({combinedString}) AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_gags WHERE sid IN ({combinedString}) AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND server_id='{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);
                var reader = await comm.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    muted.Add(reader.GetString("sid"));
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return muted;
    }
    public async Task<bool> IsPlayerGaggedAsync(string sid, PluginConfig Config)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_gags WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_gags WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND server_id='{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);
                if (await comm.ExecuteScalarAsync() == null)
                {
                    return false;
                }
                return true;
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return false;
    }

    #endregion

    public async Task<List<BannedPlayer>> GetPlayerBansBySid(string sid)
    {
        List<BannedPlayer> playerBans = new List<BannedPlayer>();

        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_bans WHERE sid={sid} ORDER BY created DESC";
                var comm = new MySqlCommand(sql, connection);
                MySqlDataReader reader = await comm.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string Name = reader.GetString("name");
                    string Sid = reader.GetString("sid");
                    string Ip = reader.GetString("ip");
                    string BanReason = reader.GetString("reason");
                    int BanCreated = reader.GetInt32("created");
                    int BanTime = reader.GetInt32("time");
                    int BanTimeEnd = reader.GetInt32("end");
                    string AdminSid = reader.GetString("adminsid");
                    int BanType = reader.GetInt32("BanType");
                    int Unbanned = reader.GetInt32("Unbanned");
                    string UnbannedBy = "";
                    if (Unbanned == 1)
                    {
                        UnbannedBy = reader.GetString("UnbannedBy");
                    }
                    BannedPlayer player = new BannedPlayer(
                        Name,
                        Sid,
                        Ip,
                        BanReason,
                        BanCreated,
                        BanTime,
                        BanTimeEnd,
                        AdminSid,
                        Unbanned,
                        UnbannedBy,
                        BanType
                        );

                    playerBans.Add(player);
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return playerBans;
    }


    #region Gets

    public async Task<BannedPlayer?> GetPlayerBan(string? arg, PluginConfig? Config)
    {
        if (arg == null || arg.ToLower() == "undefined")
        {
            return null;
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_bans WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_bans WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND server_id='{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);
                MySqlDataReader reader = await comm.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string Name = reader.GetString("name");
                    string Sid = reader.GetString("sid");
                    string Ip = reader.GetString("ip");
                    string BanReason = reader.GetString("reason");
                    int BanCreated = reader.GetInt32("created");
                    int BanTime = reader.GetInt32("time");
                    int BanTimeEnd = reader.GetInt32("end");
                    string AdminSid = reader.GetString("adminsid");
                    int Unbanned = reader.GetInt32("Unbanned");
                    int BanType = reader.GetInt32("BanType");
                    string UnbannedBy = "";
                    if (Unbanned == 1)
                    {
                        UnbannedBy = reader.GetString("UnbannedBy");
                    }
                    BannedPlayer player = new BannedPlayer(
                        Name,
                        Sid,
                        Ip,
                        BanReason,
                        BanCreated,
                        BanTime,
                        BanTimeEnd,
                        AdminSid,
                        Unbanned,
                        UnbannedBy,
                        BanType
                        );


                    return player;
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_bans WHERE ip='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";
                var comm = new MySqlCommand(sql, connection);
                MySqlDataReader reader = await comm.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string Name = reader.GetString("name");
                    string Sid = reader.GetString("sid");
                    string Ip = reader.GetString("ip");
                    string BanReason = reader.GetString("reason");
                    int BanCreated = reader.GetInt32("created");
                    int BanTime = reader.GetInt32("time");
                    int BanTimeEnd = reader.GetInt32("end");
                    string AdminSid = reader.GetString("adminsid");
                    int Unbanned = reader.GetInt32("Unbanned");
                    int BanType = reader.GetInt32("BanType");
                    string UnbannedBy = "";
                    if (Unbanned == 1)
                    {
                        UnbannedBy = reader.GetString("UnbannedBy");
                    }
                    BannedPlayer player = new BannedPlayer(
                        Name,
                        Sid,
                        Ip,
                        BanReason,
                        BanCreated,
                        BanTime,
                        BanTimeEnd,
                        AdminSid,
                        Unbanned,
                        UnbannedBy,
                        BanType
                        );

                    return player;
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return null;
    }

    public async Task<BannedPlayer?> GetPlayerGag(string? arg, PluginConfig? Config)
    {
        if (arg == null || arg.ToLower() == "undefined")
        {
            return null;
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_gags WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_gags WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND server_id='{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);
                MySqlDataReader reader = await comm.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string Name = reader.GetString("name");
                    string Sid = reader.GetString("sid");
                    string Ip = "Undefined";
                    string BanReason = reader.GetString("reason");
                    int BanCreated = reader.GetInt32("created");
                    int BanTime = reader.GetInt32("time");
                    int BanTimeEnd = reader.GetInt32("end");
                    string AdminSid = reader.GetString("adminsid");
                    int Unbanned = reader.GetInt32("Unbanned");
                    int BanType = 0;
                    string UnbannedBy = "";
                    if (Unbanned == 1)
                    {
                        UnbannedBy = reader.GetString("UnbannedBy");
                    }

                    BannedPlayer player = new BannedPlayer(
                        Name,
                        Sid,
                        Ip,
                        BanReason,
                        BanCreated,
                        BanTime,
                        BanTimeEnd,
                        AdminSid,
                        Unbanned,
                        UnbannedBy,
                        BanType
                        );
                    return player;
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return null;
    }

    public async Task<BannedPlayer?> GetPlayerMute(string? arg, PluginConfig Config)
    {
        if (arg == null || arg.ToLower() == "undefined")
        {
            return null;
        }
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_mutes WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";

                if (Config != null)
                {
                    if (!Config.BanOnAllServers)
                    {
                        sql = $"SELECT * FROM iks_mutes WHERE sid='{arg}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND server_id='{Config.ServerId}'";
                    }
                }

                var comm = new MySqlCommand(sql, connection);
                MySqlDataReader reader = await comm.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string Name = reader.GetString("name");
                    string Sid = reader.GetString("sid");
                    string Ip = "Undefined";
                    string BanReason = reader.GetString("reason");
                    int BanCreated = reader.GetInt32("created");
                    int BanTime = reader.GetInt32("time");
                    int BanTimeEnd = reader.GetInt32("end");
                    string AdminSid = reader.GetString("adminsid");
                    int Unbanned = reader.GetInt32("Unbanned");
                    int BanType = 0;
                    string UnbannedBy = "";
                    if (Unbanned == 1)
                    {
                        UnbannedBy = reader.GetString("UnbannedBy");
                    }
                    BannedPlayer player = new BannedPlayer(
                        Name,
                        Sid,
                        Ip,
                        BanReason,
                        BanCreated,
                        BanTime,
                        BanTimeEnd,
                        AdminSid,
                        Unbanned,
                        UnbannedBy,
                        BanType
                        );

                    return player;
                }
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return null;
    }

    #endregion
}