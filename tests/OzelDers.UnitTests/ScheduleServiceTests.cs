using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;
using OzelDers.Web.Services;

namespace OzelDers.UnitTests;

public class ScheduleServiceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Theory]
    [InlineData("2026-11-23", "2026-11-23")] // Pazartesi -> Aynı Pazartesi
    [InlineData("2026-11-25", "2026-11-23")] // Çarşamba -> 23 Kasım Pazartesi
    [InlineData("2026-11-29", "2026-11-23")] // Pazar -> 23 Kasım Pazartesi (ISO 8601)
    [InlineData("2026-11-30", "2026-11-30")] // Sonraki Pazartesi -> 30 Kasım Pazartesi
    public void GetMondayOfWeek_CalculatesIsoMondayCorrectlyForAllDaysOfWeek(
        string referenceDateStr,
        string expectedMondayStr)
    {
        using var context = CreateDbContext(nameof(GetMondayOfWeek_CalculatesIsoMondayCorrectlyForAllDaysOfWeek) + referenceDateStr);
        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        var referenceDate = DateOnly.Parse(referenceDateStr);
        var expectedMonday = DateOnly.Parse(expectedMondayStr);

        var actualMonday = service.GetMondayOfWeek(referenceDate);

        Assert.Equal(expectedMonday, actualMonday);
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_PopulatesSevenDays_NavigatesPreviousAndNextWeeks_AndCalculatesSummary()
    {
        await using var context = CreateDbContext(nameof(GetWeeklyScheduleAsync_PopulatesSevenDays_NavigatesPreviousAndNextWeeks_AndCalculatesSummary));

        var student = new Student
        {
            TeacherId = "teacher-1",
            FirstName = "Ahmet",
            LastName = "Yılmaz"
        };
        var math = new Subject
        {
            TeacherId = "teacher-1",
            Name = "Matematik",
            IsDefault = true
        };
        var geometry = new Subject
        {
            TeacherId = "teacher-1",
            Name = "Geometri",
            IsDefault = false
        };

        context.Students.Add(student);
        context.Subjects.AddRange(math, geometry);
        await context.SaveChangesAsync();

        var mathEnrollment = new StudentSubject
        {
            StudentId = student.Id,
            SubjectId = math.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 2
        };
        var geoEnrollment = new StudentSubject
        {
            StudentId = student.Id,
            SubjectId = geometry.Id,
            HourlyRate = 1200m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1
        };

        context.StudentSubjects.AddRange(mathEnrollment, geoEnrollment);
        await context.SaveChangesAsync();

        // Seçili hafta: 30 Kasım 2026 Pazartesi – 6 Aralık 2026 Pazar (UTC)
        var mondayMath = Lesson.CreateFromStudentSubject(
            mathEnrollment,
            new DateTime(2026, 11, 30, 15, 0, 0, DateTimeKind.Utc));

        var tuesdayGeo = Lesson.CreateFromStudentSubject(
            geoEnrollment,
            new DateTime(2026, 12, 1, 16, 0, 0, DateTimeKind.Utc));
        tuesdayGeo.Status = LessonStatus.Completed;

        var wednesdayCancelled = Lesson.CreateFromStudentSubject(
            mathEnrollment,
            new DateTime(2026, 12, 2, 15, 0, 0, DateTimeKind.Utc));
        wednesdayCancelled.Status = LessonStatus.Cancelled;

        // Sonraki haftadaki ders (7 Aralık 2026 Pazartesi) -> bu haftada görünmemeli
        var nextWeekLesson = Lesson.CreateFromStudentSubject(
            mathEnrollment,
            new DateTime(2026, 12, 7, 15, 0, 0, DateTimeKind.Utc));

        context.Lessons.AddRange(mondayMath, tuesdayGeo, wednesdayCancelled, nextWeekLesson);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // Çarşamba tarihi (2 Aralık 2026) verilse bile 30 Kasım – 6 Aralık haftasını getirmeli
        var currentWeek = await service.GetWeeklyScheduleAsync("teacher-1", new DateOnly(2026, 12, 2));

        Assert.Equal(new DateOnly(2026, 11, 30), currentWeek.WeekStartDate);
        Assert.Equal(new DateOnly(2026, 12, 6), currentWeek.WeekEndDate);
        Assert.Equal(new DateOnly(2026, 11, 23), currentWeek.PreviousWeekStartDate);
        Assert.Equal(new DateOnly(2026, 12, 7), currentWeek.NextWeekStartDate);
        Assert.Equal(7, currentWeek.Days.Count);

        Assert.Equal(3, currentWeek.TotalLessonCount);
        Assert.Equal(1, currentWeek.ScheduledLessonCount);
        Assert.Equal(1, currentWeek.CompletedLessonCount);
        Assert.Equal(1, currentWeek.CancelledLessonCount);
        // İptal edilen ders aktif süre ve aktif ücret toplamına katılmaz (1000 + 1200 = 2200 TL, 120 dk)
        Assert.Equal(120, currentWeek.TotalActiveMinutes);
        Assert.Equal(2200m, currentWeek.TotalActiveFeeSnapshot);

        Assert.Single(currentWeek.Days[0].Lessons); // Pazartesi
        Assert.Equal("Matematik", currentWeek.Days[0].Lessons[0].SubjectName);
        Assert.Single(currentWeek.Days[1].Lessons); // Salı
        Assert.Equal("Geometri", currentWeek.Days[1].Lessons[0].SubjectName);
        Assert.Single(currentWeek.Days[2].Lessons); // Çarşamba

        // Sonraki haftaya geçiş (NextWeekStartDate) yalnızca 7 Aralık dersini getirmeli
        var nextWeek = await service.GetWeeklyScheduleAsync("teacher-1", currentWeek.NextWeekStartDate);
        Assert.Equal(new DateOnly(2026, 12, 7), nextWeek.WeekStartDate);
        Assert.Equal(1, nextWeek.TotalLessonCount);
        Assert.Single(nextWeek.Days[0].Lessons);
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_IsolatesTeachers_AndNeverReturnsOtherTeachersLessons()
    {
        await using var context = CreateDbContext(nameof(GetWeeklyScheduleAsync_IsolatesTeachers_AndNeverReturnsOtherTeachersLessons));

        var t1Student = new Student { TeacherId = "teacher-1", FirstName = "Ali", LastName = "Veli" };
        var t2Student = new Student { TeacherId = "teacher-2", FirstName = "Ayşe", LastName = "Fatma" };
        var t1Subj = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };
        var t2Subj = new Subject { TeacherId = "teacher-2", Name = "Fizik", IsDefault = true };

        context.Students.AddRange(t1Student, t2Student);
        context.Subjects.AddRange(t1Subj, t2Subj);
        await context.SaveChangesAsync();

        var ss1 = new StudentSubject { StudentId = t1Student.Id, SubjectId = t1Subj.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        var ss2 = new StudentSubject { StudentId = t2Student.Id, SubjectId = t2Subj.Id, HourlyRate = 1500m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        context.StudentSubjects.AddRange(ss1, ss2);
        await context.SaveChangesAsync();

        var startUtc = new DateTime(2026, 11, 30, 10, 0, 0, DateTimeKind.Utc);
        context.Lessons.AddRange(
            Lesson.CreateFromStudentSubject(ss1, startUtc),
            Lesson.CreateFromStudentSubject(ss2, startUtc));
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);
        var t1Schedule = await service.GetWeeklyScheduleAsync("teacher-1", new DateOnly(2026, 11, 30));

        Assert.Equal(1, t1Schedule.TotalLessonCount);
        var onlyCard = Assert.Single(t1Schedule.Days[0].Lessons);
        Assert.Equal("Ali Veli", onlyCard.StudentFullName);
        Assert.Equal("Matematik", onlyCard.SubjectName);
    }
}
