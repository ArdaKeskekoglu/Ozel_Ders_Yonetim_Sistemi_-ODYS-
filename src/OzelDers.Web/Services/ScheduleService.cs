using System.Collections.Concurrent;
using System.Data;
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
/// ders kartlarını, manuel ders oturumu oluşturma, düzenleme, iptal, erteleme, kaldırma
/// ve eşzamanlılık/çakışma korumasını yürüten servis (FR-008, FR-030..FR-045, FR-059, NFR-007).
/// </summary>
public class ScheduleService : IScheduleService
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo DefaultTurkeyTimeZone = ResolveTurkeyTimeZone();

    /// <summary>
    /// Aynı öğretmenin takvim yazma işlemlerinde eşzamanlı yarış durumlarını (TOCTOU race condition)
    /// uygulama katmanında seri hâle getiren öğretmen bazlı kilit tablosu.
    /// İlişkisel veritabanında buna ek olarak Serializable transaction katmanı da çalıştırılır.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TeacherScheduleLocks =
        new(StringComparer.Ordinal);

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
                var isPendingAttendance =
                    (l.Status == LessonStatus.Scheduled || l.Status == LessonStatus.Rescheduled) &&
                    endUtc <= utcNow;

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
                    IsPendingAttendanceVerification = isPendingAttendance
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
            RescheduledLessonCount = lessonCards.Count(l => l.Status == LessonStatus.Rescheduled),
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

        return await ExecuteTeacherAtomicScheduleWriteAsync(
            teacherId,
            async ct =>
            {
                var enrollment = await _dbContext.StudentSubjects
                    .Include(ss => ss.Student)
                    .Include(ss => ss.Subject)
                    .FirstOrDefaultAsync(
                        ss => ss.Id == model.StudentSubjectId &&
                              ss.Student != null &&
                              ss.Student.TeacherId == teacherId &&
                              ss.Subject != null &&
                              ss.Subject.TeacherId == teacherId,
                        ct);

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
                    ct);

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
                await _dbContext.SaveChangesAsync(ct);

                var endLocal = startLocal.AddMinutes(model.DurationMinutes);
                var infoMessage =
                    $"{enrollment.Student.FullName} — '{enrollment.Subject.Name}' dersi ({model.LessonDate:dd.MM.yyyy} {startLocal:HH:mm}–{endLocal:HH:mm}, {lesson.SessionFeeSnapshot:N2} ₺) takvime eklendi.";

                return new ScheduleLessonOperationResult(
                    true,
                    lesson,
                    infoMessage,
                    Array.Empty<string>());
            },
            cancellationToken);
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

        return await ExecuteTeacherAtomicScheduleWriteAsync(
            teacherId,
            async ct =>
            {
                var lesson = await GetOwnedLessonAsync(teacherId, model.Id, ct);
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
                            ct);

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

                if (lesson.Status != LessonStatus.Cancelled)
                {
                    var conflictError = await CheckTeacherTimeConflictAsync(
                        teacherId,
                        startUtc,
                        endUtc,
                        excludeLessonId: lesson.Id,
                        ct);

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

                await _dbContext.SaveChangesAsync(ct);

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
            },
            cancellationToken);
    }

    public async Task<ScheduleLessonOperationResult> CancelLessonAsync(
        string teacherId,
        int lessonId,
        string? cancelReason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        return await ExecuteTeacherAtomicScheduleWriteAsync(
            teacherId,
            async ct =>
            {
                var lesson = await GetOwnedLessonAsync(teacherId, lessonId, ct);
                if (lesson is null || lesson.StudentSubject?.Student is null || lesson.StudentSubject.Subject is null)
                {
                    return Fail("İptal edilmek istenen ders oturumu bulunamadı veya yetkiniz yok.");
                }

                if (lesson.Status == LessonStatus.Completed)
                {
                    return Fail("Tamamlanmış bir ders oturumu doğrudan takvimden iptal edilemez.");
                }

                if (lesson.Status == LessonStatus.Cancelled)
                {
                    return new ScheduleLessonOperationResult(
                        true,
                        lesson,
                        "Ders oturumu zaten iptal edilmiş durumda.",
                        Array.Empty<string>());
                }

                lesson.Status = LessonStatus.Cancelled;

                if (!string.IsNullOrWhiteSpace(cancelReason))
                {
                    var cancelNote = $"[İptal: {cancelReason.Trim()}]";
                    lesson.Notes = AppendNoteWithLimit(lesson.Notes, cancelNote);
                }

                await _dbContext.SaveChangesAsync(ct);

                var startLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(lesson.StartUtc, DateTimeKind.Utc),
                    _timeZone);

                return new ScheduleLessonOperationResult(
                    true,
                    lesson,
                    $"{lesson.StudentSubject.Student.FullName} — '{lesson.StudentSubject.Subject.Name}' ({startLocal:dd.MM.yyyy HH:mm}) dersi iptal edildi. Bu saat aralığı yeni dersler için serbest bırakıldı.",
                    Array.Empty<string>());
            },
            cancellationToken);
    }

    public async Task<ScheduleLessonOperationResult> RestoreCancelledLessonAsync(
        string teacherId,
        int lessonId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        return await ExecuteTeacherAtomicScheduleWriteAsync(
            teacherId,
            async ct =>
            {
                var lesson = await GetOwnedLessonAsync(teacherId, lessonId, ct);
                if (lesson is null || lesson.StudentSubject?.Student is null || lesson.StudentSubject.Subject is null)
                {
                    return Fail("Ders oturumu bulunamadı veya yetkiniz yok.");
                }

                if (lesson.Status != LessonStatus.Cancelled)
                {
                    return Fail("Yalnızca iptal edilmiş bir ders oturumu yeniden planlı duruma alınabilir.");
                }

                if (lesson.StudentSubject.Student.IsArchived)
                {
                    return Fail($"'{lesson.StudentSubject.Student.FullName}' adlı öğrenci arşivlenmiş durumda olduğundan iptal edilen ders yeniden aktifleştirilemez.");
                }

                if (lesson.StudentSubject.Subject.IsArchived)
                {
                    return Fail($"'{lesson.StudentSubject.Subject.Name}' ders türü arşivlenmiş durumda olduğundan iptal edilen ders yeniden aktifleştirilemez.");
                }

                var conflictError = await CheckTeacherTimeConflictAsync(
                    teacherId,
                    lesson.StartUtc,
                    lesson.EndUtc,
                    excludeLessonId: lesson.Id,
                    ct);

                if (conflictError is not null)
                {
                    return Fail(conflictError);
                }

                lesson.Status = LessonStatus.Scheduled;
                await _dbContext.SaveChangesAsync(ct);

                var startLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(lesson.StartUtc, DateTimeKind.Utc),
                    _timeZone);

                return new ScheduleLessonOperationResult(
                    true,
                    lesson,
                    $"{lesson.StudentSubject.Student.FullName} — '{lesson.StudentSubject.Subject.Name}' ({startLocal:dd.MM.yyyy HH:mm}) dersi yeniden planlı (Scheduled) duruma alındı.",
                    Array.Empty<string>());
            },
            cancellationToken);
    }

    public async Task<RescheduleLessonViewModel?> GetLessonForRescheduleAsync(
        string teacherId,
        int lessonId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Ders ertelemek için öğretmen oturumu gereklidir.");
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

        if (lesson is null ||
            lesson.StudentSubject?.Student is null ||
            lesson.StudentSubject.Subject is null ||
            lesson.Status == LessonStatus.Completed)
        {
            return null;
        }

        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(lesson.StartUtc, DateTimeKind.Utc),
            _timeZone);
        var endLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(lesson.EndUtc, DateTimeKind.Utc),
            _timeZone);

        var currentDate = DateOnly.FromDateTime(startLocal);
        var currentStartTime = TimeOnly.FromDateTime(startLocal);

        return new RescheduleLessonViewModel
        {
            LessonId = lesson.Id,
            StudentId = lesson.StudentSubject.StudentId,
            StudentFullName = lesson.StudentSubject.Student.FullName,
            SubjectName = lesson.StudentSubject.Subject.Name,
            CurrentDate = currentDate,
            CurrentStartTime = currentStartTime,
            CurrentEndTime = TimeOnly.FromDateTime(endLocal),
            CurrentDurationMinutes = lesson.DurationMinutes,
            HourlyRateSnapshot = lesson.HourlyRateSnapshot,
            SessionFeeSnapshot = lesson.SessionFeeSnapshot,
            NewDate = currentDate,
            NewStartTime = currentStartTime,
            NewDurationMinutes = lesson.DurationMinutes
        };
    }

    public async Task<ScheduleLessonOperationResult> RescheduleLessonAsync(
        string teacherId,
        RescheduleLessonViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        if (model.NewDurationMinutes < 15 || model.NewDurationMinutes > 360)
        {
            return Fail("Ders oturumu süresi 15 ile 360 dakika arasında olmalıdır.");
        }

        return await ExecuteTeacherAtomicScheduleWriteAsync(
            teacherId,
            async ct =>
            {
                var lesson = await GetOwnedLessonAsync(teacherId, model.LessonId, ct);
                if (lesson is null || lesson.StudentSubject?.Student is null || lesson.StudentSubject.Subject is null)
                {
                    return Fail("Ertelenmek istenen ders oturumu bulunamadı veya yetkiniz yok.");
                }

                if (lesson.Status == LessonStatus.Completed)
                {
                    return Fail("Tamamlanmış bir ders oturumu ertelenemez.");
                }

                var oldStartLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(lesson.StartUtc, DateTimeKind.Utc),
                    _timeZone);

                var newStartLocal = model.NewDate.ToDateTime(model.NewStartTime, DateTimeKind.Unspecified);
                var newStartUtc = TimeZoneInfo.ConvertTimeToUtc(newStartLocal, _timeZone);
                var newEndUtc = newStartUtc.AddMinutes(model.NewDurationMinutes);

                var conflictError = await CheckTeacherTimeConflictAsync(
                    teacherId,
                    newStartUtc,
                    newEndUtc,
                    excludeLessonId: lesson.Id,
                    ct);

                if (conflictError is not null)
                {
                    return Fail(conflictError);
                }

                var trail = $"[Ertelendi: {oldStartLocal:dd.MM.yyyy HH:mm} → {newStartLocal:dd.MM.yyyy HH:mm}]";
                if (!string.IsNullOrWhiteSpace(model.Reason))
                {
                    trail += $" {model.Reason.Trim()}";
                }

                lesson.StartUtc = newStartUtc;
                lesson.EndUtc = newEndUtc;
                lesson.DurationMinutes = model.NewDurationMinutes;
                lesson.SessionFeeSnapshot = Lesson.CalculateSessionFee(lesson.HourlyRateSnapshot, model.NewDurationMinutes);
                lesson.Status = LessonStatus.Rescheduled;
                lesson.Notes = AppendNoteWithLimit(lesson.Notes, trail);

                await _dbContext.SaveChangesAsync(ct);

                var newEndLocal = newStartLocal.AddMinutes(model.NewDurationMinutes);
                return new ScheduleLessonOperationResult(
                    true,
                    lesson,
                    $"{lesson.StudentSubject.Student.FullName} — '{lesson.StudentSubject.Subject.Name}' dersi {model.NewDate:dd.MM.yyyy} {newStartLocal:HH:mm}–{newEndLocal:HH:mm} tarihine ertelendi.",
                    Array.Empty<string>());
            },
            cancellationToken);
    }

    public async Task<ScheduleLessonOperationResult> DeleteLessonAsync(
        string teacherId,
        int lessonId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        return await ExecuteTeacherAtomicScheduleWriteAsync(
            teacherId,
            async ct =>
            {
                var lesson = await GetOwnedLessonAsync(teacherId, lessonId, ct);
                if (lesson is null || lesson.StudentSubject?.Student is null || lesson.StudentSubject.Subject is null)
                {
                    return Fail("Kaldırılmak istenen ders oturumu bulunamadı veya yetkiniz yok.");
                }

                if (lesson.Status == LessonStatus.Completed)
                {
                    return Fail("Tamamlanmış bir ders oturumu kaldırılamaz. Geçmiş ve finansal bütünlük için tamamlanan ders kayıtları korunur.");
                }

                var studentName = lesson.StudentSubject.Student.FullName;
                var subjectName = lesson.StudentSubject.Subject.Name;

                _dbContext.Lessons.Remove(lesson);
                await _dbContext.SaveChangesAsync(ct);

                return new ScheduleLessonOperationResult(
                    true,
                    lesson,
                    $"{studentName} — '{subjectName}' planlı ders kaydı takvimden kaldırıldı.",
                    Array.Empty<string>());
            },
            cancellationToken);
    }

    public async Task<bool> HasTimeConflictAsync(
        string teacherId,
        DateTime startUtc,
        DateTime endUtc,
        int? excludeLessonId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId) || endUtc <= startUtc)
        {
            return false;
        }

        var conflictError = await CheckTeacherTimeConflictAsync(
            teacherId,
            startUtc,
            endUtc,
            excludeLessonId,
            cancellationToken);

        return conflictError is not null;
    }

    /// <summary>
    /// Aynı öğretmenin takvim yazma işlemlerini hem öğretmen bazlı asenkron kilit hem de
    /// ilişkisel veritabanında Serializable transaction içinde atomik olarak yürütür (Faz 22, FR-043).
    /// </summary>
    private async Task<ScheduleLessonOperationResult> ExecuteTeacherAtomicScheduleWriteAsync(
        string teacherId,
        Func<CancellationToken, Task<ScheduleLessonOperationResult>> action,
        CancellationToken cancellationToken)
    {
        var teacherLock = TeacherScheduleLocks.GetOrAdd(teacherId, static _ => new SemaphoreSlim(1, 1));
        await teacherLock.WaitAsync(cancellationToken);

        try
        {
            var isRelational = _dbContext.Database.IsRelational();
            await using var transaction = isRelational
                ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;

            try
            {
                var result = await action(cancellationToken);
                if (transaction is not null)
                {
                    if (result.Succeeded)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                    else
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                }

                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return Fail("Bu ders kaydı başka bir oturumda eşzamanlı olarak güncellendi. Lütfen takvimi yenileyip tekrar deneyiniz.");
            }
            catch (DbUpdateException)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return Fail("Takvim kaydı sırasında eşzamanlı bir işlem veya veri kısıtı çakışması oluştu. Lütfen tekrar deneyiniz.");
            }
        }
        finally
        {
            teacherLock.Release();
        }
    }

    private async Task<Lesson?> GetOwnedLessonAsync(
        string teacherId,
        int lessonId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Lessons
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
    }

    private static string AppendNoteWithLimit(string? existingNotes, string addition)
    {
        var combined = string.IsNullOrWhiteSpace(existingNotes)
            ? addition
            : $"{existingNotes.Trim()} | {addition}";

        return combined.Length <= 1000 ? combined : combined[..1000];
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
