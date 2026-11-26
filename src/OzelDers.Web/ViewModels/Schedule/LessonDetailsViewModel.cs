using OzelDers.Web.Domain.Enums;

namespace OzelDers.Web.ViewModels.Schedule;

/// <summary>
/// Tek bir ders oturumunun tüm detaylarını, ücret snapshot değerlerini, tek hafta istisnası
/// aksiyonlarını ve aynı öğrenci–ders türünün diğer haftalardaki ilişkili oturumlarını taşıyan görünüm modeli (FR-044, FR-046, FR-047).
/// </summary>
public class LessonDetailsViewModel
{
    public int LessonId { get; init; }

    public int StudentSubjectId { get; init; }

    public int StudentId { get; init; }

    public required string StudentFullName { get; init; }

    public string? StudentPhoneNumber { get; init; }

    public string? StudentEmail { get; init; }

    public int SubjectId { get; init; }

    public required string SubjectName { get; init; }

    public bool IsTeacherDefaultSubject { get; init; }

    public DateOnly LessonDate { get; init; }

    public required string DayNameTr { get; init; }

    public DateTime StartUtc { get; init; }

    public DateTime EndUtc { get; init; }

    public DateTime StartLocal { get; init; }

    public DateTime EndLocal { get; init; }

    public int DurationMinutes { get; init; }

    public decimal HourlyRateSnapshot { get; init; }

    public decimal SessionFeeSnapshot { get; init; }

    public decimal CurrentStudentSubjectHourlyRate { get; init; }

    public LessonStatus Status { get; init; }

    public LessonAttendanceStatus AttendanceStatus { get; init; }

    public DateTime? AttendanceVerifiedAtUtc { get; init; }

    public LessonFeeDecision FeeDecision { get; init; }

    public DateTime? FeeDecisionAtUtc { get; init; }

    public string? FeeDecisionReason { get; init; }

    public bool IsPendingAttendanceVerification { get; init; }

    public bool IsPendingFeeDecision =>
        AttendanceStatus == LessonAttendanceStatus.Absent &&
        FeeDecision == LessonFeeDecision.Pending;

    public string? Notes { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public IReadOnlyList<ScheduleLessonCardViewModel> RelatedSeriesLessons { get; init; } =
        Array.Empty<ScheduleLessonCardViewModel>();

    public bool IsCancelled => Status == LessonStatus.Cancelled;

    public bool IsCompleted => Status == LessonStatus.Completed;

    public bool IsRescheduled => Status == LessonStatus.Rescheduled;

    public bool IsAbsent => AttendanceStatus == LessonAttendanceStatus.Absent;

    public bool IsAttendanceVerified => AttendanceStatus != LessonAttendanceStatus.Pending;

    public string TimeRangeText => $"{StartLocal:HH:mm} – {EndLocal:HH:mm}";

    public string AttendanceBadgeText =>
        AttendanceStatus switch
        {
            LessonAttendanceStatus.Attended => "Öğrenci Geldi (Ders Gerçekleşti)",
            LessonAttendanceStatus.Absent => "Öğrenci Gelmedi (Devamsızlık)",
            LessonAttendanceStatus.NotApplicable => "Ders İptal Edildi",
            _ when IsPendingAttendanceVerification => "Yoklama Doğrulaması Bekliyor",
            _ => "Henüz Ders Saati Gelmedi / Planlı"
        };

    public string FeeDecisionBadgeText =>
        FeeDecision switch
        {
            LessonFeeDecision.Charge => $"Ücret Alınacak ({SessionFeeSnapshot:N2} ₺)",
            LessonFeeDecision.Waive => "Ücret Alınmayacak / Muaf (0,00 ₺)",
            _ when AttendanceStatus == LessonAttendanceStatus.Absent => "Devamsızlık Ücret Kararı Bekliyor",
            _ => "Yoklama Sonrası Belirlenecek"
        };

    public string FeeDecisionBadgeCssClass =>
        FeeDecision switch
        {
            LessonFeeDecision.Charge => "bg-primary-subtle text-primary-emphasis",
            LessonFeeDecision.Waive => "bg-secondary-subtle text-secondary-emphasis",
            _ when AttendanceStatus == LessonAttendanceStatus.Absent => "bg-warning-subtle text-warning-emphasis",
            _ => "bg-light text-muted border"
        };

    public string StatusBadgeText =>
        AttendanceStatus switch
        {
            LessonAttendanceStatus.Attended => "Geldi • Tamamlandı",
            LessonAttendanceStatus.Absent when FeeDecision == LessonFeeDecision.Charge => "Gelmedi • Ücret Alınacak",
            LessonAttendanceStatus.Absent when FeeDecision == LessonFeeDecision.Waive => "Gelmedi • Ücret Alınmayacak",
            LessonAttendanceStatus.Absent => "Gelmedi • Ücret Kararı Bekliyor",
            _ => Status switch
            {
                LessonStatus.Completed => "Tamamlandı",
                LessonStatus.Cancelled when FeeDecision == LessonFeeDecision.Charge => "İptal • Ücret Alınacak",
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
            LessonAttendanceStatus.Absent when FeeDecision == LessonFeeDecision.Pending => "bg-warning-subtle text-warning-emphasis",
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
}
