using MySqlConnector;

namespace Iks_Admin;

public class Database
{
    private string _dbConnectionString; 
    public Database(string connString)
    {
        _dbConnectionString = connString;
    }
    
    public async Task BanPlayer(string playerSid, string playerName, int Time, string reason)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO as_bans (`admin_steamid`, `steamid`, `name`, `admin_name`, `created`, `duration`, `end`, `reason`) VALUES ('[VoteBKM]', '{playerSid}', '{playerName}', '[VoteBKM]', {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, {Time*60}, {DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Time*60}, '{reason}')";
                var comm = new MySqlCommand(sql, connection);
                comm.ExecuteNonQuery();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [VoteBKM] Db error: {ex}");
        }
    }
}