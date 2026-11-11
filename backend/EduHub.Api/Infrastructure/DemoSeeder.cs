using EduHub.Api.Infrastructure;
using EduHub.Api.Modules.Academics;
using EduHub.Api.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Infrastructure;

public static class DemoSeeder
{
    public static async Task SeedAsync(EduHubDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        // 1. Seed Users (Admin, Lecturers, Students)
        var admin = new User
        {
            FullName = "Quản trị viên Hệ thống",
            Email = "admin@eduhub.local",
            PasswordHash = PasswordHasher.Hash("Admin@123"),
            Role = "Admin",
            Status = "Active"
        };

        var lecturer1 = new User
        {
            FullName = "ThS. Nguyễn Văn Giảng",
            Email = "lecturer@eduhub.local",
            PasswordHash = PasswordHasher.Hash("Lecturer@123"),
            Role = "Lecturer",
            Status = "Active",
            Phone = "0901234567"
        };

        var lecturer2 = new User
        {
            FullName = "TS. Trần Thị Hương",
            Email = "huong.tran@eduhub.local",
            PasswordHash = PasswordHasher.Hash("Lecturer@123"),
            Role = "Lecturer",
            Status = "Active",
            Phone = "0909876543"
        };

        var student1 = new User
        {
            FullName = "Lê Văn Học",
            Email = "student@eduhub.local",
            PasswordHash = PasswordHasher.Hash("Student@123"),
            Role = "Student",
            Status = "Active",
            Phone = "0912345678"
        };

        var student2 = new User
        {
            FullName = "Phạm Thị Mai",
            Email = "mai.pham@eduhub.local",
            PasswordHash = PasswordHasher.Hash("Student@123"),
            Role = "Student",
            Status = "Active",
            Phone = "0923456789"
        };

        var student3 = new User
        {
            FullName = "Hoàng Minh Tuấn",
            Email = "tuan.hoang@eduhub.local",
            PasswordHash = PasswordHasher.Hash("Student@123"),
            Role = "Student",
            Status = "Active",
            Phone = "0934567890"
        };

        db.Users.AddRange(admin, lecturer1, lecturer2, student1, student2, student3);
        await db.SaveChangesAsync();

        // 2. Seed Departments
        var deptCntt = new Department
        {
            Name = "Công nghệ Thông tin",
            Code = "CNTT",
            Description = "Khoa Công nghệ thông tin và Truyền thông"
        };

        var deptKhmt = new Department
        {
            Name = "Khoa học Máy tính",
            Code = "KHMT",
            Description = "Khoa Khoa học máy tính & Trí tuệ nhân tạo"
        };

        var deptKtpm = new Department
        {
            Name = "Kỹ thuật Phần mềm",
            Code = "KTPM",
            Description = "Khoa Kỹ thuật phần mềm & Hệ thống thông tin"
        };

        db.Departments.AddRange(deptCntt, deptKhmt, deptKtpm);
        await db.SaveChangesAsync();

        // 3. Seed Classes
        var class1 = new ClassRoom
        {
            Name = "Lập trình Web nâng cao - Nhóm 01",
            Description = "Môn học chuyên sâu về ASP.NET Core & React TypeScript",
            DepartmentId = deptCntt.Id,
            LecturerId = lecturer1.Id,
            JoinCode = "WEB01ADV",
            Status = "Active"
        };

        var class2 = new ClassRoom
        {
            Name = "Kiến trúc Phần mềm Modular Monolith",
            Description = "Tìm hiểu thiết kế kiến trúc phần mềm hiện đại",
            DepartmentId = deptKtpm.Id,
            LecturerId = lecturer1.Id,
            JoinCode = "ARCH2026",
            Status = "Active"
        };

        var class3 = new ClassRoom
        {
            Name = "Nhập môn Trí tuệ Nhân tạo",
            Description = "Lý thuyết và thực hành Machine Learning cơ bản",
            DepartmentId = deptKhmt.Id,
            LecturerId = lecturer2.Id,
            JoinCode = "AI101BAS",
            Status = "Active"
        };

        db.Classes.AddRange(class1, class2, class3);
        await db.SaveChangesAsync();

        // 4. Seed Class Members (Enrollments)
        db.ClassMembers.AddRange(
            new ClassMember { ClassRoomId = class1.Id, StudentId = student1.Id, Status = "Active" },
            new ClassMember { ClassRoomId = class1.Id, StudentId = student2.Id, Status = "Active" },
            new ClassMember { ClassRoomId = class1.Id, StudentId = student3.Id, Status = "Active" },
            new ClassMember { ClassRoomId = class2.Id, StudentId = student1.Id, Status = "Active" }
        );

        await db.SaveChangesAsync();
    }
}
