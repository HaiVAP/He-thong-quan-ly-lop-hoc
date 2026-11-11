using EduHub.Api.Modules.Identity;
using EduHub.Api.Modules.Academics;
using EduHub.Api.Modules.Assignments;
using EduHub.Api.Modules.Communication;
using EduHub.Api.Modules.Materials;
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
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<Material> Materials => Set<Material>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        m.Entity<User>().HasIndex(x => x.Email).IsUnique();
        m.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        m.Entity<ClassRoom>().HasIndex(x => x.JoinCode).IsUnique();
        m.Entity<ClassMember>().HasIndex(x => new { x.ClassRoomId, x.StudentId }).IsUnique();
        m.Entity<Submission>().HasIndex(x => new { x.AssignmentId, x.StudentId, x.AttemptNumber }).IsUnique();

        // Academics relationships
        m.Entity<ClassRoom>().HasOne(x => x.Lecturer).WithMany().HasForeignKey(x => x.LecturerId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ClassRoom>().HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ClassMember>().HasOne(x => x.ClassRoom).WithMany(x => x.Members).HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<ClassMember>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // Assignments relationships
        m.Entity<Assignment>().HasOne(x => x.ClassRoom).WithMany().HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Assignment>().HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Submission>().HasOne(x => x.Assignment).WithMany(x => x.Submissions).HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Submission>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // Communication relationships
        m.Entity<Announcement>().HasOne(x => x.ClassRoom).WithMany().HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Announcement>().HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);

        // Materials relationships (Sprint 2 / Module 2)
        m.Entity<MaterialCategory>().HasOne(x => x.ClassRoom).WithMany().HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Material>().HasOne(x => x.ClassRoom).WithMany().HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Material>().HasOne(x => x.Category).WithMany(x => x.Materials).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        m.Entity<Material>().HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}
