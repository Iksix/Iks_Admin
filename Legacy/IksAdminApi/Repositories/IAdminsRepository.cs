namespace IksAdminApi.Repositories;

public interface IAdminsRepository
{
    /// <summary>
    /// Method returns All Admins
    /// </summary>
    /// /// <param name="ignoreDeleted">
    /// if true: admins with deleted_at != null will be ignored
    /// </param>
    Task<IEnumerable<Admin>> GetAll(bool ignoreDeleted = true);

    /// <summary>
    /// Method returns All Admins with specified server_id
    /// </summary>
    /// <param name="serverId">
    /// server_id from iks_servers table or IksAdmin configuration
    /// </param>
    /// <param name="ignoreDeleted">
    /// if true: admins with deleted_at != null will be ignored
    /// </param>
    Task<IEnumerable<Admin>> GetAll(int? serverId, bool ignoreDeleted = true);
}