using EduHub.Api.Common.Models;
using EduHub.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Modules.Statistics;

[ApiController]
[Route("api/statistics")]
[Authorize(Roles = "Admin")]
public class StatisticsController(EduHubDbContext db) : ControllerBase
{
    /// <summary>
    /// FR-31: Admin xem thống kê tổng quan toàn hệ thống
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var totalUsers = await db.Users.CountAsync();
        var totalLecturers = await db.Users.CountAsync(x => x.Role == "Lecturer");
        var totalStudents = await db.Users.CountAsync(x => x.Role == "Student");
        var totalDepartments = await db.Departments.CountAsync();
        var totalClasses = await db.Classes.CountAsync();
        var totalAssignments = await db.Assignments.CountAsync();
        var totalSubmissions = await db.Submissions.CountAsync();

        return Ok(ApiResponse.Ok(new
        {
            Users = totalUsers,
            Lecturers = totalLecturers,
            Students = totalStudents,
            Departments = totalDepartments,
            Classes = totalClasses,
            Assignments = totalAssignments,
            Submissions = totalSubmissions
        }));
    }
}
