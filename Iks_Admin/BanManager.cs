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

    

    
    public async Task BanPlayer(string name, string sid, string? ip, string adminsid, int time, string reason)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_bans (`name`, `sid`, `ip`, `adminsid`, `created`, `time`, `end`, `reason`) VALUES ('{name}', '{sid}', '{ip}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time*60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time*60}', '{reason}')";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }
    
    public async Task rBanPlayer(string name, string sid, string ip, string adminsid, int time, string reason)
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
                string sql = $"INSERT INTO iks_bans (`name`, `sid`, `ip`, `adminsid`, `created`, `time`, `end`, `reason`) VALUES ('{name}', '{sid}', 'Undefined[RconBan]', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time*60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time*60}', '{reason}')";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }
    public async Task UnBanPlayer(string sid, string adminsid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"UPDATE iks_bans SET `Unbanned` = 1, `UnbannedBy` = '{adminsid}' WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }
    public async Task MutePlayer(string name, string sid, string adminsid, int time, string reason)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_mutes (`name`, `sid`, `adminsid`, `created`, `time`, `end`, `reason`) VALUES ('{name}', '{sid}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time*60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time*60}', '{reason}')";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }
    public async Task GagPlayer(string name, string sid, string adminsid, int time, string reason)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_gags (`name`, `sid`, `adminsid`, `created`, `time`, `end`, `reason`) VALUES ('{name}', '{sid}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time*60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time*60}', '{reason}')";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }

    public async Task UnMutePlayer(string sid, string adminsid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"UPDATE iks_mutes SET `Unbanned` = 1, `UnbannedBy` = '{adminsid}' WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }
    
    public async Task UnGagPlayer(string sid, string adminsid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"UPDATE iks_gags SET `Unbanned` = 1, `UnbannedBy` = '{adminsid}' WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND `Unbanned` = 0";
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
    
    public bool IsPlayerBanned(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_bans WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";
                var comm = new MySqlCommand(sql, connection);
                if (comm.ExecuteScalar() != null)
                {
                    Console.WriteLine("Player is banned");
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
    
    public bool IsPlayerBannedIp(string ip)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_bans WHERE ip='{ip}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0 AND BanType=1";
                var comm = new MySqlCommand(sql, connection);
                if (comm.ExecuteScalar() != null)
                {
                    Console.WriteLine("Player is banned");
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
    public bool IsPlayerMuted(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_mutes WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";
                var comm = new MySqlCommand(sql, connection);
                if (comm.ExecuteScalar() == null)
                {
                    return false;
                }
                Console.WriteLine("Player is muted");
                return true;
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        
        return false;
    }
    public async Task<bool> IsPlayerMutedAsync(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_mutes WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";
                var comm = new MySqlCommand(sql, connection);
                if (await comm.ExecuteScalarAsync() == null)
                {
                    return false;
                }
                Console.WriteLine("Player is muted");
                return true;
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        
        return false;
    }
    public bool IsPlayerGagged(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_gags WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";
                var comm = new MySqlCommand(sql, connection);
                if (comm.ExecuteScalar() == null)
                {
                    return false;
                }
                Console.WriteLine("Player is gagged");
                return true;
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        
        return false;
    }
    public async Task<bool> IsPlayerGaggedAsync(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_gags WHERE sid='{sid}' AND (end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()} OR time=0) AND Unbanned=0";
                var comm = new MySqlCommand(sql, connection);
                if (await comm.ExecuteScalarAsync() == null)
                {
                    return false;
                }
                Console.WriteLine("Player is gagged");
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

}