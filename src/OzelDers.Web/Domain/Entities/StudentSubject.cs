using OzelDers.Web.Domain.Common;

namespace OzelDers.Web.Domain.Entities;

/// <summary>
/// Öğrenciye atanan ders türü, öğrenciye özel saatlik ücret, tek oturum süresi ve
/// haftalık hedef oturum sayısı ilişki varlığı (FR-025..FR-030, BR-16, DEC-14, NFR-007).
/// Bir öğrenciye birden fazla ders türü atanabilir; ancak Faz 17'de oluşturulacak her ders oturumu (Lesson)
/// yalnızca tek bir StudentSubject kaydına bağlanır.
/// </summary>
public class StudentSubject : BaseEntity
{
    public int StudentId { get; set; }

    public int SubjectId { get; set; }

    /// <summary>
    /// Öğrencinin bu ders türü için saatlik (60 dakika) ders ücreti (TL).
    /// </summary>
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Bu ders türü için planlanan tek bir ders oturumunun varsayılan süresi (dakika).
    /// Örn. 60 dk veya 120 dk (FR-029).
    /// </summary>
    public int DefaultDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Öğrencinin bu ders türünden haftalık alması hedeflenen ayrı ders oturumu adedi (FR-028).
    /// Örn. Haftada 2 oturum × 60 dk ile 1 oturum × 120 dk ayrımını korur.
    /// </summary>
    public int WeeklyTargetCount { get; set; } = 1;

    /// <summary>
    /// Öğrencinin bu ders türünü aktif olarak alıp almadığı durumu.
    /// Geçmiş ders kayıtlarını bozmamak için silmek yerine pasife alınabilir.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Student? Student { get; set; }

    public Subject? Subject { get; set; }

    /// <summary>
    /// Saatlik ücret ve varsayılan oturum süresine göre tek bir oturumun hesaplanan ücreti.
    /// </summary>
    public decimal EstimatedSessionFee => Math.Round(HourlyRate * DefaultDurationMinutes / 60m, 2);

    /// <summary>
    /// Haftalık toplam hedef ders süresi (dakika).
    /// </summary>
    public int WeeklyTotalMinutes => WeeklyTargetCount * DefaultDurationMinutes;
}
