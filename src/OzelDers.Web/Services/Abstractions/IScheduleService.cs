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
/// önceki/sonraki hafta navigasyon verilerini ve manuel ders ekleme işlemlerini yürüten servis sözleşmesi
/// (FR-008, FR-031..FR-038, FR-043, FR-045).
/// </summary>
public interface IScheduleService
{
    /// <summary>
    /// Verilen referans tarihin içinde bulunduğu Pazartesi–Pazar haftasının başlangıç tarihini döndürür.
    /// </summary>
    DateOnly GetMondayOfWeek(DateOnly referenceDate);

    /// <summary>
    /// Öğretmenin seçili haftadaki (Pazartesi–Pazar) ders oturumlarını ve hafta navigasyon modelini getirir.
    /// </summary>
    Task<WeeklyScheduleViewModel> GetWeeklyScheduleAsync(
        string teacherId,
        DateOnly? referenceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manuel ders ekleme ekranı için öğretmenin aktif öğrenci–ders türü seçeneklerini ve varsayılan değerlerini hazırlar.
    /// </summary>
    Task<CreateLessonViewModel> PrepareCreateLessonViewModelAsync(
        string teacherId,
        DateOnly? date = null,
        int? studentId = null,
        CreateLessonViewModel? existingModel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tek bir öğrenci–ders türü (StudentSubjectId) için tarih, saat, süre, aktiflik ve çakışma doğrulamalarını yaparak yeni ders oturumu oluşturur.
    /// </summary>
    Task<ScheduleLessonOperationResult> CreateLessonAsync(
        string teacherId,
        CreateLessonViewModel model,
        CancellationToken cancellationToken = default);
}
