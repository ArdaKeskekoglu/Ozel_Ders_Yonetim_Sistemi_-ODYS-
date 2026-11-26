using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.UnitTests;

public class ScheduleLessonAttendanceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ElapsedLesson_RemainsPendingAttendanceVerification_AndDoesNotAutoComplete()
    {
        await using var context = CreateDbContext(nameof(ElapsedLesson_RemainsPendingAttendanceVerification_AndDoesNotAutoComplete));

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

        // Geçmiş bir tarihte (10 Kasım 2026) planlanmış bir ders (FR-060, FR-061, BR-01)
        var pastStartUtc = new DateTime(2026, 11, 10, 14, 0, 0, DateTimeKind.Utc);
        var pastLesson = Lesson.CreateFromStudentSubject(enrollment, pastStartUtc, 60);
        context.Lessons.Add(pastLesson);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        var week = await service.GetWeeklyScheduleAsync("teacher-1", new DateOnly(2026, 11, 10));
        Assert.Equal(1, week.TotalLessonCount);
        Assert.Equal(0, week.CompletedLessonCount);
        Assert.Equal(1, week.PendingVerificationCount);

        var card = Assert.Single(week.Days[1].Lessons); // Salı
        Assert.Equal(LessonStatus.Scheduled, card.Status);
        Assert.Equal(LessonAttendanceStatus.Pending, card.AttendanceStatus);
        Assert.True(card.IsPendingAttendanceVerification);
        Assert.Equal("Yoklama Bekliyor", card.StatusBadgeText);

        var pendingCenter = await service.GetPendingAttendanceLessonsAsync("teacher-1");
        var pendingItem = Assert.Single(pendingCenter.Items);
        Assert.Equal(pastLesson.Id, pendingItem.LessonId);
        Assert.Equal("Ahmet Yılmaz", pendingItem.StudentFullName);
    }

    [Fact]
    public async Task RecordAttendanceAsync_SupportsAttendedAbsentAndCancelled_AndClearsPendingVerification()
    {
        await using var context = CreateDbContext(nameof(RecordAttendanceAsync_SupportsAttendedAbsentAndCancelled_AndClearsPendingVerification));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Demir" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.Add(ahmet);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var enrollment = new StudentSubject
        {
            StudentId = ahmet.Id,
            SubjectId = math.Id,
            HourlyRate = 1200m,
            DefaultDurationMinutes = 60,
            WeeklyTargetCount = 2,
            IsActive = true
        };
        context.StudentSubjects.Add(enrollment);
        await context.SaveChangesAsync();

        var lesson1 = Lesson.CreateFromStudentSubject(
            enrollment,
            new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc));
        var lesson2 = Lesson.CreateFromStudentSubject(
            enrollment,
            new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc));

        context.Lessons.AddRange(lesson1, lesson2);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // 1. Ders: Öğrenci Geldi (Attended) -> Status Completed olmalı, yoklama bekliyor listesinden düşmeli (FR-057, FR-058)
        var attendedResult = await service.RecordAttendanceAsync(
            "teacher-1",
            new RecordLessonAttendanceInputViewModel
            {
                LessonId = lesson1.Id,
                AttendanceStatus = LessonAttendanceStatus.Attended,
                AttendanceNote = "Türev uygulamaları tamamlandı"
            });

        Assert.True(attendedResult.Succeeded);
        Assert.Equal(LessonStatus.Completed, attendedResult.Lesson!.Status);
        Assert.Equal(LessonAttendanceStatus.Attended, attendedResult.Lesson.AttendanceStatus);
        Assert.NotNull(attendedResult.Lesson.AttendanceVerifiedAtUtc);
        Assert.Contains("Türev uygulamaları tamamlandı", attendedResult.Lesson.Notes);

        // 2. Ders: Öğrenci Gelmedi (Absent) -> AttendanceStatus Absent olmalı, yoklama bekliyor listesinden düşmeli (FR-058)
        var absentResult = await service.RecordAttendanceAsync(
            "teacher-1",
            new RecordLessonAttendanceInputViewModel
            {
                LessonId = lesson2.Id,
                AttendanceStatus = LessonAttendanceStatus.Absent,
                AttendanceNote = "Öğrenci mazeret bildirdi"
            });

        Assert.True(absentResult.Succeeded);
        Assert.Equal(LessonAttendanceStatus.Absent, absentResult.Lesson!.AttendanceStatus);
        Assert.NotNull(absentResult.Lesson.AttendanceVerifiedAtUtc);

        // Her iki ders için de yoklama doğrulandığından artık bekleyen yoklama kalmamalı
        var pendingAfter = await service.GetPendingAttendanceLessonsAsync("teacher-1");
        Assert.Empty(pendingAfter.Items);

        var week = await service.GetWeeklyScheduleAsync("teacher-1", new DateOnly(2026, 11, 9));
        Assert.Equal(0, week.PendingVerificationCount);
        Assert.Equal(1, week.CompletedLessonCount);
    }

    [Fact]
    public async Task BatchRecordAttendanceAsync_UpdatesMultiplePendingLessonsAtomically_AndEnforcesTeacherOwnership()
    {
        await using var context = CreateDbContext(nameof(BatchRecordAttendanceAsync_UpdatesMultiplePendingLessonsAtomically_AndEnforcesTeacherOwnership));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Yılmaz" };
        var otherStudent = new Student { TeacherId = "teacher-2", FirstName = "Zeynep", LastName = "Kaya" };
        var math1 = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };
        var math2 = new Subject { TeacherId = "teacher-2", Name = "Matematik", IsDefault = true };

        context.Students.AddRange(ahmet, otherStudent);
        context.Subjects.AddRange(math1, math2);
        await context.SaveChangesAsync();

        var enrollment1 = new StudentSubject { StudentId = ahmet.Id, SubjectId = math1.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 2 };
        var enrollment2 = new StudentSubject { StudentId = otherStudent.Id, SubjectId = math2.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        context.StudentSubjects.AddRange(enrollment1, enrollment2);
        await context.SaveChangesAsync();

        var l1 = Lesson.CreateFromStudentSubject(enrollment1, new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc));
        var l2 = Lesson.CreateFromStudentSubject(enrollment1, new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc));
        var otherLesson = Lesson.CreateFromStudentSubject(enrollment2, new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc));

        context.Lessons.AddRange(l1, l2, otherLesson);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // Başka öğretmenin dersini toplu yoklamaya dahil etmeye çalışırsa işlem reddedilmeli
        var unauthorizedBatch = await service.BatchRecordAttendanceAsync(
            "teacher-1",
            new BatchRecordAttendanceInputViewModel
            {
                LessonIds = new List<int> { l1.Id, otherLesson.Id },
                AttendanceStatus = LessonAttendanceStatus.Attended
            });
        Assert.False(unauthorizedBatch.Succeeded);

        // Kendi bekleyen derslerini topluca "Geldi" olarak onaylama (FR-063, FR-064)
        var validBatch = await service.BatchRecordAttendanceAsync(
            "teacher-1",
            new BatchRecordAttendanceInputViewModel
            {
                LessonIds = new List<int> { l1.Id, l2.Id },
                AttendanceStatus = LessonAttendanceStatus.Attended,
                AttendanceNote = "Gün sonu toplu onay"
            });

        Assert.True(validBatch.Succeeded);
        Assert.Equal(2, validBatch.CreatedCount);

        var updatedLessons = await context.Lessons
            .Where(l => l.Id == l1.Id || l.Id == l2.Id)
            .ToListAsync();

        Assert.All(updatedLessons, l =>
        {
            Assert.Equal(LessonStatus.Completed, l.Status);
            Assert.Equal(LessonAttendanceStatus.Attended, l.AttendanceStatus);
            Assert.NotNull(l.AttendanceVerifiedAtUtc);
            Assert.Contains("Gün sonu toplu onay", l.Notes);
        });
    }
}
