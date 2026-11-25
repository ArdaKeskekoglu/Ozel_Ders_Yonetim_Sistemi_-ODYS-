using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Enums;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.Web.Services;

/// <summary>
/// Öğretmenin haftalık ders çizelgesini (Pazartesi–Pazar), önceki/sonraki hafta navigasyonunu
/// ve ders kartlarını hazırlayan servis (FR-008, FR-031..FR-034, FR-044, FR-045, NFR-007).
/// </summary>
public class ScheduleService : IScheduleService
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo DefaultTurkeyTimeZone = ResolveTurkeyTimeZone();

    private readonly ApplicationDbContext _dbContext;
    private readonly TimeZoneInfo _timeZone;

    public ScheduleService(
        ApplicationDbContext dbContext,
        TimeZoneInfo? timeZone = null)
    {
        _dbContext = dbContext;
        _timeZone = timeZone ?? DefaultTurkeyTimeZone;
    }

    public DateOnly GetMondayOfWeek(DateOnly referenceDate)
    {
        // Pazartesi = 0, Salı = 1, ..., Pazar = 6 ofseti (ISO 8601)
        var offset = ((int)referenceDate.DayOfWeek + 6) % 7;
        return referenceDate.AddDays(-offset);
    }

    public async Task<WeeklyScheduleViewModel> GetWeeklyScheduleAsync(
        string teacherId,
        DateOnly? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Haftalık ders programını görüntülemek için öğretmen oturumu gereklidir.");
        }

        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _timeZone);
        var todayLocal = DateOnly.FromDateTime(localNow);

        var targetDate = referenceDate ?? todayLocal;
        var weekStartDate = GetMondayOfWeek(targetDate);
        var weekEndDate = weekStartDate.AddDays(6);
        var currentWeekStartDate = GetMondayOfWeek(todayLocal);

        var weekStartLocal = weekStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var nextWeekStartLocal = weekStartDate.AddDays(7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(weekStartLocal, _timeZone);
        var nextWeekStartUtc = TimeZoneInfo.ConvertTimeToUtc(nextWeekStartLocal, _timeZone);

        var rawLessons = await _dbContext.Lessons
            .AsNoTracking()
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Student)
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Subject)
            .Where(l =>
                l.StartUtc >= weekStartUtc &&
                l.StartUtc < nextWeekStartUtc &&
                l.StudentSubject != null &&
                l.StudentSubject.Student != null &&
                l.StudentSubject.Student.TeacherId == teacherId &&
                l.StudentSubject.Subject != null &&
                l.StudentSubject.Subject.TeacherId == teacherId)
            .OrderBy(l => l.StartUtc)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        var lessonCards = rawLessons
            .Select(l =>
            {
                var startUtc = DateTime.SpecifyKind(l.StartUtc, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(l.EndUtc, DateTimeKind.Utc);
                var startLocal = TimeZoneInfo.ConvertTimeFromUtc(startUtc, _timeZone);
                var endLocal = TimeZoneInfo.ConvertTimeFromUtc(endUtc, _timeZone);

                return new ScheduleLessonCardViewModel
                {
                    LessonId = l.Id,
                    StudentSubjectId = l.StudentSubjectId,
                    StudentId = l.StudentSubject!.StudentId,
                    StudentFullName = l.StudentSubject.Student!.FullName,
                    SubjectId = l.StudentSubject.SubjectId,
                    SubjectName = l.StudentSubject.Subject!.Name,
                    StartUtc = startUtc,
                    EndUtc = endUtc,
                    StartLocal = startLocal,
                    EndLocal = endLocal,
                    DurationMinutes = l.DurationMinutes,
                    HourlyRateSnapshot = l.HourlyRateSnapshot,
                    SessionFeeSnapshot = l.SessionFeeSnapshot,
                    Status = l.Status,
                    Notes = l.Notes,
                    IsPendingAttendanceVerification = l.Status == LessonStatus.Scheduled && endUtc <= utcNow
                };
            })
            .ToList();

        var lessonsByDate = lessonCards
            .GroupBy(c => DateOnly.FromDateTime(c.StartLocal))
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ScheduleLessonCardViewModel>)g.ToList());

        var days = new List<ScheduleDayColumnViewModel>(7);
        for (var i = 0; i < 7; i++)
        {
            var dayDate = weekStartDate.AddDays(i);
            var dayDateTime = dayDate.ToDateTime(TimeOnly.MinValue);

            days.Add(new ScheduleDayColumnViewModel
            {
                Date = dayDate,
                DayOfWeek = dayDate.DayOfWeek,
                DayNameTr = GetTurkishDayName(dayDate.DayOfWeek),
                ShortDateText = dayDateTime.ToString("dd MMM", TrCulture),
                IsToday = dayDate == todayLocal,
                Lessons = lessonsByDate.TryGetValue(dayDate, out var dayLessons)
                    ? dayLessons
                    : Array.Empty<ScheduleLessonCardViewModel>()
            });
        }

        var activeLessons = lessonCards
            .Where(l => l.Status != LessonStatus.Cancelled)
            .ToList();

        return new WeeklyScheduleViewModel
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            PreviousWeekStartDate = weekStartDate.AddDays(-7),
            NextWeekStartDate = weekStartDate.AddDays(7),
            CurrentWeekStartDate = currentWeekStartDate,
            WeekRangeTitle = FormatWeekRangeTitle(weekStartDate, weekEndDate),
            TotalLessonCount = lessonCards.Count,
            ScheduledLessonCount = lessonCards.Count(l => l.Status == LessonStatus.Scheduled),
            CompletedLessonCount = lessonCards.Count(l => l.Status == LessonStatus.Completed),
            CancelledLessonCount = lessonCards.Count(l => l.Status == LessonStatus.Cancelled),
            PendingVerificationCount = lessonCards.Count(l => l.IsPendingAttendanceVerification),
            TotalActiveMinutes = activeLessons.Sum(l => l.DurationMinutes),
            TotalActiveFeeSnapshot = activeLessons.Sum(l => l.SessionFeeSnapshot),
            Days = days
        };
    }

    private static string GetTurkishDayName(DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            DayOfWeek.Monday => "Pazartesi",
            DayOfWeek.Tuesday => "Salı",
            DayOfWeek.Wednesday => "Çarşamba",
            DayOfWeek.Thursday => "Perşembe",
            DayOfWeek.Friday => "Cuma",
            DayOfWeek.Saturday => "Cumartesi",
            DayOfWeek.Sunday => "Pazar",
            _ => string.Empty
        };

    private static string FormatWeekRangeTitle(DateOnly start, DateOnly end)
    {
        var startDt = start.ToDateTime(TimeOnly.MinValue);
        var endDt = end.ToDateTime(TimeOnly.MinValue);

        if (start.Year == end.Year && start.Month == end.Month)
        {
            return $"{start.Day} – {endDt.ToString("dd MMMM yyyy", TrCulture)}";
        }

        if (start.Year == end.Year)
        {
            return $"{startDt.ToString("dd MMMM", TrCulture)} – {endDt.ToString("dd MMMM yyyy", TrCulture)}";
        }

        return $"{startDt.ToString("dd MMMM yyyy", TrCulture)} – {endDt.ToString("dd MMMM yyyy", TrCulture)}";
    }

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "TRT",
                    TimeSpan.FromHours(3),
                    "Türkiye Saati (UTC+3)",
                    "Türkiye Standart Saati");
            }
        }
    }
}
