using OzelDers.Web.Domain.Entities;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.Web.Services.Abstractions;

public sealed record StudentOperationResult(
    bool Succeeded,
    Student? Student,
    IReadOnlyList<string> Errors);

/// <summary>
/// Öğrenci listeleme, arama, detay görüntüleme, ekleme, güncelleme, özel not yönetimi ve arşivleme işlemlerini yürüten servis sözleşmesi (FR-007, FR-010..FR-021).
/// </summary>
public interface IStudentService
{
    Task<StudentListFilterViewModel> GetStudentsForTeacherAsync(
        string teacherId,
        string? searchTerm = null,
        StudentStatusFilter statusFilter = StudentStatusFilter.Active,
        int? subjectId = null,
        CancellationToken cancellationToken = default);

    Task<StudentDetailsViewModel?> GetStudentDetailsAsync(
        string teacherId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<StudentOperationResult> CreateStudentAsync(
        string teacherId,
        CreateStudentViewModel model,
        CancellationToken cancellationToken = default);

    Task<EditStudentViewModel?> GetStudentForEditAsync(
        string teacherId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<StudentOperationResult> UpdateStudentAsync(
        string teacherId,
        EditStudentViewModel model,
        CancellationToken cancellationToken = default);

    Task<StudentOperationResult> UpdateStudentNotesAsync(
        string teacherId,
        int studentId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<StudentOperationResult> SetStudentArchiveStatusAsync(
        string teacherId,
        int studentId,
        bool isArchived,
        CancellationToken cancellationToken = default);
}
