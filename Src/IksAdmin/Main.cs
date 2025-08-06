using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using IksAdmin.Api.Application.AdminApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XUtils;

namespace IksAdmin;

public class Main : BasePlugin
{
    public override string ModuleName { get; } = "IksAdmin";
    
    public override string ModuleVersion { get; } = "3.1.0";
    
    public override string ModuleAuthor { get; } = "iks__";

    /// <summary>
    /// ServiceCollection for Lazy load in <see cref="Load"/>
    /// </summary>
    public static IServiceCollection ServiceCollection { get; set; } = null!;
    
    private readonly IAdminApi _adminApi;

    public Main(IAdminApi adminApi)
    {
        _adminApi = adminApi;
    }

    public override void Load(bool hotReload)
    {
        Console.WriteLine("IksAdmin Load");
        Console.WriteLine($"Server name: {_adminApi.CoreConfig.ServerName}");
        Console.WriteLine("Getting admins list:");
        
        Task.Run(async () =>
        {
            var admins = await _adminApi.AdminsService.GetAdminsAsync();

            foreach (var admin in admins)
            {
                Console.WriteLine(admin);
            }
        });
    }
}