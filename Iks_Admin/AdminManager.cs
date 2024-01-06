using MySqlConnector;

namespace Iks_Admin;

public class AdminManager
{
    private string _dbConnectionString;

    public AdminManager(string dbConnStr)
    {
        _dbConnectionString = dbConnStr;
    }

    public List<Admin> ReloadAdmins()
    {
        List<Admin> admins = new List<Admin>();
        
        try
        {
            using (var connection = new MySqlConnection(_dbConnectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM iks_admins";
                var comm = new MySqlCommand(sql, connection);
                
                var res = comm.ExecuteReader();

                while (res.Read())
                {
                    Admin newAdmin = new Admin(
                        res.GetString("name"),
                        res.GetString("sid"),
                        res.GetString("flags"),
                    res.GetInt32("immunity"),
                        res.GetInt32("end"));
                    admins.Add(newAdmin);
                }
                
            }
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($" [Iks_Admins] Db error: {ex}");
        }

        return admins;
    }
}