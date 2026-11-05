using EduHub.Api.Modules.Identity;
using EduHub.Api.Modules.Academics;
using Microsoft.EntityFrameworkCore;
namespace EduHub.Api.Infrastructure;
public static class DemoSeeder{
 public static async Task SeedAsync(EduHubDbContext db){
  if(await db.Users.AnyAsync()) return;
  db.Users.AddRange(
   new User{FullName="Quản trị viên",Email="admin@eduhub.local",PasswordHash=PasswordHasher.Hash("Admin@123"),Role="Admin"},
   new User{FullName="Giảng viên Demo",Email="lecturer@eduhub.local",PasswordHash=PasswordHasher.Hash("Lecturer@123"),Role="Lecturer"},
   new User{FullName="Sinh viên Demo",Email="student@eduhub.local",PasswordHash=PasswordHasher.Hash("Student@123"),Role="Student"});
  db.Departments.Add(new Department{Name="Công nghệ thông tin",Code="CNTT",Description="Khoa Công nghệ thông tin"});
  await db.SaveChangesAsync();
 }
}