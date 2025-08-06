using IksAdmin.Api.Entities.Bans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IksAdmin.Infrastructure.MySql.Configurations;

public class BansConfiguration: IEntityTypeConfiguration<Ban>
{
    public void Configure(EntityTypeBuilder<Ban> entity)
    {
        entity.ToTable("iks_bans");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.SteamId).HasColumnName("steam_id");
        entity.Property(e => e.Ip).HasColumnName("ip");
        entity.Property(e => e.PlayerName).HasColumnName("name");
        entity.Property(e => e.Duration).HasColumnName("duration");
        entity.Property(e => e.Reason).HasColumnName("reason");
        entity.Property(e => e.BanType)
            .HasColumnName("ban_type")
            .HasColumnType("tinyint")
            .HasConversion( v => (int)v,
                            v => (BanType)v );
            
        entity.Property(e => e.ServerId).HasColumnName("server_id");
        entity.Property(e => e.AdminId).HasColumnName("admin_id");
        entity.Property(e => e.UnbannedBy).HasColumnName("unbanned_by");
        entity.Property(e => e.UnbanReason).HasColumnName("unban_reason");
        
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        entity.Property(e => e.EndAt).HasColumnName("end_at");
    }
    
}