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
    
    public async Task BanPlayer(string name, string sid, string adminsid, int time, string reason)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_bans (`name`, `sid`, `adminsid`, `created`, `time`, `end`, `reason`) VALUES ('{name}', '{sid}', '{adminsid}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}', '{time*60}', '{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + time*60}', '{reason}')";
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

    public bool IsPlayerBanned(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_bans WHERE sid='{sid}' AND end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                var comm = new MySqlCommand(sql, connection);
                if (comm.ExecuteScalar() == null)
                {
                    return false;
                }
                Console.WriteLine("Player is banned");
                return true;
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
                string sql = $"SELECT * FROM iks_mutes WHERE sid='{sid}' AND end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
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
    public bool IsPlayerGagged(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_gags WHERE sid='{sid}' AND end>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
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
}