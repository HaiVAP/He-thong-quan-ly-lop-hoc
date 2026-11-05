using System.Security.Claims;using EduHub.Api.Infrastructure;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace EduHub.Api.Modules.Communication;
[ApiController,Route("api/classes/{classId:int}/announcements"),Authorize]
public class AnnouncementsController(EduHubDbContext db):ControllerBase{
 int Uid=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet]public async Task<IActionResult> List(int classId){var c=await db.Classes.FindAsync(classId);if(c is null)return NotFound();if(!User.IsInRole("Admin")&&c.LecturerId!=Uid&&!await db.ClassMembers.AnyAsync(x=>x.ClassRoomId==classId&&x.StudentId==Uid&&x.Status=="Active"))return Forbid();return Ok(await db.Announcements.Include(x=>x.Author).Where(x=>x.ClassRoomId==classId).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.Title,x.Content,x.CreatedAt,Author=x.Author.FullName}).ToListAsync());}
 [HttpPost,Authorize(Roles="Lecturer")]public async Task<IActionResult> Create(int classId,Announcement r){var c=await db.Classes.FindAsync(classId);if(c is null)return NotFound();if(c.LecturerId!=Uid)return Forbid();var a=new Announcement{ClassRoomId=classId,AuthorId=Uid,Title=r.Title,Content=r.Content};db.Announcements.Add(a);await db.SaveChangesAsync();return Ok(a);}
}