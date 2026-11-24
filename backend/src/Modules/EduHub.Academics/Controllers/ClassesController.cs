using System.Security.Claims;
using EduHub.Academics.Application.DTOs;
using EduHub.Academics.Application.Services;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Academics.Controllers;

[ApiController]
[Route("api/classes")]
[Authorize]
public class ClassesController(IClassService classService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsLecturer => User.IsInRole("Lecturer");
    private bool IsStudent => User.IsInRole("Student");

    /// <summary>
    /// FR-14 & FR-08: Lấy danh sách lớp học của người dùng hiện tại
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyClasses()
    {
        var classes = await classService.GetMyClassesAsync(CurrentUserId, IsLecturer, IsStudent, IsAdmin);
        return Ok(ApiResponse.Ok(classes));
    }

    /// <summary>
    /// FR-08: Xem thông tin chi tiết một lớp học
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await classService.GetByIdAsync(id, CurrentUserId, IsLecturer, IsStudent, IsAdmin);
        return Ok(ApiResponse.Ok(c));
    }

    /// <summary>
    /// FR-07 & FR-09: Giảng viên tạo lớp học mới và hệ thống tự động sinh JoinCode
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
    {
        var classRoom = await classService.CreateAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { id = classRoom.Id }, ApiResponse.Ok(classRoom, "Tạo lớp học thành công."));
    }

    /// <summary>
    /// FR-08: Giảng viên cập nhật thông tin lớp học do mình tạo
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClassRequest request)
    {
        var c = await classService.UpdateAsync(id, CurrentUserId, IsAdmin, request);
        return Ok(ApiResponse.Ok(c, "Cập nhật lớp học thành công."));
    }

    /// <summary>
    /// FR-08: Xóa lớp học
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await classService.DeleteAsync(id, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok("Đã xóa lớp học thành công."));
    }

    /// <summary>
    /// FR-10: Giảng viên tạo lại mã tham gia lớp học (Regenerate JoinCode)
    /// </summary>
    [HttpPost("{id:int}/regenerate-code")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> RegenerateCode(int id)
    {
        var newCode = await classService.RegenerateCodeAsync(id, CurrentUserId);
        return Ok(ApiResponse.Ok(new { id, joinCode = newCode }, "Tạo lại mã tham gia thành công."));
    }

    /// <summary>
    /// FR-11 & FR-12: Sinh viên tham gia lớp học bằng mã lớp
    /// </summary>
    [HttpPost("join")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Join([FromBody] JoinClassRequest request)
    {
        var (classId, className) = await classService.JoinAsync(CurrentUserId, request);
        return Ok(ApiResponse.Ok(new { id = classId, name = className }, "Tham gia lớp học thành công."));
    }

    /// <summary>
    /// Sinh viên tự rời khỏi lớp học
    /// </summary>
    [HttpDelete("{id:int}/leave")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Leave(int id)
    {
        await classService.LeaveAsync(id, CurrentUserId);
        return Ok(ApiResponse.Ok("Đã rời khỏi lớp học thành công."));
    }

    /// <summary>
    /// FR-13: Xem danh sách thành viên trong lớp học
    /// </summary>
    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var members = await classService.GetMembersAsync(id, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok(members));
    }

    /// <summary>
    /// FR-13: Giảng viên hoặc Admin xóa/loại một sinh viên ra khỏi lớp học
    /// </summary>
    [HttpDelete("{id:int}/members/{studentId:int}")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> RemoveMember(int id, int studentId)
    {
        await classService.RemoveMemberAsync(id, studentId, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok("Đã xóa sinh viên khỏi lớp học thành công."));
    }
}

