using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class DBServers
{
    public static async Task Add(ServerModel server)
    {
        try
        {
            AdminUtils.LogDebug("Add server to base...");
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var existingServer = await Get(server.Id);
            if (existingServer != null)
            {
                AdminUtils.LogDebug("Server exists with id " + existingServer.Id);
                server.Id = existingServer.Id;
                await Update(server);
                return;
            }
            await conn.QueryAsync(@"
                insert into iks_servers(id, ip, name, rcon, created_at, updated_at)
                values(@serverId, @ip, @name, @rcon, unix_timestamp(), unix_timestamp())
            ", new {
                serverId = server.Id,
                ip = server.Ip,
                name = server.Name,
                rcon = server.Rcon
            });
            AdminUtils.LogDebug("Server added to db ✔");
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    private static async Task Update(ServerModel server)
    {
        try
        {
            AdminUtils.LogDebug("Server update...");
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_servers set
                ip = @ip,
                name = @name,
                rcon = @rcon,
                updated_at = unix_timestamp(),
                deleted_at = null
                where id = @id
            ", 
            new {
                ip = server.Ip,
                name = server.Name,
                rcon = server.Rcon,
                id = server.Id
            }
            );
            AdminUtils.LogDebug($"Server updated ✔");
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<ServerModel?> Get(int id)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();

            var server = await conn.QueryFirstOrDefaultAsync<ServerModel>(@"
                select
                id as id,
                ip as ip,
                name as name,
                created_at as createdAt,
                updated_at as updatedAt,
                deleted_at as deletedAt,
                rcon as rcon
                from iks_servers
                where id = @id
                and deleted_at is null
            ", new {
                id
            });
            return server;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<ServerModel>> GetAll()
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();

            var servers = (await conn.QueryAsync<ServerModel>(@"
                select
                id as id,
                ip as ip,
                name as name,
                created_at as createdAt,
                updated_at as updatedAt,
                deleted_at as deletedAt,
                rcon as rcon
                from iks_servers
                where deleted_at is null
            ")).ToList();

            return servers;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
}