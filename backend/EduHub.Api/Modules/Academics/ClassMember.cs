using EduHub.Api.Modules.Identity;
namespace EduHub.Api.Modules.Academics;
public class ClassMember{public int Id{get;set;}public int ClassRoomId{get;set;}public ClassRoom ClassRoom{get;set;}=null!;public int StudentId{get;set;}public User Student{get;set;}=null!;public DateTime JoinedAt{get;set;}=DateTime.UtcNow;public string Status{get;set;}="Active";}