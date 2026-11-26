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

    public LessonAttendanceStatus AttendanceStatus { get; init; }

    public DateTime? AttendanceVerifiedAtUtc { get; init; }

    public string? Notes { get; init; }

    /// <summary>
    /// Dersin saati geçmiş ancak öğretmen yoklama doğrulamasını henüz yapmamışsa (AttendanceStatus == Pending)
    /// ve ders iptal edilmemişse yoklama doğrulaması bekliyor demektir (FR-060, FR-061).
    /// </summary>
    public bool IsPendingAttendanceVerification { get; init; }

    public bool IsCancelled => Status == LessonStatus.Cancelled;

    public bool IsRescheduled => Status == LessonStatus.Rescheduled;

    public bool IsCompleted => Status == LessonStatus.Completed;

    public bool IsAbsent => AttendanceStatus == LessonAttendanceStatus.Absent;

    public bool IsAttendanceVerified => AttendanceStatus != LessonAttendanceStatus.Pending;

    public string TimeRangeText => $"{StartLocal:HH:mm} – {EndLocal:HH:mm}";

    public string StatusBadgeText =>
        AttendanceStatus switch
        {
            LessonAttendanceStatus.Attended => "Geldi • Tamamlandı",
            LessonAttendanceStatus.Absent => "Gelmedi",
            _ => Status switch
            {
                LessonStatus.Completed => "Tamamlandı",
                LessonStatus.Cancelled => "İptal Edildi",
                LessonStatus.Rescheduled when IsPendingAttendanceVerification => "Ertelendi • Yoklama Bekliyor",
                LessonStatus.Rescheduled => "Ertelendi",
                _ when IsPendingAttendanceVerification => "Yoklama Bekliyor",
                _ => "Planlandı"
            }
        };

    public string StatusBadgeCssClass =>
        AttendanceStatus switch
        {
            LessonAttendanceStatus.Attended => "bg-success-subtle text-success-emphasis",
            LessonAttendanceStatus.Absent => "bg-danger-subtle text-danger-emphasis",
            _ => Status switch
            {
                LessonStatus.Completed => "bg-success-subtle text-success-emphasis",
                LessonStatus.Cancelled => "bg-secondary-subtle text-secondary-emphasis",
                LessonStatus.Rescheduled when IsPendingAttendanceVerification => "bg-warning-subtle text-warning-emphasis",
                LessonStatus.Rescheduled => "bg-info-subtle text-info-emphasis",
                _ when IsPendingAttendanceVerification => "bg-warning-subtle text-warning-emphasis",
                _ => "bg-primary-subtle text-primary-emphasis"
            }
        };

    public string CardContainerCssClass =>
        AttendanceStatus switch
        {
            LessonAttendanceStatus.Attended => "bg-white border-success-subtle",
            LessonAttendanceStatus.Absent => "bg-white border-danger-subtle",
            _ => Status switch
            {
                LessonStatus.Cancelled => "bg-light border-secondary-subtle opacity-75",
                LessonStatus.Rescheduled when IsPendingAttendanceVerification => "bg-white border-warning",
                LessonStatus.Rescheduled => "bg-white border-info",
                LessonStatus.Completed => "bg-white border-success-subtle",
                _ when IsPendingAttendanceVerification => "bg-white border-warning",
                _ => "bg-white"
            }
        };
}
