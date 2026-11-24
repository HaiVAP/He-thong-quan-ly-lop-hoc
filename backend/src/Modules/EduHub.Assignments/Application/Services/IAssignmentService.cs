using EduHub.Assignments.Application.DTOs;
using EduHub.Assignments.Domain;

namespace EduHub.Assignments.Application.Services;

public interface IAssignmentService
{
    Task<List<Assignment>> ListByClassAsync(int classId, int currentUserId, bool isAdmin);
    Task<Assignment> CreateAsync(int classId, int currentUserId, bool isAdmin, CreateAssignmentRequest request);
    Task<Submission> SubmitAsync(int assignmentId, int currentUserId, SubmitAssignmentRequest request);
    Task<List<SubmissionResponseDto>> GetSubmissionsAsync(int assignmentId, int currentUserId, bool isAdmin);
    Task<Submission> GradeAsync(int submissionId, int currentUserId, bool isAdmin, GradeAssignmentRequest request);
}

