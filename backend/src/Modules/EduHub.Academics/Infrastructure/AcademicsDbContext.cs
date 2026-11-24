using EduHub.Academics.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Academics.Infrastructure;

public class AcademicsDbContext(DbContextOptions<AcademicsDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ClassRoom> Classes => Set<ClassRoom>();
    public DbSet<ClassMember> ClassMembers => Set<ClassMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("academics");

        modelBuilder.Entity<Department>(b =>
        {
            b.ToTable("Departments");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<ClassRoom>(b =>
        {
            b.ToTable("Classes");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.JoinCode).IsUnique();
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.JoinCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.Status).IsRequired().HasMaxLength(50);

            b.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.Members)
                .WithOne(x => x.ClassRoom)
                .HasForeignKey(x => x.ClassRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClassMember>(b =>
        {
            b.ToTable("ClassMembers");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.ClassRoomId, x.StudentId }).IsUnique();
            b.Property(x => x.Status).IsRequired().HasMaxLength(50);
        });
    }
}

