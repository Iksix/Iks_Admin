using IksAdmin.Api.Application.AdminApi;
using IksAdmin.Api.Application.Admins;
using IksAdmin.Api.Application.Bans;
using IksAdmin.Api.Application.Comms;
using IksAdmin.Api.Contracts;
using IksAdmin.Api.Contracts.Configs;

namespace IksAdmin.Api;

internal class AdminApi : IAdminApi
{
    /// <summary>
    /// Service provider for Lazy loads
    /// </summary>
    public static IServiceProvider? ServiceProvider = null!;
    
    public AdminApi(
        IAdminsService adminsService,
        IBansService bansService,
        ICommsService commsService,
        AdminCoreConfig coreConfig
        )
    {
        AdminsService = adminsService;
        BansService = bansService;
        CommsService = commsService;
        
        CoreConfig = coreConfig;
        
        // Устанавливаем singleton
        // IAdminApi.Singleton = this;
    }

    public IServiceProvider? GetServiceProvider() => ServiceProvider;
    
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public AdminCoreConfig CoreConfig { get; set; }
    public IAdminsService AdminsService { get; }
    public IBansService BansService { get; }
    public ICommsService CommsService { get; }
}