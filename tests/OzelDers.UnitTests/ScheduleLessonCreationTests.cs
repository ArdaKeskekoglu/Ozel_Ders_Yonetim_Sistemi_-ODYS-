using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.UnitTests;

public class ScheduleLessonCreationTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateLessonAsync_CreatesSeparateSingleSubjectSessions_AndFreezesFeeSnapshots()
    {
        await using var context = CreateDbContext(nameof(CreateLessonAsync_CreatesSeparateSingleSubjectSessions_AndFreezesFeeSnapshots));

        var ahmet = new Student
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

        context.Students.Add(ahmet);
        context.Subjects.AddRange(math, geometry);
        await context.SaveChangesAsync();

        var ahmetMath = new StudentSubject
        {
            StudentId = ahmet.Id,
            SubjectId = math.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 2,
            IsActive = true
        };
        var ahmetGeo = new StudentSubject
        {
            StudentId = ahmet.Id,
            SubjectId = geometry.Id,
            HourlyRate = 1200m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1,
            IsActive = true
        };

        context.StudentSubjects.AddRange(ahmetMath, ahmetGeo);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // 1. Pazartesi 15:00-16:00 -> Ahmet / Matematik
        var mondayMath = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ahmetMath.Id,
                LessonDate = new DateOnly(2026, 11, 30),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60,
                Notes = "Türev 1"
            });

        // 2. Çarşamba 15:00-16:00 -> Ahmet / Matematik
        var wednesdayMath = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ahmetMath.Id,
                LessonDate = new DateOnly(2026, 12, 2),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60,
                Notes = "Türev 2"
            });

        // 3. Salı 16:00-17:00 -> Ahmet / Geometri
        var tuesdayGeo = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ahmetGeo.Id,
                LessonDate = new DateOnly(2026, 12, 1),
                StartTime = new TimeOnly(16, 0),
                DurationMinutes = 60,
                Notes = "Üçgenler"
            });

        Assert.True(mondayMath.Succeeded);
        Assert.True(wednesdayMath.Succeeded);
        Assert.True(tuesdayGeo.Succeeded);

        var week = await service.GetWeeklyScheduleAsync("teacher-1", new DateOnly(2026, 11, 30));
        Assert.Equal(3, week.TotalLessonCount);
        Assert.Equal(180, week.TotalActiveMinutes);
        Assert.Equal(3200m, week.TotalActiveFeeSnapshot);
    }

    [Fact]
    public async Task CreateLessonAsync_RejectsOverlappingActiveLessons_AllowsBackToBackAndCancelledSlots()
    {
        await using var context = CreateDbContext(nameof(CreateLessonAsync_RejectsOverlappingActiveLessons_AllowsBackToBackAndCancelledSlots));

        var s1 = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Demir" };
        var s2 = new Student { TeacherId = "teacher-1", FirstName = "Elif", LastName = "Kaya" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.AddRange(s1, s2);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var ss1 = new StudentSubject { StudentId = s1.Id, SubjectId = math.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        var ss2 = new StudentSubject { StudentId = s2.Id, SubjectId = math.Id, HourlyRate = 1100m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        context.StudentSubjects.AddRange(ss1, ss2);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);
        var date = new DateOnly(2026, 11, 30);

        // 15:00 - 16:00 ilk ders
        var first = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ss1.Id,
                LessonDate = date,
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60
            });
        Assert.True(first.Succeeded);

        // 15:30 - 16:30 çakışan ders -> reddedilmeli (FR-043)
        var overlapping = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ss2.Id,
                LessonDate = date,
                StartTime = new TimeOnly(15, 30),
                DurationMinutes = 60
            });
        Assert.False(overlapping.Succeeded);
        Assert.Contains(overlapping.Errors, e => e.Contains("zaman çakışması"));

        // 16:00 - 17:00 ardışık ders [start, end) -> çakışmaz, kabul edilmeli
        var backToBack = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ss2.Id,
                LessonDate = date,
                StartTime = new TimeOnly(16, 0),
                DurationMinutes = 60
            });
        Assert.True(backToBack.Succeeded);

        // İlk ders (15:00 - 16:00) iptal edilirse aynı saate yeni ders eklenebilmeli
        first.Lesson!.Status = LessonStatus.Cancelled;
        await context.SaveChangesAsync();

        var replaceCancelled = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ss2.Id,
                LessonDate = date,
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60
            });
        Assert.True(replaceCancelled.Succeeded);
    }

    [Fact]
    public async Task CreateLessonAsync_RejectsArchivedStudentArchivedSubjectInactiveEnrollmentAndCrossTeacherAccess()
    {
        await using var context = CreateDbContext(nameof(CreateLessonAsync_RejectsArchivedStudentArchivedSubjectInactiveEnrollmentAndCrossTeacherAccess));

        var activeStudent = new Student { TeacherId = "teacher-1", FirstName = "Can", LastName = "Yıldız", IsArchived = false };
        var archivedStudent = new Student { TeacherId = "teacher-1", FirstName = "Eski", LastName = "Öğrenci", IsArchived = true };
        var otherTeacherStudent = new Student { TeacherId = "teacher-2", FirstName = "Başka", LastName = "Öğrenci", IsArchived = false };

        var activeSubject = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true, IsArchived = false };
        var archivedSubject = new Subject { TeacherId = "teacher-1", Name = "Fizik", IsDefault = false, IsArchived = true };
        var otherTeacherSubject = new Subject { TeacherId = "teacher-2", Name = "Kimya", IsDefault = true, IsArchived = false };

        context.Students.AddRange(activeStudent, archivedStudent, otherTeacherStudent);
        context.Subjects.AddRange(activeSubject, archivedSubject, otherTeacherSubject);
        await context.SaveChangesAsync();

        var inactiveEnrollment = new StudentSubject
        {
            StudentId = activeStudent.Id,
            SubjectId = activeSubject.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1,
            IsActive = false
        };
        var archivedStudentEnrollment = new StudentSubject
        {
            StudentId = archivedStudent.Id,
            SubjectId = activeSubject.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1,
            IsActive = true
        };
        var archivedSubjectEnrollment = new StudentSubject
        {
            StudentId = activeStudent.Id,
            SubjectId = archivedSubject.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1,
            IsActive = true
        };
        var otherTeacherEnrollment = new StudentSubject
        {
            StudentId = otherTeacherStudent.Id,
            SubjectId = otherTeacherSubject.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 1,
            IsActive = true
        };

        context.StudentSubjects.AddRange(
            inactiveEnrollment,
            archivedStudentEnrollment,
            archivedSubjectEnrollment,
            otherTeacherEnrollment);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);
        var date = new DateOnly(2026, 11, 30);

        var r1 = await service.CreateLessonAsync("teacher-1", new CreateLessonViewModel { StudentSubjectId = inactiveEnrollment.Id, LessonDate = date, StartTime = new TimeOnly(10, 0), DurationMinutes = 60 });
        var r2 = await service.CreateLessonAsync("teacher-1", new CreateLessonViewModel { StudentSubjectId = archivedStudentEnrollment.Id, LessonDate = date, StartTime = new TimeOnly(11, 0), DurationMinutes = 60 });
        var r3 = await service.CreateLessonAsync("teacher-1", new CreateLessonViewModel { StudentSubjectId = archivedSubjectEnrollment.Id, LessonDate = date, StartTime = new TimeOnly(12, 0), DurationMinutes = 60 });
        var r4 = await service.CreateLessonAsync("teacher-1", new CreateLessonViewModel { StudentSubjectId = otherTeacherEnrollment.Id, LessonDate = date, StartTime = new TimeOnly(13, 0), DurationMinutes = 60 });

        Assert.False(r1.Succeeded);
        Assert.False(r2.Succeeded);
        Assert.False(r3.Succeeded);
        Assert.False(r4.Succeeded);
        Assert.Equal(0, await context.Lessons.CountAsync());
    }
}
