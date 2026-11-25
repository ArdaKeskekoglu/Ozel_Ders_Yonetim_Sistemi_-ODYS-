using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;

namespace OzelDers.UnitTests;

public class LessonEntityAndSnapshotTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateFromStudentSubject_BindsSingleSubject_AndFreezesFeeSnapshotsAgainstLaterRateChanges()
    {
        await using var context = CreateDbContext(nameof(CreateFromStudentSubject_BindsSingleSubject_AndFreezesFeeSnapshotsAgainstLaterRateChanges));

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
            WeeklyTargetCount = 2,
            IsActive = true
        };
        var geoEnrollment = new StudentSubject
        {
            StudentId = student.Id,
            SubjectId = geometry.Id,
            HourlyRate = 1200m,
            DefaultDurationMinutes = 90,
            WeeklyTargetCount = 1,
            IsActive = true
        };

        context.StudentSubjects.AddRange(mathEnrollment, geoEnrollment);
        await context.SaveChangesAsync();

        var mondayStartUtc = new DateTime(2026, 11, 30, 12, 0, 0, DateTimeKind.Utc);
        var tuesdayStartUtc = new DateTime(2026, 12, 1, 13, 0, 0, DateTimeKind.Utc);

        var mathLesson = Lesson.CreateFromStudentSubject(
            mathEnrollment,
            mondayStartUtc,
            notes: " Türev giriş ");

        var geoLesson = Lesson.CreateFromStudentSubject(
            geoEnrollment,
            tuesdayStartUtc,
            notes: "Üçgende benzerlik");

        context.Lessons.AddRange(mathLesson, geoLesson);
        await context.SaveChangesAsync();

        // Sonradan öğrencinin saatlik ücreti ve öğretmenin varsayılan dersi değişse bile geçmiş ders oturumlarının snapshot ücreti ve ders türü sabit kalmalı (FR-030)
        mathEnrollment.HourlyRate = 1500m;
        math.IsDefault = false;
        geometry.IsDefault = true;
        await context.SaveChangesAsync();

        var savedMathLesson = await context.Lessons
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Subject)
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Student)
            .FirstAsync(l => l.Id == mathLesson.Id);

        var savedGeoLesson = await context.Lessons
            .Include(l => l.StudentSubject)
            .ThenInclude(ss => ss!.Subject)
            .FirstAsync(l => l.Id == geoLesson.Id);

        Assert.Equal(mathEnrollment.Id, savedMathLesson.StudentSubjectId);
        Assert.Equal("Matematik", savedMathLesson.StudentSubject!.Subject!.Name);
        Assert.Equal("Ahmet Yılmaz", savedMathLesson.StudentSubject.Student!.FullName);
        Assert.Equal(60, savedMathLesson.DurationMinutes);
        Assert.Equal(mondayStartUtc.AddMinutes(60), savedMathLesson.EndUtc);
        Assert.Equal(1000m, savedMathLesson.HourlyRateSnapshot);
        Assert.Equal(1000m, savedMathLesson.SessionFeeSnapshot);
        Assert.Equal(LessonStatus.Scheduled, savedMathLesson.Status);
        Assert.Equal("Türev giriş", savedMathLesson.Notes);

        // 90 dakikalık (1.5 saat) Geometri oturumu: 1200 TL/saat -> 1800 TL oturum ücreti
        Assert.Equal(geoEnrollment.Id, savedGeoLesson.StudentSubjectId);
        Assert.Equal("Geometri", savedGeoLesson.StudentSubject!.Subject!.Name);
        Assert.Equal(90, savedGeoLesson.DurationMinutes);
        Assert.Equal(1200m, savedGeoLesson.HourlyRateSnapshot);
        Assert.Equal(1800m, savedGeoLesson.SessionFeeSnapshot);
    }

    [Fact]
    public void CreateFromStudentSubject_SupportsCustomSessionDurationAndRejectsInvalidDuration()
    {
        var enrollment = new StudentSubject
        {
            Id = 10,
            StudentId = 1,
            SubjectId = 2,
            HourlyRate = 900m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1
        };

        var startUtc = new DateTime(2026, 12, 2, 10, 0, 0, DateTimeKind.Utc);
        var blockLesson = Lesson.CreateFromStudentSubject(enrollment, startUtc, overrideDurationMinutes: 120);

        Assert.Equal(120, blockLesson.DurationMinutes);
        Assert.Equal(startUtc.AddMinutes(120), blockLesson.EndUtc);
        Assert.Equal(900m, blockLesson.HourlyRateSnapshot);
        Assert.Equal(1800m, blockLesson.SessionFeeSnapshot);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lesson.CreateFromStudentSubject(enrollment, startUtc, overrideDurationMinutes: 10));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lesson.CreateFromStudentSubject(enrollment, startUtc, overrideDurationMinutes: 400));
    }

    [Fact]
    public void LessonEntity_DoesNotContainRedundantStudentSubjectOrTeacherForeignKeys()
    {
        var lessonProperties = typeof(Lesson)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet();

        Assert.Contains(nameof(Lesson.StudentSubjectId), lessonProperties);
        Assert.DoesNotContain("StudentId", lessonProperties);
        Assert.DoesNotContain("SubjectId", lessonProperties);
        Assert.DoesNotContain("TeacherId", lessonProperties);
    }
}
