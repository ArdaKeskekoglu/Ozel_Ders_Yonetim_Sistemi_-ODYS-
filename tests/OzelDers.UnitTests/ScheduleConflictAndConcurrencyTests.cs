using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.UnitTests;

public class ScheduleConflictAndConcurrencyTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Theory]
    [InlineData("15:00", 60, false)] // 1. Birebir aynı aralık (15:00-16:00 vs 15:00-16:00) -> Çakışır
    [InlineData("15:30", 60, false)] // 2. İçten başlayıp dışarı taşan (15:30-16:30 vs 15:00-16:00) -> Çakışır
    [InlineData("14:30", 60, false)] // 3. Önden başlayıp içeride biten (14:30-15:30 vs 15:00-16:00) -> Çakışır
    [InlineData("15:15", 30, false)] // 4. Mevcut dersin tamamen içinde kalan (15:15-15:45 vs 15:00-16:00) -> Çakışır
    [InlineData("14:30", 120, false)] // 5. Mevcut dersi tamamen kapsayan (14:30-16:30 vs 15:00-16:00) -> Çakışır
    [InlineData("14:00", 60, true)]  // 6. Hemen öncesinde biten bitişik ders (14:00-15:00 vs 15:00-16:00) -> Çakışmaz [start, end)
    [InlineData("16:00", 60, true)]  // 7. Hemen sonrasında başlayan bitişik ders (16:00-17:00 vs 15:00-16:00) -> Çakışmaz [start, end)
    public async Task CreateLessonAsync_EnforcesAllHalfOpenIntervalOverlapRules(
        string candidateStartStr,
        int candidateDurationMinutes,
        bool expectedSuccess)
    {
        var dbName = $"{nameof(CreateLessonAsync_EnforcesAllHalfOpenIntervalOverlapRules)}_{candidateStartStr}_{candidateDurationMinutes}";
        await using var context = CreateDbContext(dbName);

        var student = new Student { TeacherId = "teacher-interval", FirstName = "Ahmet", LastName = "Yılmaz" };
        var math = new Subject { TeacherId = "teacher-interval", Name = "Matematik", IsDefault = true };
        context.Students.Add(student);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var enrollment = new StudentSubject
        {
            StudentId = student.Id,
            SubjectId = math.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 2
        };
        context.StudentSubjects.Add(enrollment);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);
        var lessonDate = new DateOnly(2026, 11, 30);

        // Referans ders: 15:00 - 16:00
        var baseLesson = await service.CreateLessonAsync(
            "teacher-interval",
            new CreateLessonViewModel
            {
                StudentSubjectId = enrollment.Id,
                LessonDate = lessonDate,
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60
            });
        Assert.True(baseLesson.Succeeded);

        var candidateStart = TimeOnly.Parse(candidateStartStr);
        var candidateResult = await service.CreateLessonAsync(
            "teacher-interval",
            new CreateLessonViewModel
            {
                StudentSubjectId = enrollment.Id,
                LessonDate = lessonDate,
                StartTime = candidateStart,
                DurationMinutes = candidateDurationMinutes
            });

        Assert.Equal(expectedSuccess, candidateResult.Succeeded);
    }

    [Fact]
    public async Task ConcurrentOverlappingCreateRequests_AllowExactlyOneLesson_AndRejectAllOtherRacingRequests()
    {
        var sharedDbName = nameof(ConcurrentOverlappingCreateRequests_AllowExactlyOneLesson_AndRejectAllOtherRacingRequests);

        await using (var seedContext = CreateDbContext(sharedDbName))
        {
            var student = new Student { TeacherId = "teacher-race", FirstName = "Ahmet", LastName = "Demir" };
            var math = new Subject { TeacherId = "teacher-race", Name = "Matematik", IsDefault = true };
            seedContext.Students.Add(student);
            seedContext.Subjects.Add(math);
            await seedContext.SaveChangesAsync();

            seedContext.StudentSubjects.Add(new StudentSubject
            {
                StudentId = student.Id,
                SubjectId = math.Id,
                HourlyRate = 1000m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 2,
                IsActive = true
            });
            await seedContext.SaveChangesAsync();
        }

        int enrollmentId;
        await using (var readContext = CreateDbContext(sharedDbName))
        {
            enrollmentId = (await readContext.StudentSubjects.FirstAsync()).Id;
        }

        // 8 ayrı paralel DbContext ve ScheduleService örneği aynı anda aynı öğretmenin 15:00-16:00 aralığına ders eklemeye çalışsın
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 8)
            .Select(async index =>
            {
                await startGate.Task;
                await using var scopedContext = CreateDbContext(sharedDbName);
                var scopedService = new ScheduleService(scopedContext, TimeZoneInfo.Utc);

                return await scopedService.CreateLessonAsync(
                    "teacher-race",
                    new CreateLessonViewModel
                    {
                        StudentSubjectId = enrollmentId,
                        LessonDate = new DateOnly(2026, 11, 30),
                        StartTime = new TimeOnly(15, 0),
                        DurationMinutes = 60,
                        Notes = $"Paralel İstek #{index}"
                    });
            })
            .ToArray();

        startGate.SetResult();
        var results = await Task.WhenAll(tasks);

        // Yarışan 8 istekten yalnızca TAM OLARAK 1 tanesi başarılı olmalı, diğer 7 tanesi çakışma nedeniyle reddedilmeli
        Assert.Equal(1, results.Count(r => r.Succeeded));
        Assert.Equal(7, results.Count(r => !r.Succeeded));

        await using var verifyContext = CreateDbContext(sharedDbName);
        Assert.Equal(1, await verifyContext.Lessons.CountAsync());
    }
}
