using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.UnitTests;

public class TeacherRegistrationTests
{
    private static ServiceProvider CreateServiceProvider(string databaseName)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<ITeacherAccountService, TeacherAccountService>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RegisterTeacherAsync_WithValidInput_CreatesTeacherWithNormalizedFields()
    {
        using var provider = CreateServiceProvider(nameof(RegisterTeacherAsync_WithValidInput_CreatesTeacherWithNormalizedFields));
        var service = provider.GetRequiredService<ITeacherAccountService>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var model = new RegisterViewModel
        {
            FirstName = "  Elif ",
            LastName = " Karaca  ",
            Email = "  elif.karaca@odys.local ",
            PhoneNumber = " 0555 000 00 00 ",
            Password = "GuvenliSifre123",
            ConfirmPassword = "GuvenliSifre123"
        };

        var result = await service.RegisterTeacherAsync(model);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.Empty(result.Errors);
        Assert.Equal("Elif", result.User.FirstName);
        Assert.Equal("Karaca", result.User.LastName);
        Assert.Equal("Elif Karaca", result.User.DisplayName);
        Assert.Equal("0555 000 00 00", result.User.PhoneNumber);

        var persisted = await userManager.FindByEmailAsync("elif.karaca@odys.local");
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task RegisterTeacherAsync_WithDuplicateEmail_ReturnsSafeTurkishErrorMessage()
    {
        using var provider = CreateServiceProvider(nameof(RegisterTeacherAsync_WithDuplicateEmail_ReturnsSafeTurkishErrorMessage));
        var service = provider.GetRequiredService<ITeacherAccountService>();

        var model = new RegisterViewModel
        {
            FirstName = "Burak",
            LastName = "Şahin",
            Email = "burak@odys.local",
            Password = "GuvenliSifre123",
            ConfirmPassword = "GuvenliSifre123"
        };

        var first = await service.RegisterTeacherAsync(model);
        Assert.True(first.Succeeded);

        var second = await service.RegisterTeacherAsync(model);
        Assert.False(second.Succeeded);
        Assert.Null(second.User);
        var error = Assert.Single(second.Errors);
        Assert.Equal("Bu e-posta adresi ile kayıtlı bir öğretmen hesabı zaten mevcut.", error);
    }

    [Fact]
    public async Task RegisterTeacherAsync_WithWeakPassword_ReturnsTurkishPasswordPolicyErrors()
    {
        using var provider = CreateServiceProvider(nameof(RegisterTeacherAsync_WithWeakPassword_ReturnsTurkishPasswordPolicyErrors));
        var service = provider.GetRequiredService<ITeacherAccountService>();

        var model = new RegisterViewModel
        {
            FirstName = "Can",
            LastName = "Yıldız",
            Email = "can@odys.local",
            Password = "kisa",
            ConfirmPassword = "kisa"
        };

        var result = await service.RegisterTeacherAsync(model);

        Assert.False(result.Succeeded);
        Assert.Contains("Şifre en az 8 karakter uzunluğunda olmalıdır.", result.Errors);
        Assert.Contains("Şifre en az bir rakam (0-9) içermelidir.", result.Errors);
        Assert.Contains("Şifre en az bir büyük harf (A-Z) içermelidir.", result.Errors);
    }

    [Fact]
    public void RegisterViewModel_Validation_FailsOnMismatchedPasswordsAndInvalidEmail()
    {
        var model = new RegisterViewModel
        {
            FirstName = "A",
            LastName = "Yılmaz",
            Email = "gecersiz-eposta",
            Password = "GuvenliSifre123",
            ConfirmPassword = "FarkliSifre123"
        };

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(RegisterViewModel.FirstName)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(RegisterViewModel.Email)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }
}
