using System.Security.Cryptography;
using EduHub.Academics.Application.DTOs;
using EduHub.Academics.Domain;
using EduHub.Academics.Infrastructure;
using EduHub.Contracts;
using EduHub.Contracts.Events;
using EduHub.Shared.Events;
using EduHub.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Academics.Application.Services;

public class ClassService(AcademicsDbContext db, IUserDirectory userDirectory, IEventBus eventBus) : IClassService
{
    private static string GenerateJoinCode() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToUpperInvariant();

    public async Task<object> GetMyClassesAsync(int currentUserId, bool isLecturer, bool isStudent, bool isAdmin)
    {
        if (isLecturer)
        {
            var classes = await db.Classes
                .AsNoTracking()
                .Include(x => x.Department)
                .Where(x => x.LecturerId == currentUserId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.JoinCode,
                    c.Status,
                    c.CreatedAt,
                    Department = new { c.Department.Id, c.Department.Name, c.Department.Code },
                    MemberCount = c.Members.Count(m => m.Status == "Active")
                })
                .ToListAsync();

            return classes;
        }

        if (isStudent)
        {
            var memberships = await db.ClassMembers
                .AsNoTracking()
                .Include(x => x.ClassRoom).ThenInclude(x => x.Department)
                .Where(x => x.StudentId == currentUserId && x.Status == "Active")
                .OrderByDescending(x => x.JoinedAt)
                .ToListAsync();

            var lecturerIds = memberships.Select(m => m.ClassRoom.LecturerId).Distinct().ToList();
            var lecturerMap = await userDirectory.GetByIdsAsync(lecturerIds);

            var studentClasses = memberships.Select(m =>
            {
                lecturerMap.TryGetValue(m.ClassRoom.LecturerId, out var lecturer);
                return new
                {
                    m.ClassRoom.Id,
                    m.ClassRoom.Name,
                    m.ClassRoom.Description,
                    m.ClassRoom.JoinCode,
                    m.ClassRoom.Status,
                    m.JoinedAt,
                    Lecturer = new
                    {
                        Id = m.ClassRoom.LecturerId,
                        FullName = lecturer?.FullName ?? "Giảng viên",
                        Email = lecturer?.Email ?? ""
                    },
                    Department = new
                    {
                        m.ClassRoom.Department.Id,
                        m.ClassRoom.Department.Name,
                        m.ClassRoom.Department.Code
                    }
                };
            }).ToList();

            return studentClasses;
        }

