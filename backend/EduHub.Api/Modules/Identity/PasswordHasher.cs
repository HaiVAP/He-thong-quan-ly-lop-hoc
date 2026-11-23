using System.Security.Cryptography;
namespace EduHub.Api.Modules.Identity;
public static class PasswordHasher{
 public static string Hash(string password){var salt=RandomNumberGenerator.GetBytes(16);var hash=Rfc2898DeriveBytes.Pbkdf2(password,salt,210000,HashAlgorithmName.SHA256,32);return $"PBKDF2${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";}
 public static bool Verify(string password,string encoded){var p=encoded.Split('$');if(p.Length!=3||p[0]!="PBKDF2")return false;try{var salt=Convert.FromBase64String(p[1]);var expected=Convert.FromBase64String(p[2]);var actual=Rfc2898DeriveBytes.Pbkdf2(password,salt,210000,HashAlgorithmName.SHA256,expected.Length);return CryptographicOperations.FixedTimeEquals(actual,expected);}catch{return false;}}
}