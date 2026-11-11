using System.Security.Claims;
using System.Security.Cryptography;
using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;
using EduHub.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Modules.Academics;

[ApiController]
[Route("api/classes")]
[Authorize]
public class ClassesController(EduHubDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsLecturer => User.IsInRole("Lecturer");
    private bool IsStudent => User.IsInRole("Student");

    public record CreateClassRequest(string Name, string? Description, int DepartmentId);
    public record UpdateClassRequest(string Name, string? Description, int DepartmentId, string? Status);
    public record JoinClassRequest(string JoinCode);

    private static string GenerateJoinCode() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToUpperInvariant();

    /// <summary>
    /// FR-14 & FR-08: Lấy danh sách lớp học của người dùng hiện tại
    /// (Giảng viên: lớp do mình phụ trách; Sinh viên: lớp đã tham gia; Admin: toàn bộ lớp)
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyClasses()
    {
        if (IsLecturer)
        {
            var classes = await db.Classes
                .AsNoTracking()
                .Include(x => x.Department)
                .Where(x => x.LecturerId == CurrentUserId)
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

            return Ok(ApiResponse.Ok(classes));
        }

        if (IsStudent)
        {
            var classes = await db.ClassMembers
                .AsNoTracking()
                .Include(x => x.ClassRoom).ThenInclude(x => x.Department)
                .Include(x => x.ClassRoom).ThenInclude(x => x.Lecturer)
                .Where(x => x.StudentId == CurrentUserId && x.Status == "Active")
                .OrderByDescending(x => x.JoinedAt)
                .Select(m => new
                {
                    m.ClassRoom.Id,
                    m.ClassRoom.Name,
                    m.ClassRoom.Description,
                    m.ClassRoom.JoinCode,
                    m.ClassRoom.Status,
                    m.JoinedAt,
                    Lecturer = new { m.ClassRoom.Lecturer.Id, m.ClassRoom.Lecturer.FullName, m.ClassRoom.Lecturer.Email },
                    Department = new { m.ClassRoom.Department.Id, m.ClassRoom.Department.Name, m.ClassRoom.Department.Code }
                })
                .ToListAsync();

            return Ok(ApiResponse.Ok(classes));
        }

        // Admin: lấy toàn bộ lớp học
        var allClasses = await db.Classes
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Lecturer)
            .OrderByDescending(x => x.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.JoinCode,
                c.Status,
                c.CreatedAt,
                Lecturer = new { c.Lecturer.Id, c.Lecturer.FullName, c.Lecturer.Email },
                Department = new { c.Department.Id, c.Department.Name, c.Department.Code },
                MemberCount = c.Members.Count(m => m.Status == "Active")
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(allClasses));
    }

    /// <summary>
    /// FR-08: Xem thông tin chi tiết một lớp học
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await db.Classes
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Lecturer)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        // Kiểm tra quyền truy cập: Giảng viên phụ trách, Admin, hoặc Sinh viên là thành viên
        if (!IsAdmin && c.LecturerId != CurrentUserId)
        {
            var isMember = await db.ClassMembers.AnyAsync(x => x.ClassRoomId == id && x.StudentId == CurrentUserId && x.Status == "Active");
            if (!isMember)
                throw new ForbiddenException("Bạn không có quyền truy cập lớp học này.");
        }

        var memberCount = await db.ClassMembers.CountAsync(x => x.ClassRoomId == id && x.Status == "Active");
        var assignmentCount = await db.Assignments.CountAsync(x => x.ClassRoomId == id);

        return Ok(ApiResponse.Ok(new
        {
            c.Id,
            c.Name,
            c.Description,
            JoinCode = (IsLecturer && c.LecturerId == CurrentUserId) || IsAdmin ? c.JoinCode : null,
            c.Status,
            c.CreatedAt,
            Lecturer = new { c.Lecturer.Id, c.Lecturer.FullName, c.Lecturer.Email },
            Department = new { c.Department.Id, c.Department.Name, c.Department.Code },
            MemberCount = memberCount,
            AssignmentCount = assignmentCount
        }));
    }

    /// <summary>
    /// FR-07 & FR-09: Giảng viên tạo lớp học mới và hệ thống tự động sinh JoinCode
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
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
            LecturerId = CurrentUserId,
            JoinCode = joinCode,
            Status = "Active"
        };

        db.Classes.Add(classRoom);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = classRoom.Id }, ApiResponse.Ok(classRoom, "Tạo lớp học thành công."));
    }

    /// <summary>
    /// FR-08: Giảng viên cập nhật thông tin lớp học do mình tạo
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException("Tên lớp học không được để trống.");

        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (!IsAdmin && c.LecturerId != CurrentUserId)
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

        return Ok(ApiResponse.Ok(c, "Cập nhật lớp học thành công."));
    }

    /// <summary>
    /// FR-08: Xóa lớp học
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (!IsAdmin && c.LecturerId != CurrentUserId)
            throw new ForbiddenException("Bạn chỉ có quyền xóa lớp học do chính mình tạo.");

        db.Classes.Remove(c);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Đã xóa lớp học thành công."));
    }

    /// <summary>
    /// FR-10: Giảng viên tạo lại mã tham gia lớp học (Regenerate JoinCode)
    /// </summary>
    [HttpPost("{id:int}/regenerate-code")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> RegenerateCode(int id)
    {
        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (c.LecturerId != CurrentUserId)
            throw new ForbiddenException("Bạn chỉ có thể đổi mã tham gia của lớp do mình tạo.");

        string newCode;
        do
        {
            newCode = GenerateJoinCode();
        } while (await db.Classes.AnyAsync(x => x.JoinCode == newCode));

        c.JoinCode = newCode;
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(new { c.Id, c.JoinCode }, "Tạo lại mã tham gia thành công."));
    }

    /// <summary>
    /// FR-11 & FR-12: Sinh viên tham gia lớp học bằng mã lớp
    /// </summary>
    [HttpPost("join")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Join([FromBody] JoinClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JoinCode))
            throw new BadRequestException("Mã tham gia không được để trống.");

        var code = request.JoinCode.Trim().ToUpperInvariant();
        var c = await db.Classes.SingleOrDefaultAsync(x => x.JoinCode == code);

        if (c is null || c.Status != "Active")
            throw new NotFoundException("Mã lớp không hợp lệ hoặc lớp học đã ngừng hoạt động.");

        var existingMember = await db.ClassMembers
            .SingleOrDefaultAsync(x => x.ClassRoomId == c.Id && x.StudentId == CurrentUserId);

        if (existingMember is not null)
        {
            if (existingMember.Status == "Active")
                throw new ConflictException("Bạn đã là thành viên của lớp học này.");

            // Kích hoạt lại nếu trước đó đã rời lớp
            existingMember.Status = "Active";
            existingMember.JoinedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(ApiResponse.Ok(new { c.Id, c.Name }, "Tham gia lớp học thành công."));
        }

        var member = new ClassMember
        {
            ClassRoomId = c.Id,
            StudentId = CurrentUserId,
            JoinedAt = DateTime.UtcNow,
            Status = "Active"
        };

        db.ClassMembers.Add(member);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(new { c.Id, c.Name }, "Tham gia lớp học thành công."));
    }

    /// <summary>
    /// Sinh viên tự rời khỏi lớp học
    /// </summary>
    [HttpDelete("{id:int}/leave")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Leave(int id)
    {
        var member = await db.ClassMembers
            .SingleOrDefaultAsync(x => x.ClassRoomId == id && x.StudentId == CurrentUserId && x.Status == "Active");

        if (member is null)
            throw new NotFoundException("Bạn không phải thành viên của lớp học này.");

        db.ClassMembers.Remove(member);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Đã rời khỏi lớp học thành công."));
    }

    /// <summary>
    /// FR-13: Xem danh sách thành viên trong lớp học
    /// </summary>
    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        // Giảng viên lớp, Admin, hoặc thành viên trong lớp mới được xem danh sách
        if (!IsAdmin && c.LecturerId != CurrentUserId &&
            !await db.ClassMembers.AnyAsync(x => x.ClassRoomId == id && x.StudentId == CurrentUserId && x.Status == "Active"))
        {
            throw new ForbiddenException("Bạn không có quyền xem danh sách thành viên lớp này.");
        }

        var members = await db.ClassMembers
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.ClassRoomId == id && x.Status == "Active")
            .OrderBy(x => x.Student.FullName)
            .Select(x => new
            {
                x.Id,
                x.StudentId,
                x.Student.FullName,
                x.Student.Email,
                x.Student.Phone,
                x.JoinedAt,
                x.Status
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(members));
    }

    /// <summary>
    /// FR-13: Giảng viên hoặc Admin xóa/loại một sinh viên ra khỏi lớp học
    /// </summary>
    [HttpDelete("{id:int}/members/{studentId:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> RemoveMember(int id, int studentId)
    {
        var c = await db.Classes.FindAsync(id);
        if (c is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {id}.");

        if (!IsAdmin && c.LecturerId != CurrentUserId)
            throw new ForbiddenException("Bạn chỉ có quyền xóa sinh viên khỏi lớp học do mình tạo.");

        var member = await db.ClassMembers
            .SingleOrDefaultAsync(x => x.ClassRoomId == id && x.StudentId == studentId);

        if (member is null)
            throw new NotFoundException("Sinh viên này không nằm trong danh sách lớp học.");

        db.ClassMembers.Remove(member);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Đã xóa sinh viên khỏi lớp học thành công."));
    }
}
