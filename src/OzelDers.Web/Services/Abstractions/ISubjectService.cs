using OzelDers.Web.Domain.Entities;
using OzelDers.Web.ViewModels.Subjects;

namespace OzelDers.Web.Services.Abstractions;

public sealed record SubjectOperationResult(
    bool Succeeded,
    Subject? Subject,
    string? InfoMessage,
    IReadOnlyList<string> Errors);

/// <summary>
/// Öğretmenin ders türlerini listeleme, ekleme, düzenleme, varsayılan ders seçimi ve
/// güvenli arşivleme/yeniden etkinleştirme işlemlerini yürüten servis sözleşmesi
/// (FR-022..FR-024, BR-16, NFR-007).
/// </summary>
public interface ISubjectService
{
    Task<SubjectListFilterViewModel> GetSubjectsForTeacherAsync(
        string teacherId,
        SubjectStatusFilter statusFilter = SubjectStatusFilter.Active,
        CancellationToken cancellationToken = default);

    Task<SubjectOperationResult> CreateSubjectAsync(
        string teacherId,
        CreateSubjectViewModel model,
        CancellationToken cancellationToken = default);

    Task<EditSubjectViewModel?> GetSubjectForEditAsync(
        string teacherId,
        int subjectId,
        CancellationToken cancellationToken = default);

    Task<SubjectOperationResult> UpdateSubjectAsync(
        string teacherId,
        EditSubjectViewModel model,
        CancellationToken cancellationToken = default);

    Task<SubjectOperationResult> SetDefaultSubjectAsync(
        string teacherId,
        int subjectId,
        CancellationToken cancellationToken = default);

    Task<SubjectOperationResult> SetSubjectArchiveStatusAsync(
        string teacherId,
        int subjectId,
        bool isArchived,
        CancellationToken cancellationToken = default);
}
