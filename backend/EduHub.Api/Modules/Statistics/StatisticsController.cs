using EduHub.Api.Infrastructure;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace EduHub.Api.Modules.Statistics;
[ApiController,Route("api/statistics"),Authorize(Roles="Admin")]
public class StatisticsController(EduHubDbContext db):ControllerBase{
 [HttpGet("overview")]public async Task<IActionResult> Overview()=>Ok(new{users=await db.Users.CountAsync(),lecturers=await db.Users.CountAsync(x=>x.Role=="Lecturer"),students=await db.Users.CountAsync(x=>x.Role=="Student"),departments=await db.Departments.CountAsync(),classes=await db.Classes.CountAsync(),assignments=await db.Assignments.CountAsync(),submissions=await db.Submissions.CountAsync()});
}