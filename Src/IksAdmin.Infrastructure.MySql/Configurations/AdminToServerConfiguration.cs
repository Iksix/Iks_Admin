using IksAdmin.Api.Entities.Admins;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IksAdmin.Infrastructure.MySql.Configurations;

public class AdminToServerConfiguration : IEntityTypeConfiguration<AdminToServer>
{
    public void Configure(EntityTypeBuilder<AdminToServer> entity)
    {
        entity.ToTable("iks_admin_to_server");

        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.Id).HasColumnName("id");
        
        entity.Property(e => e.AdminId).HasColumnName("admin_id");
        entity.Property(e => e.ServerId).HasColumnName("server_id");
    }
}