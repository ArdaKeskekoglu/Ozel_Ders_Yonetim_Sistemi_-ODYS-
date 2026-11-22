using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;

namespace OzelDers.UnitTests;

public class TeacherIdentityTests
{
    private static ServiceProvider CreateIdentityServiceProvider(string databaseName)
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

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CreateAsync_StoresTeacherWithHashedPasswordAndDefaultSettings()
    {
        using var provider = CreateIdentityServiceProvider(nameof(CreateAsync_StoresTeacherWithHashedPasswordAndDefaultSettings));
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher<ApplicationUser>>();

        var teacher = new ApplicationUser
        {
            UserName = "ogretmen@odys.local",
            Email = "ogretmen@odys.local",
            FirstName = "Ayşe",
            LastName = "Yılmaz"
        };

        const string plainPassword = "GuvenliSifre123";
        var result = await userManager.CreateAsync(teacher, plainPassword);

        Assert.True(result.Succeeded);

        var savedTeacher = await userManager.FindByEmailAsync("ogretmen@odys.local");
        Assert.NotNull(savedTeacher);
        Assert.Equal("Ayşe Yılmaz", savedTeacher.FullName);
        Assert.Equal("Ayşe Yılmaz", savedTeacher.DisplayName);
        Assert.Equal("Turkey Standard Time", savedTeacher.TimeZoneId);
        Assert.Equal("TRY", savedTeacher.CurrencyCode);

        // NFR-005: Şifre düz metin olarak saklanmamalı, güvenli hash ile doğrulanmalı
        Assert.NotNull(savedTeacher.PasswordHash);
        Assert.NotEqual(plainPassword, savedTeacher.PasswordHash);
        var verification = passwordHasher.VerifyHashedPassword(savedTeacher, savedTeacher.PasswordHash, plainPassword);
        Assert.NotEqual(PasswordVerificationResult.Failed, verification);
    }

    [Fact]
    public async Task CreateAsync_RejectsWeakPasswordAndDuplicateEmail()
    {
        using var provider = CreateIdentityServiceProvider(nameof(CreateAsync_RejectsWeakPasswordAndDuplicateEmail));
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var weakUser = new ApplicationUser
        {
            UserName = "zayif@odys.local",
            Email = "zayif@odys.local",
            FirstName = "Mehmet",
            LastName = "Kaya"
        };

        var weakResult = await userManager.CreateAsync(weakUser, "12345");
        Assert.False(weakResult.Succeeded);

        var firstTeacher = new ApplicationUser
        {
            UserName = "tekil@odys.local",
            Email = "tekil@odys.local",
            FirstName = "Zeynep",
            LastName = "Demir"
        };
        var firstCreate = await userManager.CreateAsync(firstTeacher, "GuvenliSifre123");
        Assert.True(firstCreate.Succeeded);

        var duplicateTeacher = new ApplicationUser
        {
            UserName = "tekil2@odys.local",
            Email = "tekil@odys.local",
            FirstName = "Ali",
            LastName = "Demir"
        };
        var duplicateCreate = await userManager.CreateAsync(duplicateTeacher, "GuvenliSifre123");
        Assert.False(duplicateCreate.Succeeded);
    }
}
