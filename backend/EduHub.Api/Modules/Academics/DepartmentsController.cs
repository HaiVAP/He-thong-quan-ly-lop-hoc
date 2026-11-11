using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;
using EduHub.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Modules.Academics;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController(EduHubDbContext db) : ControllerBase
{
    public record CreateDepartmentRequest(string Name, string Code, string? Description);
    public record UpdateDepartmentRequest(string Name, string Code, string? Description);

    /// <summary>
    /// FR-06: Xem danh sách các khoa
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await db.Departments
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(ApiResponse.Ok(departments));
    }

    /// <summary>
    /// FR-06: Xem thông tin chi tiết một khoa
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var department = await db.Departments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);

        if (department is null)
            throw new NotFoundException($"Không tìm thấy khoa có ID {id}.");

        return Ok(ApiResponse.Ok(department));
    }

    /// <summary>
    /// FR-05: Admin thêm mới khoa
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Tên khoa và Mã khoa không được để trống.");

        var code = request.Code.Trim().ToUpper();
        if (await db.Departments.AnyAsync(x => x.Code == code))
            throw new ConflictException($"Mã khoa '{code}' đã tồn tại trong hệ thống.");

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description?.Trim()
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = department.Id }, ApiResponse.Ok(department, "Thêm khoa mới thành công."));
    }

    /// <summary>
    /// FR-05: Admin cập nhật thông tin khoa
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Tên khoa và Mã khoa không được để trống.");

        var department = await db.Departments.FindAsync(id);
        if (department is null)
            throw new NotFoundException($"Không tìm thấy khoa có ID {id}.");

        var code = request.Code.Trim().ToUpper();
        if (await db.Departments.AnyAsync(x => x.Code == code && x.Id != id))
            throw new ConflictException($"Mã khoa '{code}' đã được sử dụng bởi khoa khác.");

        department.Name = request.Name.Trim();
        department.Code = code;
        department.Description = request.Description?.Trim();

        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(department, "Cập nhật thông tin khoa thành công."));
    }

    /// <summary>
    /// FR-05: Admin xóa khoa
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await db.Departments.FindAsync(id);
        if (department is null)
            throw new NotFoundException($"Không tìm thấy khoa có ID {id}.");

        var hasClasses = await db.Classes.AnyAsync(x => x.DepartmentId == id);
        if (hasClasses)
            throw new BadRequestException("Không thể xóa khoa đang có lớp học trực thuộc.");

        db.Departments.Remove(department);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Đã xóa khoa thành công."));
    }
}
