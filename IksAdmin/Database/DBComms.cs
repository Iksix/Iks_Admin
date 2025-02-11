using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class DBComms
{
    private static readonly string SelectComm = @"
    select
    id as id,
    steam_id as steamId,
    ip as ip,
    name as name,
    mute_type as muteType,
    duration as duration,
    reason as reason,
    server_id as serverId,
    admin_id as adminId,
    unbanned_by as unbannedBy,
    unban_reason as unbanReason,
    created_at as createdAt,
    end_at as endAt,
    updated_at as updatedAt,
    deleted_at as deletedAt
    from iks_comms
    ";
    public static async Task<List<PlayerComm>> GetActiveComms(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var comms = await conn.QueryAsync<PlayerComm>(SelectComm + @"
                where deleted_at is null
                and steam_id = @steamId
                and unbanned_by is null
                and (end_at > unix_timestamp() or end_at = 0)
                and (server_id is null or server_id = @serverId)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id, timestamp = AdminUtils.CurrentTimestamp()});
            return comms.ToList();
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<List<PlayerComm>> GetLastAdminComms(Admin admin, int time)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var comms = (await conn.QueryAsync<PlayerComm>($@"
                {SelectComm}
                where deleted_at is null
                and admin_id = @admin_id
                and (server_id is null or server_id = @serverId)
                and created_at > unix_timestamp() - @time
            ", new {time, admin_id = admin.Id, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return comms;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerComm>> GetLastComms(int time)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var comms = (await conn.QueryAsync<PlayerComm>($@"
                {SelectComm}
                where deleted_at is null
                and (server_id is null or server_id = @serverId)
                and created_at > unix_timestamp() - @time
            ", new {time, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return comms;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerComm>> GetAllComms(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var comms = (await conn.QueryAsync<PlayerComm>($@"
                {SelectComm}
                where deleted_at is null
                and steam_id = @steamId
                and (server_id is null or server_id = @serverId)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return comms;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerComm>> GetAllActiveComms(int timeOffset = 86400)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var comms = (await conn.QueryAsync<PlayerComm>($@"
                {SelectComm}
                where deleted_at is null
                and (server_id is null or server_id = @serverId)
                and created_at => @time
            ", new {serverId = Main.AdminApi.ThisServer.Id, time = AdminUtils.CurrentTimestamp() - timeOffset})).ToList();
            return comms;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    /// <summary>
    /// return statuses: 0 - banned, 1 - already banned, -1 - other
    /// </summary>
    public static async Task<DBResult> Add(PlayerComm punishment)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            punishment.SetEndAt();
            var id = await conn.QuerySingleAsync<int>(@"
                insert into iks_comms
                (steam_id, ip, name, mute_type, duration, reason, server_id, admin_id, unbanned_by, unban_reason, created_at, end_at, updated_at, deleted_at)
                values
                (@steamId, @ip, @name, @muteType, @duration, @reason, @serverId, @adminId, @unbannedBy, @unbanReason, @createdAt, @endAt, @updatedAt, @deletedAt);
                select last_insert_id();
            ", new {
                steamId = punishment.SteamId,
                ip = punishment.Ip,
                name = punishment.Name,
                muteType = punishment.MuteType,
                duration = punishment.Duration,
                reason = punishment.Reason,
                serverId = punishment.ServerId,
                adminId = punishment.AdminId,
                unbannedBy = punishment.UnbannedBy,
                unbanReason = punishment.UnbanReason,
                createdAt = punishment.CreatedAt,
                endAt = punishment.EndAt,
                updatedAt = punishment.UpdatedAt,
                deletedAt = punishment.DeletedAt
            });
            return new DBResult(id, 0);
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            return new DBResult(null, -1, e.ToString());
        }
    }
    /// <summary>
    /// return statuses: 0 - unbanned, 1 - ban not finded, 2 - admin can't do this, -1 - other
    /// </summary>
    public static async Task<DBResult> UnComm(Admin admin, PlayerComm comm, string? reason)
    {
        try
        {
            if (!CanUnComm(admin, comm)) return new DBResult(comm.Id, 2, "admin can't do this");
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_comms set 
                unbanned_by = @adminId, 
                unban_reason = @reason,
                updated_at = @timestamp,
                where id = @id
            ", new {
                timestamp = AdminUtils.CurrentTimestamp(),
                adminId = admin.Id,
                id = comm.Id,
                reason
            });
            return new DBResult(comm.Id, 0);
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            return new DBResult(comm.Id, -1, e.ToString());
        }
    }

    private static bool CanUnComm(Admin admin, PlayerComm comm)
    {
        var bannedBy = comm.Admin;
        if (bannedBy == null) return true;
        if (bannedBy.SteamId == admin.SteamId) return true;
        if (bannedBy.SteamId != "CONSOLE")
        {
            if (admin.HasPermissions("blocks_manage.remove_all")) return true;
        } else {
            if (admin.HasPermissions("blocks_manage.remove_console")) return true;
            return false;
        }
        if (admin.HasPermissions("blocks_manage.remove_immunity") && bannedBy.CurrentImmunity < admin.CurrentImmunity) return true;
        if (admin.HasPermissions("other.equals_immunity_action") && admin.HasPermissions("blocks_manage.remove_immunity") && bannedBy.CurrentImmunity <= admin.CurrentImmunity) return true;
        return false;
    }
    
}