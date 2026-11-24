using EduHub.Assignments.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Assignments.Infrastructure;

public class AssignmentsDbContext(DbContextOptions<AssignmentsDbContext> options) : DbContext(options)
{
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("assignments");

        modelBuilder.Entity<Assignment>(b =>
        {
            b.ToTable("Assignments");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Status).IsRequired().HasMaxLength(50);

            b.HasMany(x => x.Submissions)
                .WithOne(x => x.Assignment)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Submission>(b =>
        {
            b.ToTable("Submissions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.AssignmentId, x.StudentId, x.AttemptNumber }).IsUnique();
            b.Property(x => x.Score).HasPrecision(5, 2);
        });
    }
}

