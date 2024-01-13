using System.Data;
using MySqlConnector;

namespace Iks_Admin;

public class AdminManager
{
    private string _dbConnectionString;

    public AdminManager(string dbConnStr)
    {
        _dbConnectionString = dbConnStr;
    } 

    public async Task<List<Admin>> GetAllAdmins(string server_id)
    {
        List<Admin> admins = new List<Admin>();
        
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM iks_admins";
                var comm = new MySqlCommand(sql, connection);
                
                var res = await comm.ExecuteReaderAsync();
                string name = "";
                string sid = "";
                string flags = "";
                int immunity = -1;
                int end = 0;
                int? group_id = null;
                string group_name = "";
                string server ="A";

                while (await res.ReadAsync())
                {
                    name = res.GetString("name");
                    server = res.GetString("server_id");
                    sid = res.GetString("sid");
                    flags = res.GetString("flags");
                    immunity = res.GetInt32("immunity");
                    end = res.GetInt32("end");
                    group_id = res.GetInt32("group_id");
                    if (group_id != -1)
                    {
                        try
                        {
                            using (var connection2 = new MySqlConnection(_dbConnectionString))
                            {
                                connection2.Open();
                                string sql2 = $"SELECT * FROM iks_groups WHERE id={group_id}";
                                var comm2 = new MySqlCommand(sql2, connection2);

                                var res2 = await comm2.ExecuteReaderAsync();

                                while (await res2.ReadAsync())
                                {
                                    group_name = res2.GetString("name");
                                    if (flags.Trim() == "")
                                    {
                                        flags = res2.GetString("flags");
                                    }
                                    if (immunity == -1)
                                    {
                                        immunity = res2.GetInt32("immunity");
                                    }
                                }

                                
                            }
                        }
                        catch (MySqlException ex)
                        {
                            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
                        }
                    }
                    Admin admin = new Admin(name, sid, flags, immunity, end, group_name, group_id, server);
                    if (admin.ServerId.Contains(server_id) || admin.ServerId.Trim() == "")
                    {
                        admins.Add(admin);
                    }
                }
            }
            
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return admins;
    }

    public async Task AddAdmin(string sid, string name, string flags, int immunity, int group_id, long end, string server_id)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"INSERT INTO iks_admins (`sid`, `name`, `flags`, `immunity`, `group_id`, `end`, `server_id`) VALUES ('{sid}', '{name}', '{flags}', '{immunity}', '{group_id}', '{end}', '{server_id}')";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
    }

    public async Task<bool> DeleteAdminIfEnd(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"DELETE FROM iks_admins WHERE sid='{sid}' AND end<{ DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        return true;
    } 
    public async Task<bool> DeleteAdmin(string sid)
    {
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = $"DELETE FROM iks_admins WHERE sid='{sid}'";
                var comm = new MySqlCommand(sql, connection);
                
                await comm.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }
        return true;
    } 

}