        // Admin: lấy toàn bộ lớp học
        var allClasses = await db.Classes
            .AsNoTracking()
            .Include(x => x.Department)
            .OrderByDescending(x => x.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.JoinCode,
                c.Status,
                c.CreatedAt,
                c.LecturerId,
                Department = new { c.Department.Id, c.Department.Name, c.Department.Code },
                MemberCount = c.Members.Count(m => m.Status == "Active")
            })
            .ToListAsync();

        var adminLecturerIds = allClasses.Select(c => c.LecturerId).Distinct().ToList();
        var adminLecturerMap = await userDirectory.GetByIdsAsync(adminLecturerIds);

        var result = allClasses.Select(c =>
        {
            adminLecturerMap.TryGetValue(c.LecturerId, out var lecturer);
            return new
            {
                c.Id,
                c.Name,
                c.Description,
                c.JoinCode,
                c.Status,
                c.CreatedAt,
                Lecturer = new
                {
                    Id = c.LecturerId,
                    FullName = lecturer?.FullName ?? "Giảng viên",
                    Email = lecturer?.Email ?? ""
                },
                c.Department,
                c.MemberCount
            };
        }).ToList();

        return result;
    }

    public async Task<ClassDetailDto> GetByIdAsync(int id, int currentUserId, bool isLecturer, bool isStudent, bool isAdmin)
    {
        var c = await db.Classes
            .AsNoTracking()
            .Include(x => x.Department)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        // Kiểm tra quyền truy cập: Giảng viên phụ trách, Admin, hoặc Sinh viên là thành viên
        if (!isAdmin && c.LecturerId != currentUserId)
        {
            var isMember = await db.ClassMembers.AnyAsync(x => x.ClassRoomId == id && x.StudentId == currentUserId && x.Status == "Active");
            if (!isMember)
                throw new ForbiddenException("Bạn không có quyền truy cập lớp học này.");
        }

        var memberCount = await db.ClassMembers.CountAsync(x => x.ClassRoomId == id && x.Status == "Active");
        var lecturer = await userDirectory.GetByIdAsync(c.LecturerId);

        var lecturerSummary = lecturer is not null
            ? new LecturerSummary(lecturer.Id, lecturer.FullName, lecturer.Email)
            : new LecturerSummary(c.LecturerId, "Giảng viên", "");

        var departmentSummary = new DepartmentSummary(c.Department.Id, c.Department.Name, c.Department.Code);
        var showJoinCode = (isLecturer && c.LecturerId == currentUserId) || isAdmin;

        return new ClassDetailDto(
            c.Id,
            c.Name,
            c.Description,
            showJoinCode ? c.JoinCode : null,
            c.Status,
            c.CreatedAt,
            lecturerSummary,
            departmentSummary,
            memberCount,
            0
        );
    }

    public async Task<ClassRoom> CreateAsync(int currentUserId, CreateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Tên lớp học không được để trống.");

        if (!await db.Departments.AnyAsync(x => x.Id == request.DepartmentId))
            throw new BadRequestException("Khoa trực thuộc không tồn tại.");

        string joinCode;
        do
        {
            joinCode = GenerateJoinCode();
        } while (await db.Classes.AnyAsync(x => x.JoinCode == joinCode));

        var classRoom = new ClassRoom
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DepartmentId = request.DepartmentId,
            LecturerId = currentUserId,
            JoinCode = joinCode,
            Status = "Active"
        };

        db.Classes.Add(classRoom);
        await db.SaveChangesAsync();

        return classRoom;
    }

    public async Task<ClassRoom> UpdateAsync(int id, int currentUserId, bool isAdmin, UpdateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Tên lớp học không được để trống.");

        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (!isAdmin && c.LecturerId != currentUserId)
            throw new ForbiddenException("Bạn chỉ có thể chỉnh sửa lớp học do chính mình tạo.");

        if (!await db.Departments.AnyAsync(x => x.Id == request.DepartmentId))
            throw new BadRequestException("Khoa trực thuộc không tồn tại.");

        c.Name = request.Name.Trim();
        c.Description = request.Description?.Trim();
        c.DepartmentId = request.DepartmentId;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var validStatuses = new[] { "Active", "Archived" };
            if (validStatuses.Contains(request.Status))
                c.Status = request.Status;
        }

        await db.SaveChangesAsync();
        return c;
    }

    public async Task DeleteAsync(int id, int currentUserId, bool isAdmin)
    {
        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (!isAdmin && c.LecturerId != currentUserId)
            throw new ForbiddenException("Bạn chỉ có quyền xóa lớp học do chính mình tạo.");

        db.Classes.Remove(c);
        await db.SaveChangesAsync();
    }

    public async Task<string> RegenerateCodeAsync(int id, int currentUserId)
    {
        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (c.LecturerId != currentUserId)
            throw new ForbiddenException("Bạn chỉ có thể đổi mã tham gia của lớp do mình tạo.");

        string newCode;
        do
        {
            newCode = GenerateJoinCode();
        } while (await db.Classes.AnyAsync(x => x.JoinCode == newCode));

        c.JoinCode = newCode;
        await db.SaveChangesAsync();

        return newCode;
    }

    public async Task<(int Id, string Name)> JoinAsync(int currentUserId, JoinClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JoinCode))
            throw new BadRequestException("Mã tham gia không được để trống.");

        var code = request.JoinCode.Trim().ToUpperInvariant();
        var c = await db.Classes.SingleOrDefaultAsync(x => x.JoinCode == code);

        if (c is null || c.Status != "Active")
            throw new NotFoundException("Mã lớp không hợp lệ hoặc lớp học đã ngừng hoạt động.");

        var existingMember = await db.ClassMembers
            .SingleOrDefaultAsync(x => x.ClassRoomId == c.Id && x.StudentId == currentUserId);

        if (existingMember is not null)
        {
            if (existingMember.Status == "Active")
                throw new ConflictException("Bạn đã là thành viên của lớp học này.");

            existingMember.Status = "Active";
            existingMember.JoinedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await eventBus.PublishAsync(new StudentJoinedClassEvent(
                c.Id,
                c.Name,
                currentUserId,
                c.LecturerId,
                existingMember.JoinedAt));

            return (c.Id, c.Name);
        }

        var member = new ClassMember
        {
            ClassRoomId = c.Id,
            StudentId = currentUserId,
            JoinedAt = DateTime.UtcNow,
            Status = "Active"
        };

        db.ClassMembers.Add(member);
        await db.SaveChangesAsync();

        await eventBus.PublishAsync(new StudentJoinedClassEvent(
            c.Id,
            c.Name,
            currentUserId,
            c.LecturerId,
            member.JoinedAt));

        return (c.Id, c.Name);
    }

    public async Task LeaveAsync(int id, int currentUserId)
    {
        var member = await db.ClassMembers
            .SingleOrDefaultAsync(x => x.ClassRoomId == id && x.StudentId == currentUserId && x.Status == "Active");

        if (member is null)
            throw new NotFoundException("Bạn không phải thành viên của lớp học này.");

        db.ClassMembers.Remove(member);
        await db.SaveChangesAsync();
    }

    public async Task<List<ClassMemberDto>> GetMembersAsync(int id, int currentUserId, bool isAdmin)
    {
        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (!isAdmin && c.LecturerId != currentUserId &&
            !await db.ClassMembers.AnyAsync(x => x.ClassRoomId == id && x.StudentId == currentUserId && x.Status == "Active"))
        {
            throw new ForbiddenException("Bạn không có quyền xem danh sách thành viên lớp này.");
        }

        var members = await db.ClassMembers
            .AsNoTracking()
            .Where(x => x.ClassRoomId == id && x.Status == "Active")
            .ToListAsync();

        var studentIds = members.Select(x => x.StudentId).Distinct().ToList();
        var studentMap = await userDirectory.GetByIdsAsync(studentIds);

        var result = members.Select(x =>
        {
            studentMap.TryGetValue(x.StudentId, out var student);
            return new ClassMemberDto(
                x.Id,
                x.StudentId,
                student?.FullName ?? "Sinh viên",
                student?.Email ?? "",
                null,
                x.JoinedAt,
                x.Status
            );
        }).OrderBy(x => x.FullName).ToList();

        return result;
    }

    public async Task RemoveMemberAsync(int id, int studentId, int currentUserId, bool isAdmin)
    {
        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (!isAdmin && c.LecturerId != currentUserId)
            throw new ForbiddenException("Bạn chỉ có quyền xóa sinh viên khỏi lớp học do mình tạo.");

        var member = await db.ClassMembers
            .SingleOrDefaultAsync(x => x.ClassRoomId == id && x.StudentId == studentId);

        if (member is null)
            throw new NotFoundException("Sinh viên này không nằm trong danh sách lớp học.");

        db.ClassMembers.Remove(member);
        await db.SaveChangesAsync();
    }
}

