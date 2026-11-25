using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.UnitTests;

public class ScheduleLessonEditTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task UpdateLessonAsync_MovesDayAndTime_PreservesHourlySnapshotWhenSubjectUnchanged_AndDoesNotAffectOtherLessons()
    {
        await using var context = CreateDbContext(nameof(UpdateLessonAsync_MovesDayAndTime_PreservesHourlySnapshotWhenSubjectUnchanged_AndDoesNotAffectOtherLessons));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Yılmaz" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.Add(ahmet);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var mathEnrollment = new StudentSubject
        {
            StudentId = ahmet.Id,
            SubjectId = math.Id,
            HourlyRate = 1000m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 2,
            IsActive = true
        };
        context.StudentSubjects.Add(mathEnrollment);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // Pazartesi 15:00-16:00 ve Çarşamba 15:00-16:00 iki ders oluştur
        var mondayResult = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = mathEnrollment.Id,
                LessonDate = new DateOnly(2026, 11, 30),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60,
                Notes = "Pazartesi dersi"
            });

        var wednesdayResult = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = mathEnrollment.Id,
                LessonDate = new DateOnly(2026, 12, 2),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60,
                Notes = "Çarşamba dersi"
            });

        Assert.True(mondayResult.Succeeded);
        Assert.True(wednesdayResult.Succeeded);

        // Sonradan öğrencinin saatlik ücreti 1500 TL'ye çıkmış olsun
        mathEnrollment.HourlyRate = 1500m;
        await context.SaveChangesAsync();

        // Pazartesi dersini Salı 17:00-18:30 (90 dk) olarak taşıyalım (aynı StudentSubjectId korunduğu için 1000 TL/saat snapshot'ı korunmalı -> 90 dk = 1500 TL)
        var editVm = await service.GetLessonForEditAsync("teacher-1", mondayResult.Lesson!.Id);
        Assert.NotNull(editVm);

        editVm.LessonDate = new DateOnly(2026, 12, 1); // Salı
        editVm.StartTime = new TimeOnly(17, 0);
        editVm.DurationMinutes = 90;
        editVm.Notes = "Salı gününe taşındı";

        var updateResult = await service.UpdateLessonAsync("teacher-1", editVm);
        Assert.True(updateResult.Succeeded);

        var updatedMondayLesson = await context.Lessons.FirstAsync(l => l.Id == mondayResult.Lesson.Id);
        var untouchedWednesdayLesson = await context.Lessons.FirstAsync(l => l.Id == wednesdayResult.Lesson!.Id);

        Assert.Equal(new DateTime(2026, 12, 1, 17, 0, 0, DateTimeKind.Utc), updatedMondayLesson.StartUtc);
        Assert.Equal(new DateTime(2026, 12, 1, 18, 30, 0, DateTimeKind.Utc), updatedMondayLesson.EndUtc);
        Assert.Equal(90, updatedMondayLesson.DurationMinutes);
        Assert.Equal(1000m, updatedMondayLesson.HourlyRateSnapshot);
        Assert.Equal(1500m, updatedMondayLesson.SessionFeeSnapshot);
        Assert.Equal("Salı gününe taşındı", updatedMondayLesson.Notes);

        // Çarşamba dersi hiçbir şekilde etkilenmemiş olmalı
        Assert.Equal(new DateTime(2026, 12, 2, 15, 0, 0, DateTimeKind.Utc), untouchedWednesdayLesson.StartUtc);
        Assert.Equal(60, untouchedWednesdayLesson.DurationMinutes);
        Assert.Equal("Çarşamba dersi", untouchedWednesdayLesson.Notes);
    }

    [Fact]
    public async Task UpdateLessonAsync_ExcludesSelfFromConflictCheck_AndRejectsCollisionWithAnotherLesson()
    {
        await using var context = CreateDbContext(nameof(UpdateLessonAsync_ExcludesSelfFromConflictCheck_AndRejectsCollisionWithAnotherLesson));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Demir" };
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
            WeeklyTargetCount = 2
        };
        context.StudentSubjects.Add(enrollment);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);
        var date = new DateOnly(2026, 11, 30);

        var lesson1 = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = enrollment.Id,
                LessonDate = date,
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60
            });

        var lesson2 = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = enrollment.Id,
                LessonDate = date,
                StartTime = new TimeOnly(17, 0),
                DurationMinutes = 60
            });

        Assert.True(lesson1.Succeeded);
        Assert.True(lesson2.Succeeded);

        // 1. Dersin kendi saatini 15:00-16:00'dan 15:30-16:30'a kaydırmak kendi eski saatiyle çakışmamalı
        var selfShift = await service.UpdateLessonAsync(
            "teacher-1",
            new EditLessonViewModel
            {
                Id = lesson1.Lesson!.Id,
                StudentSubjectId = enrollment.Id,
                LessonDate = date,
                StartTime = new TimeOnly(15, 30),
                DurationMinutes = 60
            });
        Assert.True(selfShift.Succeeded);

        // 2. Dersin saatini 2. dersle (17:00-18:00) kesişen 16:30-17:30 aralığına taşımak reddedilmeli
        var collision = await service.UpdateLessonAsync(
            "teacher-1",
            new EditLessonViewModel
            {
                Id = lesson1.Lesson.Id,
                StudentSubjectId = enrollment.Id,
                LessonDate = date,
                StartTime = new TimeOnly(16, 30),
                DurationMinutes = 60
            });
        Assert.False(collision.Succeeded);
        Assert.Contains(collision.Errors, e => e.Contains("zaman çakışması"));
    }

    [Fact]
    public async Task UpdateLessonAsync_UpdatesRateSnapshotWhenSubjectChanged_AndRejectsCrossTeacherEdit()
    {
        await using var context = CreateDbContext(nameof(UpdateLessonAsync_UpdatesRateSnapshotWhenSubjectChanged_AndRejectsCrossTeacherEdit));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Yılmaz" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };
        var geo = new Subject { TeacherId = "teacher-1", Name = "Geometri", IsDefault = false };

        context.Students.Add(ahmet);
        context.Subjects.AddRange(math, geo);
        await context.SaveChangesAsync();

        var mathEnrollment = new StudentSubject { StudentId = ahmet.Id, SubjectId = math.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        var geoEnrollment = new StudentSubject { StudentId = ahmet.Id, SubjectId = geo.Id, HourlyRate = 1200m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        context.StudentSubjects.AddRange(mathEnrollment, geoEnrollment);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);
        var created = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = mathEnrollment.Id,
                LessonDate = new DateOnly(2026, 11, 30),
                StartTime = new TimeOnly(14, 0),
                DurationMinutes = 60
            });
        Assert.True(created.Succeeded);

        // Dersin türünü Matematik'ten Geometri'ye değiştirdiğimizde yeni ders türünün ücreti (1200 TL) snapshot olmalı
        var changeSubject = await service.UpdateLessonAsync(
            "teacher-1",
            new EditLessonViewModel
            {
                Id = created.Lesson!.Id,
                StudentSubjectId = geoEnrollment.Id,
                LessonDate = new DateOnly(2026, 11, 30),
                StartTime = new TimeOnly(14, 0),
                DurationMinutes = 60
            });
        Assert.True(changeSubject.Succeeded);
        Assert.Equal(geoEnrollment.Id, changeSubject.Lesson!.StudentSubjectId);
        Assert.Equal(1200m, changeSubject.Lesson.HourlyRateSnapshot);
        Assert.Equal(1200m, changeSubject.Lesson.SessionFeeSnapshot);

        // Başka bir öğretmen bu dersi düzenleyememeli
        Assert.Null(await service.GetLessonForEditAsync("teacher-2", created.Lesson.Id));
        var unauthorized = await service.UpdateLessonAsync(
            "teacher-2",
            new EditLessonViewModel
            {
                Id = created.Lesson.Id,
                StudentSubjectId = geoEnrollment.Id,
                LessonDate = new DateOnly(2026, 11, 30),
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60
            });
        Assert.False(unauthorized.Succeeded);
    }
}
