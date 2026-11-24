using EduHub.Communication.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Communication.Infrastructure;

public class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options) : DbContext(options)
{
    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("communication");

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.ToTable("Announcements");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Title).HasMaxLength(255).IsRequired();
            entity.Property(a => a.Content).IsRequired();
            entity.Property(a => a.CreatedAt).IsRequired();
            entity.HasIndex(a => a.ClassRoomId);
        });
    }
}

