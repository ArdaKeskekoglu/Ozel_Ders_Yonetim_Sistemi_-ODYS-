using OzelDers.Web.Domain.Entities;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.Web.Services.Abstractions;

public sealed record StudentOperationResult(
    bool Succeeded,
    Student? Student,
    IReadOnlyList<string> Errors);

/// <summary>
/// Öğrenci listeleme, arama, ekleme ve yönetim işlemlerini yürüten servis sözleşmesi (FR-007, FR-010..FR-021).
/// </summary>
public interface IStudentService
{
    Task<StudentListFilterViewModel> GetStudentsForTeacherAsync(
        string teacherId,
        string? searchTerm = null,
        StudentStatusFilter statusFilter = StudentStatusFilter.Active,
        CancellationToken cancellationToken = default);

    Task<StudentOperationResult> CreateStudentAsync(
        string teacherId,
        CreateStudentViewModel model,
        CancellationToken cancellationToken = default);
}
