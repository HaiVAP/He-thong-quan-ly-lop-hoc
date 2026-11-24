using System.Security.Claims;
using EduHub.Assignments.Application.DTOs;
using EduHub.Assignments.Application.Services;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Assignments.Controllers;

[ApiController]
[Authorize]
public class AssignmentsController(IAssignmentService assignmentService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    /// <summary>
    /// FR-19: Xem danh sách bài tập trong lớp học
    /// </summary>
    [HttpGet("api/classes/{classId:int}/assignments")]
    public async Task<IActionResult> ListByClass(int classId)
    {
        var assignments = await assignmentService.ListByClassAsync(classId, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok(assignments));
    }

    /// <summary>
    /// FR-18: Giảng viên tạo bài tập mới cho lớp
    /// </summary>
    [HttpPost("api/classes/{classId:int}/assignments")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Create(int classId, [FromBody] CreateAssignmentRequest request)
    {
        var assignment = await assignmentService.CreateAsync(classId, CurrentUserId, IsAdmin, request);
        return Ok(ApiResponse.Ok(assignment, "Tạo bài tập thành công."));
    }

    /// <summary>
    /// FR-20: Sinh viên nộp bài tập trực tuyến
    /// </summary>
    [HttpPost("api/assignments/{id:int}/submissions")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitAssignmentRequest request)
    {
        var submission = await assignmentService.SubmitAsync(id, CurrentUserId, request);
        return Ok(ApiResponse.Ok(submission, "Nộp bài tập thành công."));
    }

    /// <summary>
    /// FR-21: Giảng viên xem danh sách bài nộp của sinh viên
    /// </summary>
    [HttpGet("api/assignments/{id:int}/submissions")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> GetSubmissions(int id)
    {
        var submissions = await assignmentService.GetSubmissionsAsync(id, CurrentUserId, IsAdmin);
        return Ok(ApiResponse.Ok(submissions));
    }

    /// <summary>
    /// FR-22: Giảng viên chấm điểm và nhận xét bài nộp
    /// </summary>
    [HttpPut("api/submissions/{id:int}/grade")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Grade(int id, [FromBody] GradeAssignmentRequest request)
    {
        var submission = await assignmentService.GradeAsync(id, CurrentUserId, IsAdmin, request);
        return Ok(ApiResponse.Ok(submission, "Chấm điểm bài nộp thành công."));
    }
}

