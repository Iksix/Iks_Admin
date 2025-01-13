using Dapper;
using IksAdminApi;
using MySqlConnector;

namespace IksAdmin;

public static class DBGroups
{
    private static string GroupSelect = @"
        select
        id as id,
        name as name,
        flags as flags,
        immunity as immunity,
        comment as comment
        from iks_groups
    ";
    public static async Task<DBResult> AddGroup(Group group)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var existingGroup = await GetGroup(group.Name);
            if (existingGroup != null)
            {
                AdminUtils.LogDebug($"Group {group.Name} already exists...");
                AdminUtils.LogDebug($"Set new group {group.Name} id = {existingGroup.Id} ✔");
                group.Id = existingGroup.Id;
                AdminUtils.LogDebug($"Update group in base...");
                return await UpdateGroupInBase(group);
            }
            AdminUtils.LogDebug($"Add group to base...");
            return await AddGroupToBase(group);
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            return new DBResult(null, -1, e.Message);
        }
    }
    public static async Task<Group?> GetGroup(string groupName)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var group = await conn.QueryFirstOrDefaultAsync<Group>($@"
                {GroupSelect}
                where name = @groupName
            ", new { groupName });

            return group;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<List<Group>> GetAllGroups(bool ignoreDeleted = true)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var groups = (await conn.QueryAsync<Group>($@"
                {GroupSelect}
            ")).ToList();

            return groups;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public static async Task<DBResult> AddGroupToBase(Group group)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var id = await conn.QuerySingleAsync<int>(@"
                insert into iks_groups
                ( name, flags, immunity, comment)
                values
                (@name, @flags, @immunity, @comment);
                select last_insert_id();
            ", new {
                name = group.Name,
                flags = group.Flags,
                immunity = group.Immunity,
                comment = group.Comment
            });
            group.Id = id;
            AdminUtils.LogDebug($"Group added to base ✔");
            AdminUtils.LogDebug($"Group id = {group!.Id} ✔");
            Main.AdminApi.Groups.Add(group);
            return new DBResult(id, 0);
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task<DBResult> UpdateGroupInBase(Group group)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_groups set 
                name = @name,
                flags = @flags,
                immunity = @immunity,
                comment = @comment
                where id = @id 
            ", new {
                id = group.Id,
                name = group.Name,
                flags = group.Flags,
                immunity = group.Immunity,
                comment = group.Comment
            });
            var pluginGroup = Main.AdminApi.Groups.FirstOrDefault(x => x.Id == group.Id);
            if (pluginGroup != null)
                pluginGroup = group;
            AdminUtils.LogDebug($"Group updated in base ✔");
            return new DBResult(group.Id, 1);
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            return new DBResult(null, -1, e.ToString());
        }
    }
    public static async Task<DBResult> DeleteGroup(Group group)
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                update iks_admins set group_id=null
                where group_id = @groupId;
                delete from iks_groups where id=@groupId
            ", new {
                groupId = group.Id
            });
            AdminUtils.LogDebug($"Group deleted ✔");
            return new DBResult(null, 0);
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            return new DBResult(null, -1, e.ToString());
        }
    }

    public static async Task RefreshGroups()
    {
        try
        {
            AdminUtils.LogDebug("Refresing groups...");
            var groups = await GetAllGroups();
            AdminUtils.LogDebug("1/2 Groups getted ✔");
            Main.AdminApi.Groups = groups;
            AdminUtils.LogDebug("2/2 Groups setted ✔");
            AdminUtils.LogDebug("Groups refreshed ✔");
            AdminUtils.LogDebug("---------------");
            AdminUtils.LogDebug("Groups:");
            AdminUtils.LogDebug("id | name | flags | immunity");
            foreach (var group in groups)
            {
                AdminUtils.LogDebug($"{group.Id} | {group.Name} | {group.Flags} | {group.Immunity}");
            }
            await RefreshLimitations();
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
    public static async Task RefreshLimitations()
    {
        try
        {
            AdminUtils.LogDebug("Refresing limitations...");
            var limitations = await GetAllLimitations();
            AdminUtils.LogDebug("1/2 limitations getted ✔");
            Main.AdminApi.GroupLimitations = limitations;
            AdminUtils.LogDebug("2/2 limitations setted ✔");
            AdminUtils.LogDebug("limitations refreshed ✔");
            AdminUtils.LogDebug("---------------");
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    private static async Task<List<GroupLimitation>> GetAllLimitations()
    {
        try
        {
            await using var conn = new MySqlConnection(DB.ConnectionString);
            await conn.OpenAsync();
            var limitations = (await conn.QueryAsync<GroupLimitation>(@"
                select
                id as id,
                group_id as groupId,
                limitation_key as limitationKey,
                limitation_value as limitationValue
                from iks_groups_limitations
            ")).ToList();
            return limitations;
        }
        catch (MySqlException e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }
}