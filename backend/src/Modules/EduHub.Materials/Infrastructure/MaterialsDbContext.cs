using EduHub.Materials.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Materials.Infrastructure;

public class MaterialsDbContext(DbContextOptions<MaterialsDbContext> options) : DbContext(options)
{
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("materials");

        modelBuilder.Entity<MaterialCategory>(entity =>
        {
            entity.ToTable("MaterialCategories");
            entity.HasKey(x => x.Id);
            entity.HasMany(x => x.Materials)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.ToTable("Materials");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ClassRoomId);
            entity.HasIndex(x => x.CategoryId);
        });
    }
}
