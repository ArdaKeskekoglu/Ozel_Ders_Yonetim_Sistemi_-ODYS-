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
/// önceki/sonraki hafta navigasyon verilerini, manuel ders ekleme ve ders düzenleme/taşıma
/// işlemlerini yürüten servis sözleşmesi (FR-008, FR-031..FR-040, FR-043, FR-045).
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

    /// <summary>
    /// Mevcut bir ders oturumunu düzenlemek veya başka bir gün/saate taşımak için form modelini hazırlar.
    /// </summary>
    Task<EditLessonViewModel?> GetLessonForEditAsync(
        string teacherId,
        int lessonId,
        EditLessonViewModel? existingModel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mevcut ders oturumunun gün, saat, süre, öğrenci–ders türü ve not bilgilerini kendisi hariç çakışma kontrolüyle günceller.
    /// </summary>
    Task<ScheduleLessonOperationResult> UpdateLessonAsync(
        string teacherId,
        EditLessonViewModel model,
        CancellationToken cancellationToken = default);
}
