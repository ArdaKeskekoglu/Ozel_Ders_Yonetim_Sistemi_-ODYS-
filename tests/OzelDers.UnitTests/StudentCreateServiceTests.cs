using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.UnitTests;

public class StudentCreateServiceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateStudentAsync_WithValidInput_PersistsStudentOwnedByTeacher()
    {
        await using var context = CreateDbContext(nameof(CreateStudentAsync_WithValidInput_PersistsStudentOwnedByTeacher));
        var service = new StudentService(context);

        var model = new CreateStudentViewModel
        {
            FirstName = "  Kerem ",
            LastName = " Aksoy  ",
            PhoneNumber = " 0532 100 20 30 ",
            Email = " kerem.aksoy@odys.local ",
            Notes = " TYT Matematik kampı "
        };

        var result = await service.CreateStudentAsync("teacher-10", model);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Student);
        Assert.Empty(result.Errors);
        Assert.Equal("teacher-10", result.Student.TeacherId);
        Assert.Equal("Kerem", result.Student.FirstName);
        Assert.Equal("Aksoy", result.Student.LastName);
        Assert.Equal("Kerem Aksoy", result.Student.FullName);
        Assert.Equal("0532 100 20 30", result.Student.PhoneNumber);
        Assert.Equal("kerem.aksoy@odys.local", result.Student.Email);
        Assert.Equal("TYT Matematik kampı", result.Student.Notes);
        Assert.False(result.Student.IsArchived);

        var persisted = await context.Students.SingleAsync();
        Assert.Equal("teacher-10", persisted.TeacherId);
    }

    [Fact]
    public async Task CreateStudentAsync_WithOptionalFieldsBlank_SavesNullForOptionalFields()
    {
        await using var context = CreateDbContext(nameof(CreateStudentAsync_WithOptionalFieldsBlank_SavesNullForOptionalFields));
        var service = new StudentService(context);

        var model = new CreateStudentViewModel
        {
            FirstName = "Defne",
            LastName = "Şen",
            PhoneNumber = "   ",
            Email = "",
            Notes = null
        };

        var result = await service.CreateStudentAsync("teacher-10", model);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Student);
        Assert.Null(result.Student.PhoneNumber);
        Assert.Null(result.Student.Email);
        Assert.Null(result.Student.Notes);
    }

    [Fact]
    public async Task CreateStudentAsync_WithInvalidShortNames_ReturnsValidationErrorsAndDoesNotPersist()
    {
        await using var context = CreateDbContext(nameof(CreateStudentAsync_WithInvalidShortNames_ReturnsValidationErrorsAndDoesNotPersist));
        var service = new StudentService(context);

        var model = new CreateStudentViewModel
        {
            FirstName = "A",
            LastName = " "
        };

        var result = await service.CreateStudentAsync("teacher-10", model);

        Assert.False(result.Succeeded);
        Assert.Null(result.Student);
        Assert.Contains("Öğrenci adı en az 2 karakter olmalıdır.", result.Errors);
        Assert.Contains("Öğrenci soyadı en az 2 karakter olmalıdır.", result.Errors);
        Assert.Equal(0, await context.Students.CountAsync());
    }

    [Fact]
    public void CreateStudentViewModel_DataAnnotations_ValidateRequiredNamesAndOptionalFormats()
    {
        var invalidModel = new CreateStudentViewModel
        {
            FirstName = "",
            LastName = "Y",
            Email = "gecersiz-eposta"
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(invalidModel, new ValidationContext(invalidModel), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateStudentViewModel.FirstName)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateStudentViewModel.LastName)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateStudentViewModel.Email)));
    }
}
