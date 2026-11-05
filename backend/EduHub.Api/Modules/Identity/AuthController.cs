using System.Security.Claims;
using EduHub.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EduHub.Api.Modules.Identity;
[ApiController,Route("api/auth")]
public class AuthController(EduHubDbContext db,JwtTokenService tokens):ControllerBase{
 public record RegisterRequest(string FullName,string Email,string Password); public record LoginRequest(string Email,string Password); public record ProfileRequest(string FullName,string? Phone);
 [HttpPost("register")] public async Task<IActionResult> Register(RegisterRequest r){if(string.IsNullOrWhiteSpace(r.FullName)||r.Password.Length<8)return BadRequest(new{message="Họ tên bắt buộc; mật khẩu tối thiểu 8 ký tự."});var email=r.Email.Trim().ToLowerInvariant();if(await db.Users.AnyAsync(x=>x.Email==email))return Conflict(new{message="Email đã được sử dụng."});var u=new User{FullName=r.FullName.Trim(),Email=email,PasswordHash=PasswordHasher.Hash(r.Password)};db.Users.Add(u);await db.SaveChangesAsync();return Ok(new{u.Id,u.FullName,u.Email,u.Role});}
 [HttpPost("login")] public async Task<IActionResult> Login(LoginRequest r){var u=await db.Users.SingleOrDefaultAsync(x=>x.Email==r.Email.Trim().ToLowerInvariant());if(u is null||u.Status!="Active"||!PasswordHasher.Verify(r.Password,u.PasswordHash))return Unauthorized(new{message="Thông tin đăng nhập không hợp lệ hoặc tài khoản đã khóa."});return Ok(new{token=tokens.Create(u),user=new{u.Id,u.FullName,u.Email,u.Role}});}
 [Authorize,HttpGet("me")] public async Task<IActionResult> Me(){var id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var u=await db.Users.FindAsync(id);return u is null?NotFound():Ok(new{u.Id,u.FullName,u.Email,u.Role,u.Status});}
 [Authorize,HttpPut("profile")] public async Task<IActionResult> Profile(ProfileRequest r){var id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var u=await db.Users.FindAsync(id);if(u is null)return NotFound();u.FullName=r.FullName.Trim();u.Phone=r.Phone;await db.SaveChangesAsync();return Ok(new{u.Id,u.FullName,u.Phone});}
}