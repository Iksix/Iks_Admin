using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class DBWarns
{
    private static string WarnSelect = @"
    select 
    id as id,
    admin_id as adminId,
    target_id as targetId,
    duration as duration,
    reason as reason,
    created_at as createdAt,
    updated_at as updatedAt,
    end_at as endAt,
    deleted_at as deletedAt,
    deleted_by as deletedBy
    from iks_admins_warns
    ";

    public static async Task<List<Warn>> GetAllActive() {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();

            var warns = (await conn.QueryAsync<Warn>($@"
            {WarnSelect}
            where 
            (deleted_at is null or deleted_by is null)
            and (end_at > unix_timestamp() or end_at=0)
            ")).ToList();
            return warns;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<Warn>> GetAll() {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();

            var warns = (await conn.QueryAsync<Warn>($@"
            {WarnSelect}
            ")).ToList();
            return warns;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<DBResult> InsertToBase(this Warn warn) {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();

            int id = await conn.QuerySingleAsync<int>(@"
            insert into iks_admins_warns
            (admin_id, target_id, duration, reason, created_at, end_at, updated_at)
            values
            (@adminId, @targetId, @duration, @reason, @createdAt, @endAt, @updatedAt);
            select last_insert_id();
            ", new {
                adminId = warn.AdminId,
                targetId = warn.TargetId,
                duration = warn.Duration,
                reason = warn.Reason,
                createdAt = warn.CreatedAt,
                endAt = warn.EndAt,
                updatedAt = warn.UpdatedAt,
            });

            warn.Id = id;

            return new DBResult(id, 0);
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            return new DBResult(null, -1, e.Message);
        }
    }
    public static async Task<DBResult> UpdateInBase(this Warn warn) {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            warn.UpdatedAt = AdminUtils.CurrentTimestamp();
            int id = await conn.QuerySingleAsync<int>(@"
            update iks_admins_warns set
            admin_id = @adminId,
            target_id = @targetId,
            duration = @duration,
            reason = @reason,
            end_at = @endAt,
            updated_at = @updatedAt,
            deleted_at = @deletedAt,
            deleted_by = @deletedBy
            where id = @id
            ", new {
                id = warn.Id,
                adminId = warn.AdminId,
                targetId = warn.TargetId,
                duration = warn.Duration,
                reason = warn.Reason,
                createdAt = warn.CreatedAt,
                endAt = warn.EndAt,
                updatedAt = AdminUtils.CurrentTimestamp(),
                deletedBy = warn.DeletedBy,
                deletedAt = warn.DeletedAt
            });
            return new DBResult(id, 0);
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<Warn>> GetAllActiveForAdmin(int id) {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();

            var warns = (await conn.QueryAsync<Warn>($@"
            {WarnSelect}
            where 
            target_id=@id 
            and deleted_at is null
            and (end_at > unix_timestamp() or end_at=0)
            ", new {id})).ToList();
            return warns;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<List<Warn>> GetAllActiveByAdmin(int id) {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();

            var warns = (await conn.QueryAsync<Warn>($@"
            {WarnSelect}
            where 
            admin_id=@id and
            deleted_at is null
            and (end_at > unix_timestamp() or end_at=0)
            ", new {id})).ToList();
            return warns;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
}