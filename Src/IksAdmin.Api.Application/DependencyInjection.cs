using IksAdmin.Api.Application.Admins;
using Microsoft.Extensions.DependencyInjection;

namespace IksAdmin.Api.Application;

public static class DependencyInjection 
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminsService, AdminsService>();
        
        return services;
    }
}