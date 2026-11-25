using OzelDers.Web.Domain.Entities;
using OzelDers.Web.ViewModels.StudentSubjects;

namespace OzelDers.Web.Services.Abstractions;

public sealed record StudentSubjectOperationResult(
    bool Succeeded,
    StudentSubject? StudentSubject,
    string? InfoMessage,
    IReadOnlyList<string> Errors);

/// <summary>
/// Öğrenciye ders türü atama, varsayılan dersi otomatik seçili getirme,
/// öğrenciye özel saatlik ücret, oturum süresi ve haftalık oturum adedini yönetme servisi
/// (FR-025..FR-030, BR-16, DEC-14, NFR-007).
/// </summary>
public interface IStudentSubjectService
{
    Task<StudentSubjectsIndexViewModel?> GetStudentSubjectsAsync(
        string teacherId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<AssignSubjectViewModel?> PrepareAssignViewModelAsync(
        string teacherId,
        int studentId,
        AssignSubjectViewModel? existingModel = null,
        CancellationToken cancellationToken = default);

    Task<StudentSubjectOperationResult> AssignSubjectToStudentAsync(
        string teacherId,
        AssignSubjectViewModel model,
        CancellationToken cancellationToken = default);

    Task<EditStudentSubjectViewModel?> GetForEditAsync(
        string teacherId,
        int studentSubjectId,
        CancellationToken cancellationToken = default);

    Task<StudentSubjectOperationResult> UpdateStudentSubjectAsync(
        string teacherId,
        EditStudentSubjectViewModel model,
        CancellationToken cancellationToken = default);

    Task<StudentSubjectOperationResult> SetActiveStatusAsync(
        string teacherId,
        int studentSubjectId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
