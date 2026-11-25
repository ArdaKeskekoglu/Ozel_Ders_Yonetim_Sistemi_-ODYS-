using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.Web.Services;

/// <summary>
/// Öğretmenin haftalık ders çizelgesini (Pazartesi–Pazar), önceki/sonraki hafta navigasyonunu,
/// ders kartlarını, manuel ders oturumu oluşturma ve ders düzenleme/taşıma işlemlerini yürüten servis
/// (FR-008, FR-030..FR-040, FR-043..FR-045, NFR-007).
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

    public async Task<CreateLessonViewModel> PrepareCreateLessonViewModelAsync(
        string teacherId,
        DateOnly? date = null,
        int? studentId = null,
        CreateLessonViewModel? existingModel = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Manuel ders eklemek için öğretmen oturumu gereklidir.");
        }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
        var todayLocal = DateOnly.FromDateTime(localNow);

        var options = await GetActiveEnrollmentOptionsForTeacherAsync(teacherId, cancellationToken);

        if (existingModel is not null)
        {
            existingModel.AvailableEnrollments = options;
            return existingModel;
        }

        var selectedOption = studentId.HasValue
            ? options.FirstOrDefault(o => o.StudentId == studentId.Value) ?? options.FirstOrDefault()
            : options.FirstOrDefault();

        return new CreateLessonViewModel
        {
            StudentSubjectId = selectedOption?.StudentSubjectId ?? 0,
            LessonDate = date ?? todayLocal,
            StartTime = new TimeOnly(15, 0),
            DurationMinutes = selectedOption?.DefaultDurationMinutes ?? 60,
            PreselectedStudentId = studentId,
            AvailableEnrollments = options
        };
    }

    public async Task<ScheduleLessonOperationResult> CreateLessonAsync(
        string teacherId,
        CreateLessonViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        if (model.DurationMinutes < 15 || model.DurationMinutes > 360)
        {
            return Fail("Ders oturumu süresi 15 ile 360 dakika arasında olmalıdır.");
        }

        if (!string.IsNullOrWhiteSpace(model.Notes) && model.Notes.Trim().Length > 1000)
        {
            return Fail("Ders notu en fazla 1000 karakter olabilir.");
        }

        var enrollment = await _dbContext.StudentSubjects
            .Include(ss => ss.Student)
            .Include(ss => ss.Subject)
            .FirstOrDefaultAsync(
                ss => ss.Id == model.StudentSubjectId &&
                      ss.Student != null &&
                      ss.Student.TeacherId == teacherId &&
                      ss.Subject != null &&
                      ss.Subject.TeacherId == teacherId,
                cancellationToken);

        if (enrollment is null || enrollment.Student is null || enrollment.Subject is null)
        {
            return Fail("Seçilen öğrenci ve ders türü eşleşmesi bulunamadı veya hesabınıza ait değil.");
        }

        if (enrollment.Student.IsArchived)
        {
            return Fail($"'{enrollment.Student.FullName}' adlı öğrenci arşivlenmiş durumda olduğundan yeni ders oturumu eklenemez.");
        }

        if (enrollment.Subject.IsArchived)
        {
            return Fail($"'{enrollment.Subject.Name}' ders türü arşivlenmiş durumda olduğundan yeni ders oturumu eklenemez.");
        }

        if (!enrollment.IsActive)
        {
            return Fail($"{enrollment.Student.FullName} — '{enrollment.Subject.Name}' ders ataması pasif durumda olduğundan yeni ders oturumu eklenemez.");
        }

        var startLocal = model.LessonDate.ToDateTime(model.StartTime, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _timeZone);
        var endUtc = startUtc.AddMinutes(model.DurationMinutes);

        var conflictError = await CheckTeacherTimeConflictAsync(
            teacherId,
            startUtc,
            endUtc,
            excludeLessonId: null,
            cancellationToken);

        if (conflictError is not null)
        {
            return Fail(conflictError);
        }

        var lesson = Lesson.CreateFromStudentSubject(
            enrollment,
            startUtc,
            model.DurationMinutes,
            model.Notes);

        _dbContext.Lessons.Add(lesson);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var endLocal = startLocal.AddMinutes(model.DurationMinutes);
        var infoMessage =
            $"{enrollment.Student.FullName} — '{enrollment.Subject.Name}' dersi ({model.LessonDate:dd.MM.yyyy} {startLocal:HH:mm}–{endLocal:HH:mm}, {lesson.SessionFeeSnapshot:N2} ₺) takvime eklendi.";

        return new ScheduleLessonOperationResult(
            true,
            lesson,
            infoMessage,
            Array.Empty<string>());
    }

    public async Task<EditLessonViewModel?> GetLessonForEditAsync(
        string teacherId,
        int lessonId,
        EditLessonViewModel? existingModel = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Ders düzenlemek için öğretmen oturumu gereklidir.");
        }

        var lesson = await _dbContext.Lessons
            .AsNoTracking()
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Student)
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Subject)
            .FirstOrDefaultAsync(
                l => l.Id == lessonId &&
                     l.StudentSubject != null &&
                     l.StudentSubject.Student != null &&
                     l.StudentSubject.Student.TeacherId == teacherId &&
                     l.StudentSubject.Subject != null &&
                     l.StudentSubject.Subject.TeacherId == teacherId,
                cancellationToken);

        if (lesson is null || lesson.StudentSubject?.Student is null || lesson.StudentSubject.Subject is null)
        {
            return null;
        }

        var options = await GetActiveEnrollmentOptionsForTeacherAsync(teacherId, cancellationToken);

        // Eğer dersin mevcut StudentSubject kaydı sonradan pasife alınmışsa, açılır listede kaybolmaması için ekle
        if (options.All(o => o.StudentSubjectId != lesson.StudentSubjectId))
        {
            options.Insert(
                0,
                new StudentSubjectOptionViewModel
                {
                    StudentSubjectId = lesson.StudentSubjectId,
                    StudentId = lesson.StudentSubject.StudentId,
                    StudentFullName = lesson.StudentSubject.Student.FullName,
                    SubjectId = lesson.StudentSubject.SubjectId,
                    SubjectName = lesson.StudentSubject.Subject.Name,
                    IsTeacherDefaultSubject = lesson.StudentSubject.Subject.IsDefault,
                    HourlyRate = lesson.HourlyRateSnapshot,
                    DefaultDurationMinutes = lesson.DurationMinutes,
                    WeeklyTargetCount = lesson.StudentSubject.WeeklyTargetCount
                });
        }

        if (existingModel is not null)
        {
            existingModel.Id = lesson.Id;
            existingModel.HourlyRateSnapshot = lesson.HourlyRateSnapshot;
            existingModel.SessionFeeSnapshot = lesson.SessionFeeSnapshot;
            existingModel.Status = lesson.Status;
            existingModel.AvailableEnrollments = options;
            return existingModel;
        }

        var startUtc = DateTime.SpecifyKind(lesson.StartUtc, DateTimeKind.Utc);
        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(startUtc, _timeZone);

        return new EditLessonViewModel
        {
            Id = lesson.Id,
            StudentSubjectId = lesson.StudentSubjectId,
            LessonDate = DateOnly.FromDateTime(startLocal),
            StartTime = TimeOnly.FromDateTime(startLocal),
            DurationMinutes = lesson.DurationMinutes,
            Notes = lesson.Notes,
            HourlyRateSnapshot = lesson.HourlyRateSnapshot,
            SessionFeeSnapshot = lesson.SessionFeeSnapshot,
            Status = lesson.Status,
            AvailableEnrollments = options
        };
    }

    public async Task<ScheduleLessonOperationResult> UpdateLessonAsync(
        string teacherId,
        EditLessonViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        if (model.DurationMinutes < 15 || model.DurationMinutes > 360)
        {
            return Fail("Ders oturumu süresi 15 ile 360 dakika arasında olmalıdır.");
        }

        if (!string.IsNullOrWhiteSpace(model.Notes) && model.Notes.Trim().Length > 1000)
        {
            return Fail("Ders notu en fazla 1000 karakter olabilir.");
        }

        var lesson = await _dbContext.Lessons
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Student)
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Subject)
            .FirstOrDefaultAsync(
                l => l.Id == model.Id &&
                     l.StudentSubject != null &&
                     l.StudentSubject.Student != null &&
                     l.StudentSubject.Student.TeacherId == teacherId &&
                     l.StudentSubject.Subject != null &&
                     l.StudentSubject.Subject.TeacherId == teacherId,
                cancellationToken);

        if (lesson is null || lesson.StudentSubject?.Student is null || lesson.StudentSubject.Subject is null)
        {
            return Fail("Düzenlenmek istenen ders oturumu bulunamadı veya bu ders üzerinde yetkiniz yok.");
        }

        var targetEnrollment = lesson.StudentSubject;
        var isSubjectChanged = model.StudentSubjectId != lesson.StudentSubjectId;

        if (isSubjectChanged)
        {
            var newEnrollment = await _dbContext.StudentSubjects
                .Include(ss => ss.Student)
                .Include(ss => ss.Subject)
                .FirstOrDefaultAsync(
                    ss => ss.Id == model.StudentSubjectId &&
                          ss.Student != null &&
                          ss.Student.TeacherId == teacherId &&
                          ss.Subject != null &&
                          ss.Subject.TeacherId == teacherId,
                    cancellationToken);

            if (newEnrollment is null || newEnrollment.Student is null || newEnrollment.Subject is null)
            {
                return Fail("Seçilen yeni öğrenci ve ders türü eşleşmesi bulunamadı veya hesabınıza ait değil.");
            }

            if (newEnrollment.Student.IsArchived)
            {
                return Fail($"'{newEnrollment.Student.FullName}' adlı öğrenci arşivlenmiş durumda olduğundan derse atanamaz.");
            }

            if (newEnrollment.Subject.IsArchived)
            {
                return Fail($"'{newEnrollment.Subject.Name}' ders türü arşivlenmiş durumda olduğundan derse atanamaz.");
            }

            if (!newEnrollment.IsActive)
            {
                return Fail($"{newEnrollment.Student.FullName} — '{newEnrollment.Subject.Name}' ders ataması pasif durumda olduğundan seçilemez.");
            }

            targetEnrollment = newEnrollment;
        }

        var startLocal = model.LessonDate.ToDateTime(model.StartTime, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _timeZone);
        var endUtc = startUtc.AddMinutes(model.DurationMinutes);

        // İptal edilmemiş bir ders taşınıyorsa veya süresi değişiyorsa, kendisi (lesson.Id) hariç çakışma kontrolü yapılır (FR-043)
        if (lesson.Status != LessonStatus.Cancelled)
        {
            var conflictError = await CheckTeacherTimeConflictAsync(
                teacherId,
                startUtc,
                endUtc,
                excludeLessonId: lesson.Id,
                cancellationToken);

            if (conflictError is not null)
            {
                return Fail(conflictError);
            }
        }

        if (isSubjectChanged)
        {
            lesson.StudentSubjectId = targetEnrollment.Id;
            lesson.HourlyRateSnapshot = decimal.Round(targetEnrollment.HourlyRate, 2);
        }

        lesson.StartUtc = startUtc;
        lesson.EndUtc = endUtc;
        lesson.DurationMinutes = model.DurationMinutes;
        lesson.SessionFeeSnapshot = Lesson.CalculateSessionFee(lesson.HourlyRateSnapshot, model.DurationMinutes);
        lesson.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var endLocal = startLocal.AddMinutes(model.DurationMinutes);
        var studentName = targetEnrollment.Student?.FullName ?? "Öğrenci";
        var subjectName = targetEnrollment.Subject?.Name ?? "Ders";
        var infoMessage =
            $"{studentName} — '{subjectName}' dersi ({model.LessonDate:dd.MM.yyyy} {startLocal:HH:mm}–{endLocal:HH:mm}, {lesson.SessionFeeSnapshot:N2} ₺) olarak güncellendi.";

        return new ScheduleLessonOperationResult(
            true,
            lesson,
            infoMessage,
            Array.Empty<string>());
    }

    private async Task<List<StudentSubjectOptionViewModel>> GetActiveEnrollmentOptionsForTeacherAsync(
        string teacherId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StudentSubjects
            .AsNoTracking()
            .Include(ss => ss.Student)
            .Include(ss => ss.Subject)
            .Where(ss =>
                ss.IsActive &&
                ss.Student != null &&
                ss.Student.TeacherId == teacherId &&
                !ss.Student.IsArchived &&
                ss.Subject != null &&
                ss.Subject.TeacherId == teacherId &&
                !ss.Subject.IsArchived)
            .OrderBy(ss => ss.Student!.FirstName)
            .ThenBy(ss => ss.Student!.LastName)
            .ThenByDescending(ss => ss.Subject!.IsDefault)
            .ThenBy(ss => ss.Subject!.Name)
            .Select(ss => new StudentSubjectOptionViewModel
            {
                StudentSubjectId = ss.Id,
                StudentId = ss.StudentId,
                StudentFullName = ss.Student!.FirstName + " " + ss.Student.LastName,
                SubjectId = ss.SubjectId,
                SubjectName = ss.Subject!.Name,
                IsTeacherDefaultSubject = ss.Subject.IsDefault,
                HourlyRate = ss.HourlyRate,
                DefaultDurationMinutes = ss.DefaultDurationMinutes,
                WeeklyTargetCount = ss.WeeklyTargetCount
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> CheckTeacherTimeConflictAsync(
        string teacherId,
        DateTime startUtc,
        DateTime endUtc,
        int? excludeLessonId,
        CancellationToken cancellationToken)
    {
        var conflictingLesson = await _dbContext.Lessons
            .AsNoTracking()
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Student)
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Subject)
            .FirstOrDefaultAsync(
                l => (!excludeLessonId.HasValue || l.Id != excludeLessonId.Value) &&
                     l.Status != LessonStatus.Cancelled &&
                     l.StartUtc < endUtc &&
                     startUtc < l.EndUtc &&
                     l.StudentSubject != null &&
                     l.StudentSubject.Student != null &&
                     l.StudentSubject.Student.TeacherId == teacherId,
                cancellationToken);

        if (conflictingLesson is null)
        {
            return null;
        }

        var conflictStartLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(conflictingLesson.StartUtc, DateTimeKind.Utc),
            _timeZone);
        var conflictEndLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(conflictingLesson.EndUtc, DateTimeKind.Utc),
            _timeZone);
        var conflictStudent = conflictingLesson.StudentSubject?.Student?.FullName ?? "Öğrenci";
        var conflictSubject = conflictingLesson.StudentSubject?.Subject?.Name ?? "Ders";

        return $"Seçilen saat aralığında ({conflictStartLocal:HH:mm}–{conflictEndLocal:HH:mm}) {conflictStudent} — {conflictSubject} dersi ile zaman çakışması bulunmaktadır.";
    }

    private static ScheduleLessonOperationResult Fail(string error) =>
        new(false, null, null, new[] { error });

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
