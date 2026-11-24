using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Common;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.UnitTests;

public class TeacherAuthAndOwnershipTests
{
    private sealed class SampleTeacherResource : BaseEntity, ITeacherOwnedEntity
    {
        public string TeacherId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

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
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<ITeacherAccountService, TeacherAccountService>();

        return services.BuildServiceProvider();
    }

    private static CurrentTeacherService CreateCurrentTeacherService(string? authenticatedTeacherId)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(authenticatedTeacherId))
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, authenticatedTeacherId),
                    new Claim(ClaimTypes.Name, "ogretmen@odys.local")
                },
                authenticationType: "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return new CurrentTeacherService(accessor);
    }

    [Fact]
    public async Task ValidateTeacherCredentialsAsync_WithValidCredentials_SucceedsAndReturnsUser()
    {
        using var provider = CreateServiceProvider(nameof(ValidateTeacherCredentialsAsync_WithValidCredentials_SucceedsAndReturnsUser));
        var service = provider.GetRequiredService<ITeacherAccountService>();

        await service.RegisterTeacherAsync(new RegisterViewModel
        {
            FirstName = "Deniz",
            LastName = "Acar",
            Email = "deniz@odys.local",
            SelectedSubjects = new List<string> { "Matematik" },
            Password = "GuvenliSifre123",
            ConfirmPassword = "GuvenliSifre123"
        });

        var loginResult = await service.ValidateTeacherCredentialsAsync(new LoginViewModel
        {
            Email = "deniz@odys.local",
            Password = "GuvenliSifre123"
        });

        Assert.True(loginResult.Succeeded);
        Assert.NotNull(loginResult.User);
        Assert.False(loginResult.IsLockedOut);
        Assert.Null(loginResult.ErrorMessage);
    }

    [Fact]
    public async Task ValidateTeacherCredentialsAsync_WithInvalidPasswordOrUnknownEmail_ReturnsGenericSafeTurkishError()
    {
        using var provider = CreateServiceProvider(nameof(ValidateTeacherCredentialsAsync_WithInvalidPasswordOrUnknownEmail_ReturnsGenericSafeTurkishError));
        var service = provider.GetRequiredService<ITeacherAccountService>();

        await service.RegisterTeacherAsync(new RegisterViewModel
        {
            FirstName = "Selin",
            LastName = "Koç",
            Email = "selin@odys.local",
            SelectedSubjects = new List<string> { "İngilizce" },
            Password = "GuvenliSifre123",
            ConfirmPassword = "GuvenliSifre123"
        });

        var wrongPassword = await service.ValidateTeacherCredentialsAsync(new LoginViewModel
        {
            Email = "selin@odys.local",
            Password = "YanlisSifre999"
        });

        var unknownEmail = await service.ValidateTeacherCredentialsAsync(new LoginViewModel
        {
            Email = "bilinmeyen@odys.local",
            Password = "GuvenliSifre123"
        });

        Assert.False(wrongPassword.Succeeded);
        Assert.Equal("E-posta adresi veya şifre hatalı.", wrongPassword.ErrorMessage);
        Assert.False(unknownEmail.Succeeded);
        Assert.Equal("E-posta adresi veya şifre hatalı.", unknownEmail.ErrorMessage);
    }

    [Fact]
    public void EnsureOwnership_RejectsAccessToAnotherTeachersEntity()
    {
        var currentTeacherService = CreateCurrentTeacherService("teacher-1");

        var ownResource = new SampleTeacherResource { Id = 1, TeacherId = "teacher-1", Title = "Kendi Öğrencim" };
        var otherTeachersResource = new SampleTeacherResource { Id = 2, TeacherId = "teacher-2", Title = "Başka Öğretmenin Öğrencisi" };

        Assert.True(currentTeacherService.IsOwner(ownResource));
        currentTeacherService.EnsureOwnership(ownResource);

        Assert.False(currentTeacherService.IsOwner(otherTeachersResource));
        var ex = Assert.Throws<UnauthorizedAccessException>(() =>
            currentTeacherService.EnsureOwnership(otherTeachersResource));
        Assert.Contains("Başka bir öğretmene ait veriye erişim yetkiniz bulunmamaktadır", ex.Message);
    }

    [Fact]
    public void FilterByCurrentTeacher_ReturnsOnlyAuthenticatedTeachersRecords()
    {
        var currentTeacherService = CreateCurrentTeacherService("teacher-1");

        var allRecords = new List<SampleTeacherResource>
        {
            new() { Id = 1, TeacherId = "teacher-1", Title = "Matematik Dersi" },
            new() { Id = 2, TeacherId = "teacher-2", Title = "Fizik Dersi" },
            new() { Id = 3, TeacherId = "teacher-1", Title = "Geometri Dersi" }
        }.AsQueryable();

        var filtered = currentTeacherService.FilterByCurrentTeacher(allRecords).ToList();

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, item => Assert.Equal("teacher-1", item.TeacherId));
    }

    [Fact]
    public async Task SubjectOwnership_IsolatesSubjectsBetweenTeachersAndRejectsCrossTeacherAccess()
    {
        using var provider = CreateServiceProvider(nameof(SubjectOwnership_IsolatesSubjectsBetweenTeachersAndRejectsCrossTeacherAccess));
        var service = provider.GetRequiredService<ITeacherAccountService>();
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();

        var teacher1 = await service.RegisterTeacherAsync(new RegisterViewModel
        {
            FirstName = "Ayşe",
            LastName = "Demir",
            Email = "ayse@odys.local",
            SelectedSubjects = new List<string> { "Matematik", "Geometri" },
            Password = "GuvenliSifre123",
            ConfirmPassword = "GuvenliSifre123"
        });

        var teacher2 = await service.RegisterTeacherAsync(new RegisterViewModel
        {
            FirstName = "Mehmet",
            LastName = "Kaya",
            Email = "mehmet@odys.local",
            SelectedSubjects = new List<string> { "Fizik" },
            Password = "GuvenliSifre123",
            ConfirmPassword = "GuvenliSifre123"
        });

        Assert.NotNull(teacher1.User);
        Assert.NotNull(teacher2.User);

        var teacher1Guard = CreateCurrentTeacherService(teacher1.User.Id);

        var teacher1Subjects = await teacher1Guard
            .FilterByCurrentTeacher(dbContext.Subjects)
            .ToListAsync();

        Assert.Equal(2, teacher1Subjects.Count);
        Assert.All(teacher1Subjects, s => Assert.Equal(teacher1.User.Id, s.TeacherId));

        var teacher2Subject = await dbContext.Subjects.SingleAsync(s => s.TeacherId == teacher2.User.Id);
        Assert.Throws<UnauthorizedAccessException>(() =>
            teacher1Guard.EnsureOwnership(teacher2Subject));
    }
}
