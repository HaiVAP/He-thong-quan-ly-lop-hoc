using EduHub.Api.Infrastructure;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace EduHub.Api.Modules.Academics;
[ApiController,Route("api/departments"),Authorize]
public class DepartmentsController(EduHubDbContext db):ControllerBase{
 [HttpGet]public async Task<IActionResult> All()=>Ok(await db.Departments.OrderBy(x=>x.Name).ToListAsync());
 [HttpPost,Authorize(Roles="Admin")]public async Task<IActionResult> Create(Department x){if(string.IsNullOrWhiteSpace(x.Name)||string.IsNullOrWhiteSpace(x.Code))return BadRequest();x.Id=0;db.Departments.Add(x);await db.SaveChangesAsync();return Ok(x);}
 [HttpPut("{id:int}"),Authorize(Roles="Admin")]public async Task<IActionResult> Update(int id,Department x){var d=await db.Departments.FindAsync(id);if(d is null)return NotFound();d.Name=x.Name;d.Code=x.Code;d.Description=x.Description;await db.SaveChangesAsync();return Ok(d);}
 [HttpDelete("{id:int}"),Authorize(Roles="Admin")]public async Task<IActionResult> Delete(int id){var d=await db.Departments.FindAsync(id);if(d is null)return NotFound();db.Departments.Remove(d);await db.SaveChangesAsync();return NoContent();}
}