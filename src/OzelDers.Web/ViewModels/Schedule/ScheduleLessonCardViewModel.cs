using OzelDers.Web.Domain.Enums;

namespace OzelDers.Web.ViewModels.Schedule;

/// <summary>
/// Haftalık takvim ızgarasında tek bir ders oturumunu temsil eden kart görünüm modeli (FR-031, FR-041, FR-044, FR-045).
/// Her ders kartı yalnızca tek bir öğrenciye ve tek bir ders türüne (StudentSubjectId) aittir.
/// </summary>
public class ScheduleLessonCardViewModel
{
    public int LessonId { get; init; }

    public int StudentSubjectId { get; init; }

    public int StudentId { get; init; }

    public required string StudentFullName { get; init; }

    public int SubjectId { get; init; }

    public required string SubjectName { get; init; }

    public DateTime StartUtc { get; init; }

    public DateTime EndUtc { get; init; }

    public DateTime StartLocal { get; init; }

    public DateTime EndLocal { get; init; }

    public int DurationMinutes { get; init; }

    public decimal HourlyRateSnapshot { get; init; }

    public decimal SessionFeeSnapshot { get; init; }

    public LessonStatus Status { get; init; }

    public string? Notes { get; init; }

    /// <summary>
    /// Dersin saati geçmiş ancak durumu hâlâ Scheduled veya Rescheduled ise öğretmen yoklama doğrulaması bekliyor demektir (FR-060, FR-061).
    /// </summary>
    public bool IsPendingAttendanceVerification { get; init; }

    public bool IsCancelled => Status == LessonStatus.Cancelled;

    public bool IsRescheduled => Status == LessonStatus.Rescheduled;

    public bool IsCompleted => Status == LessonStatus.Completed;

    public string TimeRangeText => $"{StartLocal:HH:mm} – {EndLocal:HH:mm}";

    public string StatusBadgeText =>
        Status switch
        {
            LessonStatus.Completed => "Tamamlandı",
            LessonStatus.Cancelled => "İptal Edildi",
            LessonStatus.Rescheduled when IsPendingAttendanceVerification => "Ertelendi • Yoklama Bekliyor",
            LessonStatus.Rescheduled => "Ertelendi",
            _ when IsPendingAttendanceVerification => "Yoklama Bekliyor",
            _ => "Planlandı"
        };

    public string StatusBadgeCssClass =>
        Status switch
        {
            LessonStatus.Completed => "bg-success-subtle text-success-emphasis",
            LessonStatus.Cancelled => "bg-secondary-subtle text-secondary-emphasis",
            LessonStatus.Rescheduled when IsPendingAttendanceVerification => "bg-warning-subtle text-warning-emphasis",
            LessonStatus.Rescheduled => "bg-info-subtle text-info-emphasis",
            _ when IsPendingAttendanceVerification => "bg-warning-subtle text-warning-emphasis",
            _ => "bg-primary-subtle text-primary-emphasis"
        };

    public string CardContainerCssClass =>
        Status switch
        {
            LessonStatus.Cancelled => "bg-light border-secondary-subtle opacity-75",
            LessonStatus.Rescheduled => "bg-white border-info",
            LessonStatus.Completed => "bg-white border-success-subtle",
            _ => "bg-white"
        };
}
