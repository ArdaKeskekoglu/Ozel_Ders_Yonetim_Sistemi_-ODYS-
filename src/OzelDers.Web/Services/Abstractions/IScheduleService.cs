using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.Web.Services.Abstractions;

/// <summary>
/// Öğretmenin haftalık ders çizelgesini, hafta başlangıç/bitiş sınırlarını (Pazartesi–Pazar)
/// ve önceki/sonraki hafta navigasyon verilerini hazırlayan servis sözleşmesi (FR-008, FR-031..FR-034, FR-045).
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
}
