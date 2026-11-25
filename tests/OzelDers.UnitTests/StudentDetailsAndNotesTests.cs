using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.UnitTests;

public class StudentDetailsAndNotesTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetStudentDetailsAsync_ReturnsStudentInfoNotesAndAssignedSubjectsSummary()
    {
        await using var context = CreateDbContext(nameof(GetStudentDetailsAsync_ReturnsStudentInfoNotesAndAssignedSubjectsSummary));

        var student = new Student
        {
            TeacherId = "teacher-1",
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            PhoneNumber = "0555 111 22 33",
            Email = "ahmet@example.com",
            Notes = "TYT ve AYT birlikte yürütülüyor."
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

        context.StudentSubjects.AddRange(
            new StudentSubject
            {
                StudentId = student.Id,
                SubjectId = math.Id,
                HourlyRate = 1000m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 2,
                IsActive = true
            },
            new StudentSubject
            {
                StudentId = student.Id,
                SubjectId = geometry.Id,
                HourlyRate = 1200m,
                DefaultDurationMinutes = 60,
                WeeklyTargetCount = 1,
                IsActive = true
            });
        await context.SaveChangesAsync();

        var service = new StudentService(context);
        var details = await service.GetStudentDetailsAsync("teacher-1", student.Id);

        Assert.NotNull(details);
        Assert.Equal("Ahmet Yılmaz", details.FullName);
        Assert.Equal("0555 111 22 33", details.PhoneNumber);
        Assert.Equal("TYT ve AYT birlikte yürütülüyor.", details.Notes);
        Assert.Equal("Matematik", details.TeacherDefaultSubjectName);
        Assert.Equal(2, details.ActiveSubjectCount);
        Assert.Equal(3, details.TotalWeeklySessions);
        Assert.Equal(180, details.TotalWeeklyMinutes);
        Assert.Equal(3200m, details.TotalEstimatedWeeklyFee);
        Assert.Equal(2, details.AssignedSubjects.Count);
    }

    [Fact]
    public async Task UpdateStudentNotesAsync_UpdatesAndTrimsNotes_AndEnforcesTeacherOwnership()
    {
        await using var context = CreateDbContext(nameof(UpdateStudentNotesAsync_UpdatesAndTrimsNotes_AndEnforcesTeacherOwnership));

        var student = new Student
        {
            TeacherId = "teacher-1",
            FirstName = "Zeynep",
            LastName = "Kaya",
            Notes = "İlk not"
        };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var service = new StudentService(context);

        var updated = await service.UpdateStudentNotesAsync(
            "teacher-1",
            student.Id,
            "  Güncellenmiş haftalık ödev ve deneme takibi notu.  ");

        Assert.True(updated.Succeeded);
        Assert.NotNull(updated.Student);
        Assert.Equal("Güncellenmiş haftalık ödev ve deneme takibi notu.", updated.Student.Notes);

        // Başka bir öğretmen bu öğrencinin detayını görememeli ve notunu değiştirememeli
        Assert.Null(await service.GetStudentDetailsAsync("teacher-2", student.Id));

        var crossTeacherUpdate = await service.UpdateStudentNotesAsync("teacher-2", student.Id, "Yetkisiz not");
        Assert.False(crossTeacherUpdate.Succeeded);
    }

    [Fact]
    public async Task GetStudentsForTeacherAsync_FiltersByAssignedSubjectAndPopulatesActiveSubjectBadges()
    {
        await using var context = CreateDbContext(nameof(GetStudentsForTeacherAsync_FiltersByAssignedSubjectAndPopulatesActiveSubjectBadges));

        var s1 = new Student { TeacherId = "teacher-1", FirstName = "Ahmet", LastName = "Demir" };
        var s2 = new Student { TeacherId = "teacher-1", FirstName = "Elif", LastName = "Şahin" };
        var math = new Subject { TeacherId = "teacher-1", Name = "Matematik", IsDefault = true };
        var geo = new Subject { TeacherId = "teacher-1", Name = "Geometri", IsDefault = false };

        context.Students.AddRange(s1, s2);
        context.Subjects.AddRange(math, geo);
        await context.SaveChangesAsync();

        context.StudentSubjects.AddRange(
            new StudentSubject { StudentId = s1.Id, SubjectId = math.Id, HourlyRate = 1000m, DefaultDurationMinutes = 60, WeeklyTargetCount = 2, IsActive = true },
            new StudentSubject { StudentId = s1.Id, SubjectId = geo.Id, HourlyRate = 1200m, DefaultDurationMinutes = 60, WeeklyTargetCount = 1, IsActive = true },
            new StudentSubject { StudentId = s2.Id, SubjectId = math.Id, HourlyRate = 950m, DefaultDurationMinutes = 90, WeeklyTargetCount = 1, IsActive = true });
        await context.SaveChangesAsync();

        var service = new StudentService(context);

        // Geometri filtresi yalnızca Ahmet'i döndürmeli
        var geoFiltered = await service.GetStudentsForTeacherAsync(
            "teacher-1",
            searchTerm: null,
            statusFilter: StudentStatusFilter.Active,
            subjectId: geo.Id);

        var onlyAhmet = Assert.Single(geoFiltered.Students);
        Assert.Equal("Ahmet Demir", onlyAhmet.FullName);
        Assert.Equal(2, onlyAhmet.ActiveSubjects.Count);
    }
}
