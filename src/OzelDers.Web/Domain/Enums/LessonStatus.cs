namespace OzelDers.Web.Domain.Enums;

/// <summary>
/// Ders oturumu yaşam döngüsü durumları (FR-045, FR-057..FR-061).
/// Not: Ders saati geçmiş olsa bile öğretmen yoklama doğrulamasını yapana kadar
/// ders otomatik olarak Completed sayılmaz; Scheduled durumunda doğrulama bekler (FR-060, FR-061).
/// </summary>
public enum LessonStatus
{
    /// <summary>
    /// Planlandı (Gelecek ders veya saati geçmiş olup öğretmen yoklama doğrulaması bekleyen ders).
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Tamamlandı (Öğretmen tarafından dersin gerçekleştiği doğrulandı).
    /// </summary>
    Completed = 1,

    /// <summary>
    /// İptal Edildi (Takvimde çakışma kontrolünden düşer, geçmiş kaydı korunur).
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Ertelendi / Başka bir gün veya saate taşındı.
    /// </summary>
    Rescheduled = 3
}
