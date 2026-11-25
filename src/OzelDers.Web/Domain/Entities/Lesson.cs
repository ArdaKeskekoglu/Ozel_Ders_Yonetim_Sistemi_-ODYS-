using OzelDers.Web.Domain.Common;
using OzelDers.Web.Domain.Enums;

namespace OzelDers.Web.Domain.Entities;

/// <summary>
/// Belirli bir tarih ve saatteki tekil ders oturumu varlığı (FR-030, FR-035..FR-047, FR-069, BR-16, DEC-14).
/// Kesin İş Kuralı:
/// - Bir öğrenciye birden fazla ders türü (StudentSubjects) atanabilir;
///   ancak her ders oturumu (Lesson) yalnızca TEK bir StudentSubjectId kaydına bağlanır.
/// - Veri tekrarını ve tutarsızlık riskini önlemek için StudentId, SubjectId ve TeacherId
///   bu tabloda tekrar tutulmaz; StudentSubject ilişkisi üzerinden güvenli biçimde erişilir.
/// - Oturum oluşturulduğunda saatlik ücret (HourlyRateSnapshot) ve toplam oturum ücreti (SessionFeeSnapshot)
///   dondurularak saklanır; sonradan yapılan ücret veya varsayılan ders değişiklikleri geçmiş dersleri etkilemez.
/// </summary>
public class Lesson : BaseEntity
{
    public int StudentSubjectId { get; set; }

    /// <summary>
    /// Ders oturumunun başlangıç zamanı (UTC).
    /// </summary>
    public DateTime StartUtc { get; set; }

    /// <summary>
    /// Ders oturumunun bitiş zamanı (UTC). Zaman aralığı [StartUtc, EndUtc) olarak değerlendirilir.
    /// </summary>
    public DateTime EndUtc { get; set; }

    /// <summary>
    /// Ders oturumunun süresi (dakika). Örn. 60, 90, 120 dk.
    /// </summary>
    public int DurationMinutes { get; set; } = 60;

    /// <summary>
    /// Ders oturumu oluşturulduğu andaki saatlik (60 dk) ders ücreti snapshot değeri (FR-030, FR-069).
    /// </summary>
    public decimal HourlyRateSnapshot { get; set; }

    /// <summary>
    /// Ders oturumu oluşturulduğu andaki hesaplanmış tek oturum ücreti snapshot değeri (HourlyRateSnapshot * DurationMinutes / 60).
    /// </summary>
    public decimal SessionFeeSnapshot { get; set; }

    /// <summary>
    /// Ders oturumunun güncel durumu (Scheduled, Completed, Cancelled, Rescheduled).
    /// </summary>
    public LessonStatus Status { get; set; } = LessonStatus.Scheduled;

    /// <summary>
    /// Ders oturumuna özel öğretmen notu veya konu özeti.
    /// </summary>
    public string? Notes { get; set; }

    public StudentSubject? StudentSubject { get; set; }

    /// <summary>
    /// Saatlik ücret ve oturum süresine göre oturum ücreti snapshot değerini hesaplar.
    /// </summary>
    public static decimal CalculateSessionFee(decimal hourlyRate, int durationMinutes)
    {
        if (hourlyRate < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(hourlyRate), "Saatlik ücret negatif olamaz.");
        }

        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Ders süresi sıfırdan büyük olmalıdır.");
        }

        return Math.Round(hourlyRate * durationMinutes / 60m, 2);
    }

    /// <summary>
    /// Verilen StudentSubject kaydından ücret ve süre snapshot değerlerini alarak yeni bir ders oturumu örneği oluşturur.
    /// </summary>
    public static Lesson CreateFromStudentSubject(
        StudentSubject studentSubject,
        DateTime startUtc,
        int? overrideDurationMinutes = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(studentSubject);

        var duration = overrideDurationMinutes ?? studentSubject.DefaultDurationMinutes;
        if (duration < 15 || duration > 360)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overrideDurationMinutes),
                "Ders oturumu süresi 15 ile 360 dakika arasında olmalıdır.");
        }

        var normalizedStartUtc = startUtc.Kind == DateTimeKind.Utc
            ? startUtc
            : DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);

        var hourlyRate = decimal.Round(studentSubject.HourlyRate, 2);
        var sessionFee = CalculateSessionFee(hourlyRate, duration);

        return new Lesson
        {
            StudentSubjectId = studentSubject.Id,
            StartUtc = normalizedStartUtc,
            EndUtc = normalizedStartUtc.AddMinutes(duration),
            DurationMinutes = duration,
            HourlyRateSnapshot = hourlyRate,
            SessionFeeSnapshot = sessionFee,
            Status = LessonStatus.Scheduled,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}
