namespace OzelDers.Web.ViewModels.Schedule;

/// <summary>
/// Haftalık takvimde Pazartesi–Pazar arasındaki tek bir gün sütununu temsil eder.
/// </summary>
public class ScheduleDayColumnViewModel
{
    public DateOnly Date { get; init; }

    public DayOfWeek DayOfWeek { get; init; }

    public required string DayNameTr { get; init; }

    public required string ShortDateText { get; init; }

    public bool IsToday { get; init; }

    public IReadOnlyList<ScheduleLessonCardViewModel> Lessons { get; init; } =
        Array.Empty<ScheduleLessonCardViewModel>();

    public int ActiveLessonCount =>
        Lessons.Count(l => l.Status != OzelDers.Web.Domain.Enums.LessonStatus.Cancelled);

    public int TotalActiveMinutes =>
        Lessons
            .Where(l => l.Status != OzelDers.Web.Domain.Enums.LessonStatus.Cancelled)
            .Sum(l => l.DurationMinutes);
}

/// <summary>
/// Haftalık ders programı ve hafta navigasyonu ekran görünüm modeli (FR-031..FR-034, FR-041, FR-044, FR-045).
/// </summary>
public class WeeklyScheduleViewModel
{
    public DateOnly WeekStartDate { get; init; }

    public DateOnly WeekEndDate { get; init; }

    public DateOnly PreviousWeekStartDate { get; init; }

    public DateOnly NextWeekStartDate { get; init; }

    public DateOnly CurrentWeekStartDate { get; init; }

    public bool IsCurrentWeek => WeekStartDate == CurrentWeekStartDate;

    public required string WeekRangeTitle { get; init; }

    public int TotalLessonCount { get; init; }

    public int ScheduledLessonCount { get; init; }

    public int RescheduledLessonCount { get; init; }

    public int CompletedLessonCount { get; init; }

    public int CancelledLessonCount { get; init; }

    public int PendingVerificationCount { get; init; }

    public int PendingFeeDecisionCount { get; init; }

    public int TotalActiveMinutes { get; init; }

    public decimal TotalActiveFeeSnapshot { get; init; }

    public IReadOnlyList<ScheduleDayColumnViewModel> Days { get; init; } =
        Array.Empty<ScheduleDayColumnViewModel>();

    public bool HasAnyLessonsInWeek => TotalLessonCount > 0;
}
