using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.UnitTests;

public class ScheduleLessonDetailsAndRecurrenceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateLessonAsync_WithRepeatWeeks_GeneratesWeeklyRecurringSessions_AndSupportsSingleWeekExceptions()
    {
        await using var context = CreateDbContext(nameof(CreateLessonAsync_WithRepeatWeeks_GeneratesWeeklyRecurringSessions_AndSupportsSingleWeekExceptions));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Yılmaz" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.Add(ahmet);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var enrollment = new StudentSubject
        {
            StudentId = ahmet.Id,
            SubjectId = math.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1,
            IsActive = true
        };
        context.StudentSubjects.Add(enrollment);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // 30 Kasım 2026 Pazartesi 15:00-16:00'dan itibaren 4 hafta tekrarlayan ders üret (FR-046)
        var seriesResult = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = enrollment.Id,
                LessonDate = new DateOnly(2026, 11, 30),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60,
                RepeatWeeks = 4,
                Notes = "Haftalık AYT Matematik"
            });

        Assert.True(seriesResult.Succeeded);
        Assert.Equal(4, seriesResult.CreatedCount);

        var allLessons = await context.Lessons
            .OrderBy(l => l.StartUtc)
            .ToListAsync();

        Assert.Equal(4, allLessons.Count);
        Assert.Equal(new DateTime(2026, 11, 30, 15, 0, 0, DateTimeKind.Utc), allLessons[0].StartUtc);
        Assert.Equal(new DateTime(2026, 12, 7, 15, 0, 0, DateTimeKind.Utc), allLessons[1].StartUtc);
        Assert.Equal(new DateTime(2026, 12, 14, 15, 0, 0, DateTimeKind.Utc), allLessons[2].StartUtc);
        Assert.Equal(new DateTime(2026, 12, 21, 15, 0, 0, DateTimeKind.Utc), allLessons[3].StartUtc);

        // Tek Hafta İstisnası (FR-047): Yalnızca 2. haftadaki (7 Aralık) dersi Salı (8 Aralık) 17:00'ye erteleyelim ve 3. haftadaki (14 Aralık) dersi iptal edelim
        var week2Reschedule = await service.RescheduleLessonAsync(
            "teacher-1",
            new RescheduleLessonViewModel
            {
                LessonId = allLessons[1].Id,
                NewDate = new DateOnly(2026, 12, 8),
                NewStartTime = new TimeOnly(17, 0),
                NewDurationMinutes = 60,
                Reason = "Yalnızca 2. hafta Salı gününe alındı"
            });
        Assert.True(week2Reschedule.Succeeded);

        var week3Cancel = await service.CancelLessonAsync("teacher-1", allLessons[2].Id, "3. hafta tatil");
        Assert.True(week3Cancel.Succeeded);

        // 1. hafta ve 4. hafta hiçbir şekilde etkilenmeden Pazartesi 15:00 ve Scheduled olarak kalmalı!
        var reloaded = await context.Lessons.OrderBy(l => l.Id).ToListAsync();
        Assert.Equal(new DateTime(2026, 11, 30, 15, 0, 0, DateTimeKind.Utc), reloaded[0].StartUtc);
        Assert.Equal(LessonStatus.Scheduled, reloaded[0].Status);

        Assert.Equal(new DateTime(2026, 12, 8, 17, 0, 0, DateTimeKind.Utc), reloaded[1].StartUtc);
        Assert.Equal(LessonStatus.Rescheduled, reloaded[1].Status);

        Assert.Equal(LessonStatus.Cancelled, reloaded[2].Status);

        Assert.Equal(new DateTime(2026, 12, 21, 15, 0, 0, DateTimeKind.Utc), reloaded[3].StartUtc);
        Assert.Equal(LessonStatus.Scheduled, reloaded[3].Status);
    }

    [Fact]
    public async Task CreateLessonAsync_WithRepeatWeeks_RollsBackEntireSeriesWhenAnyFutureWeekConflicts()
    {
        await using var context = CreateDbContext(nameof(CreateLessonAsync_WithRepeatWeeks_RollsBackEntireSeriesWhenAnyFutureWeekConflicts));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Demir" };
        var elif = new Student { TeacherId = "teacher-1", FirstName = "Elif", LastName = "Kaya" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.AddRange(ahmet, elif);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var ahmetMath = new StudentSubject { StudentId = ahmet.Id, SubjectId = math.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        var elifMath = new StudentSubject { StudentId = elif.Id, SubjectId = math.Id, HourlyRate = 1100m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        context.StudentSubjects.AddRange(ahmetMath, elifMath);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // 3. haftada (14 Aralık 2026 Pazartesi 15:00) Elif'in önceden planlı bir dersi olsun
        var existingWeek3 = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = elifMath.Id,
                LessonDate = new DateOnly(2026, 12, 14),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60
            });
        Assert.True(existingWeek3.Succeeded);

        // Ahmet için 30 Kasım'dan başlayan 4 haftalık seri eklenmek istendiğinde 3. haftadaki çakışma yüzünden seri tamamen reddedilmeli (1. ve 2. haftaya da yarım kayıt düşmemeli)
        var seriesAttempt = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ahmetMath.Id,
                LessonDate = new DateOnly(2026, 11, 30),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60,
                RepeatWeeks = 4
            });

        Assert.False(seriesAttempt.Succeeded);
        Assert.Contains(seriesAttempt.Errors, e => e.Contains("3. hafta (14.12.2026)"));
        Assert.Equal(1, await context.Lessons.CountAsync());
    }

    [Fact]
    public async Task GetLessonDetailsAsync_ReturnsFullSessionDetailsAndRelatedOccurrences_AndEnforcesTeacherIsolation()
    {
        await using var context = CreateDbContext(nameof(GetLessonDetailsAsync_ReturnsFullSessionDetailsAndRelatedOccurrences_AndEnforcesTeacherIsolation));

        var ahmet = new Student
        {
            TeacherId = "teacher-1",
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            PhoneNumber = "0555 000 11 22"
        };
        var math = new Subject
        {
            TeacherId = "teacher-1",
            Name = "Matematik",
            IsDefault = true
        };

        context.Students.Add(ahmet);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var enrollment = new StudentSubject
        {
            StudentId = ahmet.Id,
            SubjectId = math.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1
        };
        context.StudentSubjects.Add(enrollment);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        var created = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = enrollment.Id,
                LessonDate = new DateOnly(2026, 11, 30),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60,
                RepeatWeeks = 3,
                Notes = "Türev serisi"
            });
        Assert.True(created.Succeeded);

        var details = await service.GetLessonDetailsAsync("teacher-1", created.Lesson!.Id);
        Assert.NotNull(details);
        Assert.Equal("Ahmet Yılmaz", details.StudentFullName);
        Assert.Equal("Matematik", details.SubjectName);
        Assert.Equal(1000m, details.HourlyRateSnapshot);
        Assert.Equal(1000m, details.SessionFeeSnapshot);
        Assert.Equal("15:00 – 16:00", details.TimeRangeText);
        // 3 haftalık seride bu ders dışındaki diğer 2 hafta RelatedSeriesLessons içinde gelmeli
        Assert.Equal(2, details.RelatedSeriesLessons.Count);

        // Başka öğretmen bu dersin detayını görememeli
        Assert.Null(await service.GetLessonDetailsAsync("teacher-2", created.Lesson.Id));
    }
}
