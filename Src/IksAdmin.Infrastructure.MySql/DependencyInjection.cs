using IksAdmin.Api.Application.Admins;
using IksAdmin.Api.Contracts;
using IksAdmin.Infrastructure.MySql.Admins;
using Microsoft.Extensions.DependencyInjection;

namespace IksAdmin.Infrastructure.MySql;

public static class DependencyInjection 
{
    public static IServiceCollection AddMySqlRepository(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>();

        services.AddScoped<IAdminsRepository, AdminsRepository>();
        
        return services;
    }
}