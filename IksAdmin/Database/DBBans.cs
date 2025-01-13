using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class DBBans
{
    private static readonly string SelectBans = @"
    select
    id as id,
    steam_id as steamId,
    ip as ip,
    name as name,
    duration as duration,
    reason as reason,
    ban_type as banType,
    server_id as serverId,
    admin_id as adminId,
    unbanned_by as unbannedBy,
    unban_reason as unbanReason,
    created_at as createdAt,
    end_at as endAt,
    updated_at as updatedAt,
    deleted_at as deletedAt
    from iks_bans
    ";
    public static async Task<PlayerBan?> GetActiveBan(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var ban = await conn.QueryFirstOrDefaultAsync<PlayerBan>(SelectBans + @"
                where deleted_at is null
                and steam_id = @steamId
                and unbanned_by is null
                and (end_at > unix_timestamp() or end_at = 0)
                and (server_id is null or server_id = @serverId)
                and (ban_type=0 or ban_type=2)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id, timestamp = AdminUtils.CurrentTimestamp()});
            return ban;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<List<PlayerBan>> GetLastAdminBans(Admin admin, int time)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var bans = (await conn.QueryAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and admin_id = @admin_id
                and (server_id is null or server_id = @serverId)
                and created_at > unix_timestamp() - @time
            ", new {time, admin_id = admin.Id, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return bans;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerBan>> GetLastBans(int time)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var bans = (await conn.QueryAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and (server_id is null or server_id = @serverId)
                and created_at > unix_timestamp() - @time
            ", new {time, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return bans;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<PlayerBan?> GetActiveBanIp(string ip)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var ban = await conn.QueryFirstOrDefaultAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and ip = @ip
                and unbanned_by is null
                and end_at > unix_timestamp()
                and (server_id is null or server_id = @serverId)
                and (ban_type=1 or ban_type=2)
            ", new {ip, serverId = Main.AdminApi.ThisServer.Id});
            return ban;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerBan>> GetAllIpBans(string ip)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var bans = (await conn.QueryAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and ip = @ip and (ban_type = 1 or ban_type = 2)
                and (server_id is null or server_id = @serverId)
            ", new {ip, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return bans;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerBan>> GetAllBans(string steamId)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var bans = (await conn.QueryAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and steam_id = @steamId
                and (server_id is null or server_id = @serverId)
            ", new {steamId, serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return bans;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<PlayerBan>> GetAllBans()
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var bans = (await conn.QueryAsync<PlayerBan>($@"
                {SelectBans}
                where deleted_at is null
                and (server_id is null or server_id = @serverId)
            ", new {serverId = Main.AdminApi.ThisServer.Id})).ToList();
            return bans;
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
    public static async Task<DBResult> Add(PlayerBan punishment)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            punishment.SetEndAt();
            var id = await conn.QuerySingleAsync<int>(@"
                insert into iks_bans
                (steam_id, ip, name, duration, reason, ban_type, server_id, admin_id, unbanned_by, unban_reason, created_at, end_at, updated_at, deleted_at)
                values
                (@steamId, @ip, @name, @duration, @reason, @banType, @serverId, @adminId, @unbannedBy, @unbanReason, @createdAt, @endAt, @updatedAt, @deletedAt);
                select last_insert_id();
            ", new {
                steamId = punishment.SteamId,
                ip = punishment.Ip,
                name = punishment.Name,
                duration = punishment.Duration,
                reason = punishment.Reason,
                banType = punishment.BanType,
                serverId = punishment.ServerId,
                adminId = punishment.AdminId,
                unbannedBy = punishment.UnbannedBy,
                unbanReason = punishment.UnbanReason,
                createdAt = punishment.CreatedAt,
                endAt = punishment.EndAt,
                updatedAt = punishment.UpdatedAt,
                deletedAt = punishment.DeletedAt
            });
            punishment.Id = id;
            return new DBResult(id, 0, "ban added");
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            return new DBResult(null, -1, e.ToString());
        }
    }

    public static async Task<DBResult> Unban(Admin admin, PlayerBan ban, string? reason)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_bans set 
                unbanned_by = @adminId, 
                unban_reason = @reason
                where id = @banId
            ", new {
                adminId = admin.Id,
                banId = ban.Id,
                reason
            });
            return new DBResult(ban.Id, 0);
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            return new DBResult(null, -1, e.ToString());
        }
    }
    public static bool CanUnban(Admin admin, PlayerBan existingBan)
    {
        var bannedBy = existingBan.Admin;
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