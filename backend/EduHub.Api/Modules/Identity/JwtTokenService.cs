using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace EduHub.Api.Modules.Identity;
public class JwtTokenService(IConfiguration cfg){
 public string Create(User user){var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Secret"]!));var creds=new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
 var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Name,user.FullName),new Claim(ClaimTypes.Email,user.Email),new Claim(ClaimTypes.Role,user.Role)};
 var token=new JwtSecurityToken(cfg["Jwt:Issuer"],cfg["Jwt:Audience"],claims,expires:DateTime.UtcNow.AddMinutes(int.Parse(cfg["Jwt:ExpiryMinutes"]??"240")),signingCredentials:creds);return new JwtSecurityTokenHandler().WriteToken(token);}
}