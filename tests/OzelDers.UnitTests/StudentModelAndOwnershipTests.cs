using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;

namespace OzelDers.UnitTests;

public class StudentModelAndOwnershipTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CurrentTeacherService CreateCurrentTeacherService(string teacherId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, teacherId) },
                    authenticationType: "TestAuth"))
        };

        return new CurrentTeacherService(new HttpContextAccessor { HttpContext = httpContext });
    }

    [Fact]
    public async Task Student_CanBeSavedWithOptionalContactInfoAndArchiveState()
    {
        await using var context = CreateDbContext(nameof(Student_CanBeSavedWithOptionalContactInfoAndArchiveState));

        var student = new Student
        {
            TeacherId = "teacher-1",
            FirstName = "Emre",
            LastName = "Aydın",
            PhoneNumber = null,
            Email = null,
            Notes = "Geometri temel konulardan başlanacak."
        };

        context.Students.Add(student);
        await context.SaveChangesAsync();

        Assert.True(student.Id > 0);
        Assert.Equal("Emre Aydın", student.FullName);
        Assert.False(student.IsArchived);
        Assert.NotEqual(default, student.CreatedAtUtc);
        Assert.Null(student.UpdatedAtUtc);

        // Arşivleme ve güncelleme zaman damgası kontrolü
        student.IsArchived = true;
        await context.SaveChangesAsync();

        Assert.True(student.IsArchived);
        Assert.NotNull(student.UpdatedAtUtc);
    }

    [Fact]
    public async Task StudentOwnership_IsolatesStudentsBetweenTeachersAndRejectsCrossTeacherAccess()
    {
        await using var context = CreateDbContext(nameof(StudentOwnership_IsolatesStudentsBetweenTeachersAndRejectsCrossTeacherAccess));

        var teacher1Student1 = new Student
        {
            TeacherId = "teacher-1",
            FirstName = "Zeynep",
            LastName = "Korkmaz",
            IsArchived = false
        };
        var teacher1ArchivedStudent = new Student
        {
            TeacherId = "teacher-1",
            FirstName = "Efe",
            LastName = "Polat",
            IsArchived = true
        };
        var teacher2Student = new Student
        {
            TeacherId = "teacher-2",
            FirstName = "Ceren",
            LastName = "Yavuz",
            IsArchived = false
        };

        context.Students.AddRange(teacher1Student1, teacher1ArchivedStudent, teacher2Student);
        await context.SaveChangesAsync();

        var teacher1Service = CreateCurrentTeacherService("teacher-1");

        var teacher1AllStudents = await teacher1Service
            .FilterByCurrentTeacher(context.Students)
            .ToListAsync();

        Assert.Equal(2, teacher1AllStudents.Count);
        Assert.All(teacher1AllStudents, s => Assert.Equal("teacher-1", s.TeacherId));

        var teacher1ActiveStudents = await teacher1Service
            .FilterByCurrentTeacher(context.Students)
            .Where(s => !s.IsArchived)
            .ToListAsync();

        var singleActive = Assert.Single(teacher1ActiveStudents);
        Assert.Equal("Zeynep Korkmaz", singleActive.FullName);

        // Diğer öğretmenin öğrencisine erişim reddedilmeli
        Assert.Throws<UnauthorizedAccessException>(() =>
            teacher1Service.EnsureOwnership(teacher2Student));
    }
}
