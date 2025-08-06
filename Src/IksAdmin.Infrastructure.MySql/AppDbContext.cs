using IksAdmin.Api.Contracts;
using IksAdmin.Api.Contracts.Configs;
using IksAdmin.Api.Entities.Admins;
using IksAdmin.Infrastructure.MySql.Configurations;
using Microsoft.EntityFrameworkCore;

namespace IksAdmin.Infrastructure.MySql;

public class AppDbContext : DbContext
{
    private readonly AdminCoreConfig _config;

    public AppDbContext(AdminCoreConfig config)
    {
        _config = config;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AdminsConfiguration());
    }

    public DbSet<Admin> Admins { get; set; }
    
    public DbSet<AdminToServer> AdminToServers { get; set; }
}