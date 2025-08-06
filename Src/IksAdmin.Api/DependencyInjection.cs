using CounterStrikeSharp.API.Core;
using IksAdmin.Api.Application;
using IksAdmin.Api.Application.AdminApi;
using IksAdmin.Api.Application.Admins;
using IksAdmin.Api.Application.Bans;
using IksAdmin.Api.Application.Comms;
using IksAdmin.Api.Contracts.Configs;
using IksAdmin.Infrastructure.MySql;
using Microsoft.Extensions.DependencyInjection;
using XUtils;

namespace IksAdmin.Api;

public static class DependencyInjection 
{
    /// <summary>
    /// Used for creating and register AdminApi in DI container, NEVER USE THAT!!!
    /// </summary>
    public static IServiceCollection CreateAdminApi(this IServiceCollection services)
    {
        if (AdminApi.ServiceProvider != null)
        {
            throw new Exception("Service provider with created IAdminApi already exists");
        }
        
        services.AddSingleton<IAdminApi, AdminApi>(x => new AdminApi(
            x.GetRequiredService<IAdminsService>(),
            x.GetRequiredService<IBansService>(),
            x.GetRequiredService<ICommsService>(),
            x.GetRequiredService<AdminCoreConfig>()
        ));
        
        // TODO: Подумать нужно ли вообще делать разные варианты получения API, или оставить только этот Singleton
        IAdminApi.Singleton = services.BuildServiceProvider().GetRequiredService<IAdminApi>();
        
        return services;
    }
    
    /// <summary>
    /// Use this for add <see cref="IAdminApi"/> to modules
    /// <br/><br/>
    /// This method adds: <br/>
    /// <see cref="IAdminApi"/>
    /// <see cref="IAdminsService"/>
    /// <see cref="IAdminsRepository"/>
    /// and etc. to your service collection
    /// </summary>
    public static IAdminApi AddAdminApi(this IServiceCollection services)
    {
        services.AddAdminConfigs();
        
        services.AddRepositories();

        services.AddServices();
        
        services.AddSingleton<IAdminApi>(AdminApi.ServiceProvider!.GetRequiredService<IAdminApi>());
        
        return services.BuildServiceProvider().GetRequiredService<IAdminApi>();
    }
    
    public static IServiceCollection AddAdminConfigs(this IServiceCollection services)
    {
        var coreConfig = new AdminCoreConfig();
        ConfigUtils.CreateOrRead(coreConfig, "IksAdmin", "core.json");
        
        services.AddSingleton(coreConfig);
        
        return services;
    }
    
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services.AddMySqlRepository();
    }
    
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        return services.AddApiServices();
    }
}