using EduHub.Api.Infrastructure;
using EduHub.Api.Modules.Academics;
using EduHub.Api.Modules.Identity;
using EduHub.Api.Modules.Materials;
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

        // 5. Seed Material Categories & Materials (Sprint 2 / Module 2)
        var cat1 = new MaterialCategory
        {
            ClassRoomId = class1.Id,
            Name = "Chương 1: Tổng quan & Kiến trúc Modular Monolith",
            Description = "Lý thuyết kiến trúc phần mềm và thiết kế mô-đun",
            SortOrder = 1
        };

        var cat2 = new MaterialCategory
        {
            ClassRoomId = class1.Id,
            Name = "Chương 2: Thiết kế REST API & EF Core",
            Description = "Thực hành xây dựng API với .NET 8 và PostgreSQL",
            SortOrder = 2
        };

        db.MaterialCategories.AddRange(cat1, cat2);
        await db.SaveChangesAsync();

        db.Materials.AddRange(
            new Material
            {
                ClassRoomId = class1.Id,
                CategoryId = cat1.Id,
                CreatedById = lecturer1.Id,
                Title = "Slide Bài giảng Chương 1 - Kiến trúc Modular Monolith",
                Description = "Tài liệu lý thuyết tổng quan về kiến trúc và đóng gói Docker",
                FileName = "BaiGiang_Chuong1_Modular_Monolith.pdf",
                FileKey = "20261111/demo_chuong1_slide.pdf",
                FileUrl = "/api/files/download/20261111/demo_chuong1_slide.pdf?filename=BaiGiang_Chuong1_Modular_Monolith.pdf",
                MimeType = "application/pdf",
                FileSizeBytes = 2450000
            },
            new Material
            {
                ClassRoomId = class1.Id,
                CategoryId = cat2.Id,
                CreatedById = lecturer1.Id,
                Title = "Tài liệu Hướng dẫn Thực hành Web API với EF Core",
                Description = "Hướng dẫn chi tiết xây dựng Controller, DbContext và JWT Authentication",
                FileName = "HuongDan_ThucHanh_WebAPI.docx",
                FileKey = "20261111/demo_huong_dan_api.docx",
                FileUrl = "/api/files/download/20261111/demo_huong_dan_api.docx?filename=HuongDan_ThucHanh_WebAPI.docx",
                MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileSizeBytes = 1120000
            }
        );
        await db.SaveChangesAsync();
    }
}
