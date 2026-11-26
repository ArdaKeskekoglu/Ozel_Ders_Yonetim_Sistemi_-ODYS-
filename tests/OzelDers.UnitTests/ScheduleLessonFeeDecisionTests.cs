using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Schedule;

namespace OzelDers.UnitTests;

public class ScheduleLessonFeeDecisionTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task MarkingLessonAbsent_WithoutExplicitDecision_DoesNotAutoCreateCharge_AndRemainsPendingFeeDecision()
    {
        await using var context = CreateDbContext(nameof(MarkingLessonAbsent_WithoutExplicitDecision_DoesNotAutoCreateCharge_AndRemainsPendingFeeDecision));

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

        var lesson = Lesson.CreateFromStudentSubject(
            enrollment,
            new DateTime(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc));
        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // Öğrenci gelmedi (Absent) işaretlendiğinde otomatik borç (Charge) oluşmamalı; Pending kalmalı (FR-065, FR-066, BR-02, BR-03)
        var absentResult = await service.RecordAttendanceAsync(
            "teacher-1",
            new RecordLessonAttendanceInputViewModel
            {
                LessonId = lesson.Id,
                AttendanceStatus = LessonAttendanceStatus.Absent
            });

        Assert.True(absentResult.Succeeded);
        Assert.Equal(LessonAttendanceStatus.Absent, absentResult.Lesson!.AttendanceStatus);
        Assert.Equal(LessonFeeDecision.Pending, absentResult.Lesson.FeeDecision);
        Assert.Null(absentResult.Lesson.FeeDecisionAtUtc);

        var week = await service.GetWeeklyScheduleAsync("teacher-1", new DateOnly(2026, 11, 10));
        Assert.Equal(0, week.PendingVerificationCount);
        Assert.Equal(1, week.PendingFeeDecisionCount);

        var card = Assert.Single(week.Days[1].Lessons);
        Assert.True(card.IsPendingFeeDecision);
        Assert.Equal("Gelmedi • Ücret Kararı Bekliyor", card.StatusBadgeText);

        var pendingCenter = await service.GetPendingAttendanceLessonsAsync("teacher-1");
        Assert.Empty(pendingCenter.Items);
        var pendingFeeItem = Assert.Single(pendingCenter.PendingFeeDecisionItems);
        Assert.Equal(lesson.Id, pendingFeeItem.LessonId);
    }

    [Fact]
    public async Task SetLessonFeeDecisionAsync_AllowsTeacherToChooseWaiveOrChargePerLesson_AndSupportsControlledCorrectionWithAuditTrail()
    {
        await using var context = CreateDbContext(nameof(SetLessonFeeDecisionAsync_AllowsTeacherToChooseWaiveOrChargePerLesson_AndSupportsControlledCorrectionWithAuditTrail));

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

        var lesson1 = Lesson.CreateFromStudentSubject(enrollment, new DateTime(2026, 11, 9, 14, 0, 0, DateTimeKind.Utc));
        var lesson2 = Lesson.CreateFromStudentSubject(enrollment, new DateTime(2026, 11, 11, 14, 0, 0, DateTimeKind.Utc));
        context.Lessons.AddRange(lesson1, lesson2);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        await service.RecordAttendanceAsync("teacher-1", new RecordLessonAttendanceInputViewModel
        {
            LessonId = lesson1.Id,
            AttendanceStatus = LessonAttendanceStatus.Absent
        });
        await service.RecordAttendanceAsync("teacher-1", new RecordLessonAttendanceInputViewModel
        {
            LessonId = lesson2.Id,
            AttendanceStatus = LessonAttendanceStatus.Absent
        });

        // 1. Ders için öğretmen "Ücret Alınmayacak / Muaf (Waive)" seçiyor (FR-065)
        var waiveResult = await service.SetLessonFeeDecisionAsync(
            "teacher-1",
            new SetLessonFeeDecisionInputViewModel
            {
                LessonId = lesson1.Id,
                FeeDecision = LessonFeeDecision.Waive,
                Reason = "Veli 1 gün önceden sağlık mazereti bildirdi"
            });

        Assert.True(waiveResult.Succeeded);
        Assert.Equal(LessonFeeDecision.Waive, waiveResult.Lesson!.FeeDecision);
        Assert.NotNull(waiveResult.Lesson.FeeDecisionAtUtc);
        Assert.Equal("Veli 1 gün önceden sağlık mazereti bildirdi", waiveResult.Lesson.FeeDecisionReason);
        Assert.Contains("[Ücret Kararı: Ücret Alınmayacak / Muaf", waiveResult.Lesson.Notes);

        // 2. Ders için öğretmen "Ücret Alınacak (Charge)" seçiyor (FR-067)
        var chargeResult = await service.SetLessonFeeDecisionAsync(
            "teacher-1",
            new SetLessonFeeDecisionInputViewModel
            {
                LessonId = lesson2.Id,
                FeeDecision = LessonFeeDecision.Charge,
                Reason = "Ders saatinde habersiz gelinmedi"
            });

        Assert.True(chargeResult.Succeeded);
        Assert.Equal(LessonFeeDecision.Charge, chargeResult.Lesson!.FeeDecision);
        Assert.Contains("[Ücret Kararı: Ücret Alınacak", chargeResult.Lesson.Notes);

        // Kontrollü düzeltme (FR-068): Öğretmen 2. dersin ücret kararını sonradan Waive olarak düzeltiyor ve denetim izi korunuyor
        var correctionResult = await service.SetLessonFeeDecisionAsync(
            "teacher-1",
            new SetLessonFeeDecisionInputViewModel
            {
                LessonId = lesson2.Id,
                FeeDecision = LessonFeeDecision.Waive,
                Reason = "Telafi dersi yapılmasına karar verildi"
            });

        Assert.True(correctionResult.Succeeded);
        Assert.Equal(LessonFeeDecision.Waive, correctionResult.Lesson!.FeeDecision);
        Assert.Equal("Telafi dersi yapılmasına karar verildi", correctionResult.Lesson.FeeDecisionReason);
        Assert.Contains("[Ücret Kararı Düzeltildi: Ücret Alınmayacak / Muaf (0,00 ₺) — Telafi dersi yapılmasına karar verildi]", correctionResult.Lesson.Notes);

        // Artık ücret kararı bekleyen devamsız ders kalmamalı
        var pendingCenterAfter = await service.GetPendingAttendanceLessonsAsync("teacher-1");
        Assert.Empty(pendingCenterAfter.PendingFeeDecisionItems);
    }

    [Fact]
    public async Task SetLessonFeeDecisionAsync_RejectsUnverifiedOrAttendedWaiveAndUnauthorizedAccess()
    {
        await using var context = CreateDbContext(nameof(SetLessonFeeDecisionAsync_RejectsUnverifiedOrAttendedWaiveAndUnauthorizedAccess));

        var ahmet = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Yılmaz" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.Add(ahmet);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var enrollment = new StudentSubject { StudentId = ahmet.Id, SubjectId = math.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1 };
        context.StudentSubjects.Add(enrollment);
        await context.SaveChangesAsync();

        var lesson = Lesson.CreateFromStudentSubject(enrollment, new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc));
        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, TimeZoneInfo.Utc);

        // Henüz yoklaması hiç girilmemiş (Pending) derse doğrudan ücret kararı verilemez
        var beforeAttendance = await service.SetLessonFeeDecisionAsync(
            "teacher-1",
            new SetLessonFeeDecisionInputViewModel
            {
                LessonId = lesson.Id,
                FeeDecision = LessonFeeDecision.Waive
            });
        Assert.False(beforeAttendance.Succeeded);

        // Öğrenci derse geldiğinde (Attended) otomatik olarak Charge atanır ve devamsızlık muafiyeti (Waive) verilemez
        var attended = await service.RecordAttendanceAsync(
            "teacher-1",
            new RecordLessonAttendanceInputViewModel
            {
                LessonId = lesson.Id,
                AttendanceStatus = LessonAttendanceStatus.Attended
            });
        Assert.True(attended.Succeeded);
        Assert.Equal(LessonFeeDecision.Charge, attended.Lesson!.FeeDecision);

        var waiveAttended = await service.SetLessonFeeDecisionAsync(
            "teacher-1",
            new SetLessonFeeDecisionInputViewModel
            {
                LessonId = lesson.Id,
                FeeDecision = LessonFeeDecision.Waive
            });
        Assert.False(waiveAttended.Succeeded);

        // Başka öğretmen ücret kararı değiştiremez
        var otherTeacherAttempt = await service.SetLessonFeeDecisionAsync(
            "teacher-2",
            new SetLessonFeeDecisionInputViewModel
            {
                LessonId = lesson.Id,
                FeeDecision = LessonFeeDecision.Charge
            });
        Assert.False(otherTeacherAttempt.Succeeded);
    }
}
