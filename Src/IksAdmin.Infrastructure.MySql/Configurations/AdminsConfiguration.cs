using IksAdmin.Api.Entities.Admins;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IksAdmin.Infrastructure.MySql.Configurations;

public class AdminsConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> entity)
    {
        entity.ToTable("iks_admins");

        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.Id).HasColumnName("id");
        
        entity.Property(e => e.SteamId)
            .HasColumnName("steam_id")
            .HasColumnType("varchar(17)")
            .HasConversion(fromModel => fromModel.HasValue ? fromModel.Value.ToString() : "CONSOLE", toModel => toModel == "CONSOLE" ? null : ulong.Parse(toModel));
        
        entity.Property(e => e.Name).HasColumnName("name");
        entity.Property(e => e.Flags).HasColumnName("flags");
        entity.Property(e => e.Immunity).HasColumnName("immunity");
        entity.Property(e => e.GroupId).HasColumnName("group_id");
        entity.Property(e => e.Discord).HasColumnName("discord");
        entity.Property(e => e.Vk).HasColumnName("vk");
        entity.Property(e => e.IsDisabled).HasColumnName("is_disabled");
        
        
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        entity.Property(e => e.EndAt).HasColumnName("end_at");
    }
}