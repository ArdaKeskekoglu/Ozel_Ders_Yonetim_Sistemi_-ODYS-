using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.UnitTests;

public class ScheduleLessonCancelAndRescheduleTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CancelAndRestoreLessonAsync_SeparatesCancelledInWeeklySchedule_FreesSlot_AndChecksConflictsOnRestore()
    {
        await using var context = CreateDbContext(nameof(CancelAndRestoreLessonAsync_SeparatesCancelledInWeeklySchedule_FreesSlot_AndChecksConflictsOnRestore));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Yılmaz" };
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
        var date = new DateOnly(2026, 11, 30);

        var created = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = ahmetMath.Id,
                LessonDate = date,
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60
            });
        Assert.True(created.Succeeded);

        // Dersi iptal et
        var cancelResult = await service.CancelLessonAsync("teacher-1", created.Lesson!.Id, "Öğrenci rahatsızlandı");
        Assert.True(cancelResult.Succeeded);
        Assert.Equal(LessonStatus.Cancelled, cancelResult.Lesson!.Status);
        Assert.Contains("[İptal: Öğrenci rahatsızlandı]", cancelResult.Lesson.Notes);

        // Haftalık takvimde iptal edilen ders görünmeli ancak aktif süre/ücret toplamına katılmamalı (FR-045)
        var weekAfterCancel = await service.GetWeeklyScheduleAsync("teacher-1", date);
        Assert.Equal(1, weekAfterCancel.TotalLessonCount);
        Assert.Equal(1, weekAfterCancel.CancelledLessonCount);
        Assert.Equal(0, weekAfterCancel.TotalActiveMinutes);
        Assert.Equal(0m, weekAfterCancel.TotalActiveFeeSnapshot);
        var card = Assert.Single(weekAfterCancel.Days[0].Lessons);
        Assert.True(card.IsCancelled);
        Assert.Equal("İptal Edildi", card.StatusBadgeText);

        // Saat boşken iptal edilen dersi yeniden aktif et (Restore) -> başarılı olmalı
        var restoreSuccess = await service.RestoreCancelledLessonAsync("teacher-1", created.Lesson.Id);
        Assert.True(restoreSuccess.Succeeded);
        Assert.Equal(LessonStatus.Scheduled, restoreSuccess.Lesson!.Status);

        // Tekrar iptal edip aynı saate Elif'in dersini koyalım
        await service.CancelLessonAsync("teacher-1", created.Lesson.Id);
        var elifLesson = await service.CreateLessonAsync(
            "teacher-1",
            new CreateLessonViewModel
            {
                StudentSubjectId = elifMath.Id,
                LessonDate = date,
                StartTime = new TimeOnly(15, 0),
                DurationMinutes = 60
            });
        Assert.True(elifLesson.Succeeded);

        // Artık aynı saatte Elif'in aktif dersi varken Ahmet'in iptal edilen dersi geri alınamamalı
        var restoreConflict = await service.RestoreCancelledLessonAsync("teacher-1", created.Lesson.Id);
        Assert.False(restoreConflict.Succeeded);
        Assert.Contains(restoreConflict.Errors, e => e.Contains("zaman çakışması"));
    }

    [Fact]
    public async Task RescheduleLessonAsync_MarksLessonAsRescheduled_RecordsAuditTrail_AndEnforcesConflictRules()
    {
        await using var context = CreateDbContext(nameof(RescheduleLessonAsync_MarksLessonAsRescheduled_RecordsAuditTrail_AndEnforcesConflictRules));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Demir" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.Add(ahmet);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var enrollment = new StudentSubject { StudentId = ahmet.Id, SubjectId = math.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
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
                Notes = "Türev"
            });
        Assert.True(created.Succeeded);

        var rescheduleVm = await service.GetLessonForRescheduleAsync("teacher-1", created.Lesson!.Id);
        Assert.NotNull(rescheduleVm);

        rescheduleVm.NewDate = new DateOnly(2026, 12, 3); // Perşembe
        rescheduleVm.NewStartTime = new TimeOnly(18, 0);
        rescheduleVm.NewDurationMinutes = 90;
        rescheduleVm.Reason = "Veli talebiyle Perşembe akşamına alındı";

        var rescheduleResult = await service.RescheduleLessonAsync("teacher-1", rescheduleVm);
        Assert.True(rescheduleResult.Succeeded);
        Assert.NotNull(rescheduleResult.Lesson);
        Assert.Equal(LessonStatus.Rescheduled, rescheduleResult.Lesson.Status);
        Assert.Equal(new DateTime(2026, 12, 3, 18, 0, 0, DateTimeKind.Utc), rescheduleResult.Lesson.StartUtc);
        Assert.Equal(90, rescheduleResult.Lesson.DurationMinutes);
        Assert.Equal(1500m, rescheduleResult.Lesson.SessionFeeSnapshot);
        Assert.Contains("[Ertelendi: 30.11.2026 15:00 → 03.12.2026 18:00]", rescheduleResult.Lesson.Notes);
        Assert.Contains("Veli talebiyle Perşembe akşamına alındı", rescheduleResult.Lesson.Notes);
    }

    [Fact]
    public async Task CompletedLessons_CannotBeCancelledRescheduledOrDeleted_WhileScheduledLessonsCanBeDeleted()
    {
        await using var context = CreateDbContext(nameof(CompletedLessons_CannotBeCancelledRescheduledOrDeleted_WhileScheduledLessonsCanBeDeleted));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Yılmaz" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.Add(ahmet);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var enrollment = new StudentSubject { StudentId = ahmet.Id, SubjectId = math.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        context.StudentSubjects.Add(enrollment);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        var completedLesson = Lesson.CreateFromStudentSubject(
            enrollment,
            new DateTime(2026, 11, 30, 10, 0, 0, DateTimeKind.Utc));
        completedLesson.Status = LessonStatus.Completed;

        var scheduledLesson = Lesson.CreateFromStudentSubject(
            enrollment,
            new DateTime(2026, 11, 30, 14, 0, 0, DateTimeKind.Utc));

        context.Lessons.AddRange(completedLesson, scheduledLesson);
        await context.SaveChangesAsync();

        // Tamamlanmış ders iptal edilemez, ertelenemez ve silinemez
        Assert.False((await service.CancelLessonAsync("teacher-1", completedLesson.Id)).Succeeded);
        Assert.Null(await service.GetLessonForRescheduleAsync("teacher-1", completedLesson.Id));
        Assert.False((await service.DeleteLessonAsync("teacher-1", completedLesson.Id)).Succeeded);

        // Başka öğretmen planlı dersi iptal edemez veya silemez
        Assert.False((await service.CancelLessonAsync("teacher-2", scheduledLesson.Id)).Succeeded);
        Assert.False((await service.DeleteLessonAsync("teacher-2", scheduledLesson.Id)).Succeeded);

        // Kendi öğretmeni henüz tamamlanmamış planlı dersi kaldırabilir (FR-042)
        var deleteScheduled = await service.DeleteLessonAsync("teacher-1", scheduledLesson.Id);
        Assert.True(deleteScheduled.Succeeded);
        Assert.Equal(1, await context.Lessons.CountAsync());
    }
}
