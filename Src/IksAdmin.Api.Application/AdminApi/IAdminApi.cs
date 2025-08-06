using IksAdmin.Api.Application.Admins;
using IksAdmin.Api.Application.Bans;
using IksAdmin.Api.Application.Comms;
using IksAdmin.Api.Contracts;
using IksAdmin.Api.Contracts.Configs;

namespace IksAdmin.Api.Application.AdminApi;


/// <summary>
/// It contains the Api of the basic functions of the IksAdmin Core <br/>
/// Also contains links for BansManager, CommsManager etc...
/// </summary>
public interface IAdminApi
{
    public static IAdminApi Singleton
    {
        get => _singleton;
        set
        {
            if (value == null!) return;
            
            if (_singleton != null!)
            {
                throw new Exception("IAdminApi Instance is already set");
                return;
            }
            
            _singleton = value;
        }
    }
    
    private static IAdminApi _singleton = null!;
    
    /// <summary>
    /// Returns current service provider
    /// </summary>
    IServiceProvider? GetServiceProvider();
    
    void SetServiceProvider(IServiceProvider serviceProvider);

    AdminCoreConfig CoreConfig { get; set; }
    
    /// <summary>
    /// Provides interactions with admins
    /// </summary>
    IAdminsService AdminsService { get; }
    
    /// <summary>
    /// Provides interactions with bans
    /// </summary>
    IBansService BansService { get; }
    
    /// <summary>
    /// Provides interactions with comms
    /// </summary>
    ICommsService CommsService { get; }
}