using EduHub.Identity.Domain;
using EduHub.Academics.Domain;
using EduHub.Assignments.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Infrastructure;

public class EduHubDbContext(DbContextOptions<EduHubDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ClassRoom> Classes => Set<ClassRoom>();
    public DbSet<ClassMember> ClassMembers => Set<ClassMember>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        m.Entity<User>().ToTable("Users", "identity");
        m.Entity<User>().HasIndex(x => x.Email).IsUnique();

        m.Entity<Department>().ToTable("Departments", "academics");
        m.Entity<Department>().HasIndex(x => x.Code).IsUnique();

        m.Entity<ClassRoom>().ToTable("Classes", "academics");
        m.Entity<ClassRoom>().HasIndex(x => x.JoinCode).IsUnique();

        m.Entity<ClassMember>().ToTable("ClassMembers", "academics");
        m.Entity<ClassMember>().HasIndex(x => new { x.ClassRoomId, x.StudentId }).IsUnique();

        m.Entity<Assignment>().ToTable("Assignments", "assignments");
        m.Entity<Submission>().ToTable("Submissions", "assignments");
        m.Entity<Submission>().HasIndex(x => new { x.AssignmentId, x.StudentId, x.AttemptNumber }).IsUnique();

        // Academics internal relationships
        m.Entity<ClassRoom>().HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ClassMember>().HasOne(x => x.ClassRoom).WithMany(x => x.Members).HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Cascade);

        // Assignments internal relationships
        m.Entity<Submission>().HasOne(x => x.Assignment).WithMany(x => x.Submissions).HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);

    }
}
