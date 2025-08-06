using IksAdmin.Api.Entities.Admins;

namespace IksAdmin.Api.Application.Admins;

public interface IAdminsService
{
    IAdminsRepository GetRepository();
    
    /// <summary>
    /// Gets admins from DataBase
    /// </summary>
    Task<IEnumerable<Admin>> GetAdminsAsync();

    /// <summary>
    /// Gets admins stored in plugin
    /// </summary>
    IEnumerable<Admin> GetStoredAdmins();

    /// <summary>
    /// Getting online admins on the server <br/>
    /// Works with <see cref="GetStoredAdmins"/>
    /// </summary>
    IEnumerable<Admin> GetOnlineAdmins();
    
    /// <summary>
    /// Getting admin by <see cref="Admin.Id"/> <br/>
    /// Works with <see cref="GetStoredAdmins"/>
    /// </summary>
    Admin GetById(int id);
}