using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.StudentSubjects;

namespace OzelDers.UnitTests;

public class StudentSubjectServiceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task PrepareAssignViewModelAsync_PreselectsTeachersDefaultSubjectAutomatically()
    {
        await using var context = CreateDbContext(nameof(PrepareAssignViewModelAsync_PreselectsTeachersDefaultSubjectAutomatically));

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
            IsDefault = false,
            IsArchived = false
        };
        var geometry = new Subject
        {
            TeacherId = "teacher-1",
            Name = "Geometri",
            IsDefault = true,
            IsArchived = false
        };
        var archivedPhysics = new Subject
        {
            TeacherId = "teacher-1",
            Name = "Fizik",
            IsDefault = false,
            IsArchived = true
        };

        context.Students.Add(student);
        context.Subjects.AddRange(math, geometry, archivedPhysics);
        await context.SaveChangesAsync();

        var service = new StudentSubjectService(context);
        var viewModel = await service.PrepareAssignViewModelAsync("teacher-1", student.Id);

        Assert.NotNull(viewModel);
        Assert.Equal(geometry.Id, viewModel.SubjectId);
        Assert.Equal(geometry.Id, viewModel.DefaultSubjectId);
        Assert.Equal("Geometri", viewModel.DefaultSubjectName);
        Assert.Equal(2, viewModel.AvailableSubjects.Count);
        Assert.DoesNotContain(viewModel.AvailableSubjects, s => s.Name == "Fizik");
    }

    [Fact]
    public async Task AssignSubjectToStudentAsync_AllowsMultipleSubjectsWithDistinctRatesAndWeeklyTargets()
    {
        await using var context = CreateDbContext(nameof(AssignSubjectToStudentAsync_AllowsMultipleSubjectsWithDistinctRatesAndWeeklyTargets));

        var student = new Student
        {
            TeacherId = "teacher-1",
            FirstName = "Ahmet",
            LastName = "Demir"
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

        var service = new StudentSubjectService(context);

        // Ahmet: Matematik -> Haftada 2 oturum x 60 dk, saatlik 1.000 TL
        var mathAssign = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = student.Id,
                SubjectId = math.Id,
                HourlyRate = 1000m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 2
            });

        // Ahmet: Geometri -> Haftada 1 oturum x 60 dk, saatlik 1.200 TL
        var geoAssign = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = student.Id,
                SubjectId = geometry.Id,
                HourlyRate = 1200m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 1
            });

        Assert.True(mathAssign.Succeeded);
        Assert.True(geoAssign.Succeeded);

        var summary = await service.GetStudentSubjectsAsync("teacher-1", student.Id);
        Assert.NotNull(summary);
        Assert.Equal(2, summary.ActiveCount);
        Assert.Equal(3, summary.TotalWeeklySessions);
        Assert.Equal(180, summary.TotalWeeklyMinutes);
        Assert.Equal(3200m, summary.TotalEstimatedWeeklyFee);
    }

    [Fact]
    public async Task AssignSubjectToStudentAsync_PreservesDistinctionBetweenTwo60MinSessionsAndOne120MinSession()
    {
        await using var context = CreateDbContext(nameof(AssignSubjectToStudentAsync_PreservesDistinctionBetweenTwo60MinSessionsAndOne120MinSession));

        var student = new Student { TeacherId = "teacher-1", FirstName = "Elif", LastName = "Kaya" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };
        var physics = new Subject { TeacherId = "teacher-1", Name = "Fizik", IsDefault = false };

        context.Students.Add(student);
        context.Subjects.AddRange(math, physics);
        await context.SaveChangesAsync();

        var service = new StudentSubjectService(context);

        // Matematik: 2 oturum x 60 dk (Toplam 120 dk, 2 ayrı ders oturumu)
        var assignTwo60 = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = student.Id,
                SubjectId = math.Id,
                HourlyRate = 900m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 2
            });

        // Fizik: 1 oturum x 120 dk (Toplam 120 dk, 1 blok ders oturumu)
        var assignOne120 = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = student.Id,
                SubjectId = physics.Id,
                HourlyRate = 900m,
                DefaultDurationMinutes = 120,
                WeeklyTargetCount = 1
            });

        Assert.True(assignTwo60.Succeeded);
        Assert.True(assignOne120.Succeeded);
        Assert.NotNull(assignTwo60.StudentSubject);
        Assert.NotNull(assignOne120.StudentSubject);

        Assert.Equal(2, assignTwo60.StudentSubject.WeeklyTargetCount);
        Assert.Equal(60, assignTwo60.StudentSubject.DefaultDurationMinutes);
        Assert.Equal(120, assignTwo60.StudentSubject.WeeklyTotalMinutes);
        Assert.Equal(900m, assignTwo60.StudentSubject.EstimatedSessionFee);

        Assert.Equal(1, assignOne120.StudentSubject.WeeklyTargetCount);
        Assert.Equal(120, assignOne120.StudentSubject.DefaultDurationMinutes);
        Assert.Equal(120, assignOne120.StudentSubject.WeeklyTotalMinutes);
        Assert.Equal(1800m, assignOne120.StudentSubject.EstimatedSessionFee);
    }

    [Fact]
    public async Task AssignSubjectToStudentAsync_RejectsDuplicateActiveSubject_AndReactivatesInactiveAssignment()
    {
        await using var context = CreateDbContext(nameof(AssignSubjectToStudentAsync_RejectsDuplicateActiveSubject_AndReactivatesInactiveAssignment));

        var student = new Student { TeacherId = "teacher-1", FirstName = "Can", LastName = "Aydın" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };

        context.Students.Add(student);
        context.Subjects.Add(math);
        await context.SaveChangesAsync();

        var service = new StudentSubjectService(context);

        var first = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = student.Id,
                SubjectId = math.Id,
                HourlyRate = 800m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 1
            });
        Assert.True(first.Succeeded);
        Assert.NotNull(first.StudentSubject);

        // Aynı aktif ders tekrar atanamamalı
        var duplicate = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = student.Id,
                SubjectId = math.Id,
                HourlyRate = 950m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 2
            });
        Assert.False(duplicate.Succeeded);
        Assert.Contains(duplicate.Errors, e => e.Contains("zaten aktif olarak atanmış"));

        // Pasife alınıp yeniden atandığında mükerrer satır açmadan mevcut kaydı yeni ücretle aktifleştirmeli
        var deactivate = await service.SetActiveStatusAsync("teacher-1", first.StudentSubject.Id, isActive: false);
        Assert.True(deactivate.Succeeded);

        var reactivate = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = student.Id,
                SubjectId = math.Id,
                HourlyRate = 1100m,
                DefaultDurationMinutes = 90,
                WeeklyTargetCount = 2
            });

        Assert.True(reactivate.Succeeded);
        Assert.NotNull(reactivate.StudentSubject);
        Assert.Equal(first.StudentSubject.Id, reactivate.StudentSubject.Id);
        Assert.True(reactivate.StudentSubject.IsActive);
        Assert.Equal(1100m, reactivate.StudentSubject.HourlyRate);
        Assert.Equal(90, reactivate.StudentSubject.DefaultDurationMinutes);
        Assert.Equal(2, reactivate.StudentSubject.WeeklyTargetCount);
        Assert.Equal(1, await context.StudentSubjects.CountAsync(ss => ss.StudentId == student.Id));
    }

    [Fact]
    public async Task StudentSubjectOperations_EnforceStrictTeacherOwnershipAndRejectCrossTeacherAccess()
    {
        await using var context = CreateDbContext(nameof(StudentSubjectOperations_EnforceStrictTeacherOwnershipAndRejectCrossTeacherAccess));

        var t1Student = new Student { TeacherId = "teacher-1", FirstName = "Zeynep", LastName = "Koç" };
        var t2Student = new Student { TeacherId = "teacher-2", FirstName = "Burak", LastName = "Şahin" };
        var t1Subject = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };
        var t2Subject = new Subject { TeacherId = "teacher-2", Name = "Kimya", IsDefault = true };

        context.Students.AddRange(t1Student, t2Student);
        context.Subjects.AddRange(t1Subject, t2Subject);
        await context.SaveChangesAsync();

        var service = new StudentSubjectService(context);

        // Teacher-1 kendi öğrencisine Teacher-2'nin dersini atayamamalı
        var crossSubject = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = t1Student.Id,
                SubjectId = t2Subject.Id,
                HourlyRate = 1000m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 1
            });
        Assert.False(crossSubject.Succeeded);

        // Teacher-1, Teacher-2'nin öğrencisine kendi dersini atayamamalı
        var crossStudent = await service.AssignSubjectToStudentAsync(
            "teacher-1",
            new AssignSubjectViewModel
            {
                StudentId = t2Student.Id,
                SubjectId = t1Subject.Id,
                HourlyRate = 1000m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 1
            });
        Assert.False(crossStudent.Succeeded);

        // Teacher-2'nin geçerli kaydını Teacher-1 görüntüleyememeli veya değiştirememeli
        var validT2 = await service.AssignSubjectToStudentAsync(
            "teacher-2",
            new AssignSubjectViewModel
            {
                StudentId = t2Student.Id,
                SubjectId = t2Subject.Id,
                HourlyRate = 1000m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 1
            });
        Assert.True(validT2.Succeeded);
        Assert.NotNull(validT2.StudentSubject);

        Assert.Null(await service.GetStudentSubjectsAsync("teacher-1", t2Student.Id));
        Assert.Null(await service.GetForEditAsync("teacher-1", validT2.StudentSubject.Id));

        var badUpdate = await service.UpdateStudentSubjectAsync(
            "teacher-1",
            new EditStudentSubjectViewModel
            {
                Id = validT2.StudentSubject.Id,
                StudentId = t2Student.Id,
                SubjectId = t2Subject.Id,
                HourlyRate = 500m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 1
            });
        Assert.False(badUpdate.Succeeded);

        var badDeactivate = await service.SetActiveStatusAsync("teacher-1", validT2.StudentSubject.Id, isActive: false);
        Assert.False(badDeactivate.Succeeded);
    }
}
