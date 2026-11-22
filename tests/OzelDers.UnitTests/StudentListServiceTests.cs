using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.UnitTests;

public class StudentListServiceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetStudentsForTeacherAsync_WhenNoStudentsExist_ReturnsEmptyStateViewModel()
    {
        await using var context = CreateDbContext(nameof(GetStudentsForTeacherAsync_WhenNoStudentsExist_ReturnsEmptyStateViewModel));
        var service = new StudentService(context);

        var result = await service.GetStudentsForTeacherAsync("teacher-empty");

        Assert.False(result.HasAnyStudents);
        Assert.False(result.HasFilteredResults);
        Assert.Equal(0, result.ActiveCount);
        Assert.Equal(0, result.ArchivedCount);
        Assert.Empty(result.Students);
    }

    [Fact]
    public async Task GetStudentsForTeacherAsync_ReturnsOnlyOwnStudentsAndRespectsStatusAndSearchFilters()
    {
        await using var context = CreateDbContext(nameof(GetStudentsForTeacherAsync_ReturnsOnlyOwnStudentsAndRespectsStatusAndSearchFilters));

        context.Students.AddRange(
            new Student
            {
                TeacherId = "teacher-1",
                FirstName = "Ahmet",
                LastName = "Demir",
                PhoneNumber = "0500 111 22 33",
                IsArchived = false
            },
            new Student
            {
                TeacherId = "teacher-1",
                FirstName = "Buse",
                LastName = "Yılmaz",
                IsArchived = false
            },
            new Student
            {
                TeacherId = "teacher-1",
                FirstName = "Cem",
                LastName = "Kaya",
                IsArchived = true
            },
            new Student
            {
                TeacherId = "teacher-2",
                FirstName = "Ahmet",
                LastName = "BaşkaÖğretmen",
                IsArchived = false
            });
        await context.SaveChangesAsync();

        var service = new StudentService(context);

        // 1) Varsayılan filtre: Yalnızca teacher-1'in aktif öğrencileri
        var activeList = await service.GetStudentsForTeacherAsync("teacher-1");
        Assert.True(activeList.HasAnyStudents);
        Assert.Equal(2, activeList.ActiveCount);
        Assert.Equal(1, activeList.ArchivedCount);
        Assert.Equal(3, activeList.TotalCount);
        Assert.Equal(new[] { "Ahmet Demir", "Buse Yılmaz" }, activeList.Students.Select(s => s.FullName).ToArray());

        // 2) Arşiv filtresi: Yalnızca teacher-1'in arşivli öğrencileri
        var archivedList = await service.GetStudentsForTeacherAsync("teacher-1", statusFilter: StudentStatusFilter.Archived);
        var singleArchived = Assert.Single(archivedList.Students);
        Assert.Equal("Cem Kaya", singleArchived.FullName);

        // 3) İsim araması: "Ahmet" arandığında teacher-2'nin "Ahmet BaşkaÖğretmen" kaydı kesinlikle gelmemeli
        var searchList = await service.GetStudentsForTeacherAsync("teacher-1", searchTerm: "Ahmet", statusFilter: StudentStatusFilter.All);
        var singleFound = Assert.Single(searchList.Students);
        Assert.Equal("Ahmet Demir", singleFound.FullName);
    }
}
