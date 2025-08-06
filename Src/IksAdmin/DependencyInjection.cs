using CounterStrikeSharp.API.Core;
using IksAdmin.Api;
using IksAdmin.Api.Application;
using IksAdmin.Api.Application.AdminApi;
using IksAdmin.Api.Contracts.Configs;
using IksAdmin.Infrastructure.MySql;
using Microsoft.Extensions.DependencyInjection;
using XUtils;

namespace IksAdmin;

public class DependencyInjection : IPluginServiceCollection<Main>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        AddConfigs(serviceCollection);

        serviceCollection.AddMySqlRepository();
        
        serviceCollection.AddApiServices();

        serviceCollection.CreateAdminApi();

        var provider = serviceCollection.BuildServiceProvider();

        provider.GetRequiredService<IAdminApi>().SetServiceProvider(provider);
    }

    private IServiceCollection AddConfigs(IServiceCollection services)
    {
        var coreConfig = new AdminCoreConfig();
        ConfigUtils.CreateOrRead(coreConfig, "IksAdmin", "core.json");
        
        services.AddSingleton(coreConfig);
        
        return services;
    }
}