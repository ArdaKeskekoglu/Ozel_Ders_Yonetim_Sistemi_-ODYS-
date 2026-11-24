using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.UnitTests;

public class TeacherProfileAndPasswordTests
{
    private static ServiceProvider CreateServiceProvider(string databaseName)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDataProtection();
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
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ITeacherAccountService, TeacherAccountService>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesAuthenticatedTeacherProfileFieldsAndReturnsDefaultSubject()
    {
        using var provider = CreateServiceProvider(nameof(UpdateProfileAsync_UpdatesAuthenticatedTeacherProfileFieldsAndReturnsDefaultSubject));
        var service = provider.GetRequiredService<ITeacherAccountService>();

        var reg = await service.RegisterTeacherAsync(new RegisterViewModel
        {
            FirstName = "Hakan",
            LastName = "Çelik",
            Email = "hakan@odys.local",
            PhoneNumber = "0500 111 22 33",
            SelectedSubjects = new List<string> { "Matematik", "Geometri" },
            Password = "GuvenliSifre123",
            ConfirmPassword = "GuvenliSifre123"
        });
        Assert.True(reg.Succeeded);
        Assert.NotNull(reg.User);

        var updateResult = await service.UpdateProfileAsync(
            reg.User.Id,
            new ProfileViewModel
            {
                FirstName = "Hakan",
                LastName = "Çelik",
                DisplayName = "Hakan Hoca (Matematik)",
                PhoneNumber = "0532 999 88 77",
                TimeZoneId = "UTC"
            });

        Assert.True(updateResult.Succeeded);

        var updatedProfile = await service.GetProfileAsync(reg.User.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal("Hakan Hoca (Matematik)", updatedProfile.DisplayName);
        Assert.Equal("0532 999 88 77", updatedProfile.PhoneNumber);
        Assert.Equal("UTC", updatedProfile.TimeZoneId);
        Assert.Equal("hakan@odys.local", updatedProfile.Email);
        Assert.Equal("Matematik", updatedProfile.DefaultSubjectName);
        Assert.Equal(new[] { "Matematik", "Geometri" }, updatedProfile.ActiveSubjectNames);
    }

    [Fact]
    public async Task LegacyTeacherWithoutSubjects_CanLogInAndDefineInitialDefaultSubjectViaProfile()
    {
        using var provider = CreateServiceProvider(nameof(LegacyTeacherWithoutSubjects_CanLogInAndDefineInitialDefaultSubjectViaProfile));
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = provider.GetRequiredService<ITeacherAccountService>();
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();

        // Faz 7 güncellemesinden önce kayıt olmuş, henüz ders türü bulunmayan mevcut öğretmen hesabı
        var legacyTeacher = new ApplicationUser
        {
            UserName = "eski.ogretmen@odys.local",
            Email = "eski.ogretmen@odys.local",
            FirstName = "Kemal",
            LastName = "Uçar"
        };
        var createResult = await userManager.CreateAsync(legacyTeacher, "GuvenliSifre123");
        Assert.True(createResult.Succeeded);

        // 1) Mevcut öğretmen sorunsuz giriş yapabilmeli
        var loginResult = await service.ValidateTeacherCredentialsAsync(new LoginViewModel
        {
            Email = "eski.ogretmen@odys.local",
            Password = "GuvenliSifre123"
        });
        Assert.True(loginResult.Succeeded);

        // 2) Profilini açtığında rastgele ders atanmamış olmalı
        var initialProfile = await service.GetProfileAsync(legacyTeacher.Id);
        Assert.NotNull(initialProfile);
        Assert.Null(initialProfile.DefaultSubjectName);
        Assert.False(initialProfile.HasAnyActiveSubject);

        // 3) Profil üzerinden ilk (varsayılan) dersini tanımlayabilmeli
        var updateResult = await service.UpdateProfileAsync(
            legacyTeacher.Id,
            new ProfileViewModel
            {
                FirstName = "Kemal",
                LastName = "Uçar",
                TimeZoneId = "Turkey Standard Time",
                InitialSubjectName = " Fizik "
            });
        Assert.True(updateResult.Succeeded);

        var updatedProfile = await service.GetProfileAsync(legacyTeacher.Id);
        Assert.NotNull(updatedProfile);
        Assert.True(updatedProfile.HasAnyActiveSubject);
        Assert.Equal("Fizik", updatedProfile.DefaultSubjectName);

        var savedSubject = await dbContext.Subjects.SingleAsync(s => s.TeacherId == legacyTeacher.Id);
        Assert.Equal("Fizik", savedSubject.Name);
        Assert.True(savedSubject.IsDefault);
        Assert.False(savedSubject.IsArchived);
    }

    [Fact]
    public async Task ChangePasswordAsync_VerifiesCurrentPasswordAndUpdatesToNewPassword()
    {
        using var provider = CreateServiceProvider(nameof(ChangePasswordAsync_VerifiesCurrentPasswordAndUpdatesToNewPassword));
        var service = provider.GetRequiredService<ITeacherAccountService>();

        var reg = await service.RegisterTeacherAsync(new RegisterViewModel
        {
            FirstName = "Merve",
            LastName = "Aslan",
            Email = "merve@odys.local",
            SelectedSubjects = new List<string> { "Biyoloji" },
            Password = "EskiSifre123",
            ConfirmPassword = "EskiSifre123"
        });
        Assert.NotNull(reg.User);

        var wrongCurrent = await service.ChangePasswordAsync(
            reg.User.Id,
            new ChangePasswordViewModel
            {
                CurrentPassword = "YanlisMevcutSifre123",
                NewPassword = "YeniSifre456",
                ConfirmNewPassword = "YeniSifre456"
            });
        Assert.False(wrongCurrent.Succeeded);
        Assert.Contains("Girdiğiniz mevcut şifre hatalı.", wrongCurrent.Errors);

        var validChange = await service.ChangePasswordAsync(
            reg.User.Id,
            new ChangePasswordViewModel
            {
                CurrentPassword = "EskiSifre123",
                NewPassword = "YeniSifre456",
                ConfirmNewPassword = "YeniSifre456"
            });
        Assert.True(validChange.Succeeded);

        var loginWithNew = await service.ValidateTeacherCredentialsAsync(new LoginViewModel
        {
            Email = "merve@odys.local",
            Password = "YeniSifre456"
        });
        Assert.True(loginWithNew.Succeeded);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ResetsPasswordAndRejectsInvalidToken()
    {
        using var provider = CreateServiceProvider(nameof(ResetPasswordAsync_WithValidToken_ResetsPasswordAndRejectsInvalidToken));
        var service = provider.GetRequiredService<ITeacherAccountService>();

        var reg = await service.RegisterTeacherAsync(new RegisterViewModel
        {
            FirstName = "Kaan",
            LastName = "Öztürk",
            Email = "kaan@odys.local",
            SelectedSubjects = new List<string> { "Kimya" },
            Password = "IlkSifre123",
            ConfirmPassword = "IlkSifre123"
        });
        Assert.True(reg.Succeeded);

        var invalidReset = await service.ResetPasswordAsync(new ResetPasswordViewModel
        {
            Email = "kaan@odys.local",
            Token = "gecersiz-token-degeri",
            NewPassword = "SifirlananSifre789",
            ConfirmNewPassword = "SifirlananSifre789"
        });
        Assert.False(invalidReset.Succeeded);
        Assert.Contains("Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.", invalidReset.Errors);

        var validToken = await service.GeneratePasswordResetTokenAsync("kaan@odys.local");
        Assert.False(string.IsNullOrWhiteSpace(validToken));

        var validReset = await service.ResetPasswordAsync(new ResetPasswordViewModel
        {
            Email = "kaan@odys.local",
            Token = validToken!,
            NewPassword = "SifirlananSifre789",
            ConfirmNewPassword = "SifirlananSifre789"
        });
        Assert.True(validReset.Succeeded);

        var loginAfterReset = await service.ValidateTeacherCredentialsAsync(new LoginViewModel
        {
            Email = "kaan@odys.local",
            Password = "SifirlananSifre789"
        });
        Assert.True(loginAfterReset.Succeeded);
    }
}
