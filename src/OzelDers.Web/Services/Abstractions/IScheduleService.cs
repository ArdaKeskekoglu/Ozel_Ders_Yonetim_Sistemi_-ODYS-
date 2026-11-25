using OzelDers.Web.Domain.Entities;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.Web.Services.Abstractions;

public sealed record ScheduleLessonOperationResult(
    bool Succeeded,
    Lesson? Lesson,
    string? InfoMessage,
    IReadOnlyList<string> Errors);

/// <summary>
/// Öğretmenin haftalık ders çizelgesini, hafta başlangıç/bitiş sınırlarını (Pazartesi–Pazar),
/// önceki/sonraki hafta navigasyon verilerini, manuel ders ekleme, ders düzenleme/taşıma,
/// iptal, erteleme, çakışma/eşzamanlılık kontrolü ve uygun koşullarda ders kaldırma
/// işlemlerini yürüten servis sözleşmesi (FR-008, FR-031..FR-045, FR-059).
/// </summary>
public interface IScheduleService
{
    DateOnly GetMondayOfWeek(DateOnly referenceDate);

    Task<WeeklyScheduleViewModel> GetWeeklyScheduleAsync(
        string teacherId,
        DateOnly? referenceDate = null,
        CancellationToken cancellationToken = default);

    Task<CreateLessonViewModel> PrepareCreateLessonViewModelAsync(
        string teacherId,
        DateOnly? date = null,
        int? studentId = null,
        CreateLessonViewModel? existingModel = null,
        CancellationToken cancellationToken = default);

    Task<ScheduleLessonOperationResult> CreateLessonAsync(
        string teacherId,
        CreateLessonViewModel model,
        CancellationToken cancellationToken = default);

    Task<EditLessonViewModel?> GetLessonForEditAsync(
        string teacherId,
        int lessonId,
        EditLessonViewModel? existingModel = null,
        CancellationToken cancellationToken = default);

    Task<ScheduleLessonOperationResult> UpdateLessonAsync(
        string teacherId,
        EditLessonViewModel model,
        CancellationToken cancellationToken = default);

    Task<ScheduleLessonOperationResult> CancelLessonAsync(
        string teacherId,
        int lessonId,
        string? cancelReason = null,
        CancellationToken cancellationToken = default);

    Task<ScheduleLessonOperationResult> RestoreCancelledLessonAsync(
        string teacherId,
        int lessonId,
        CancellationToken cancellationToken = default);

    Task<RescheduleLessonViewModel?> GetLessonForRescheduleAsync(
        string teacherId,
        int lessonId,
        CancellationToken cancellationToken = default);

    Task<ScheduleLessonOperationResult> RescheduleLessonAsync(
        string teacherId,
        RescheduleLessonViewModel model,
        CancellationToken cancellationToken = default);

    Task<ScheduleLessonOperationResult> DeleteLessonAsync(
        string teacherId,
        int lessonId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen öğretmenin verilen [startUtc, endUtc) zaman aralığında (iptal edilenler ve varsa excludeLessonId hariç)
    /// çakışan aktif bir dersi olup olmadığını kontrol eder (FR-043).
    /// </summary>
    Task<bool> HasTimeConflictAsync(
        string teacherId,
        DateTime startUtc,
        DateTime endUtc,
        int? excludeLessonId = null,
        CancellationToken cancellationToken = default);
}
