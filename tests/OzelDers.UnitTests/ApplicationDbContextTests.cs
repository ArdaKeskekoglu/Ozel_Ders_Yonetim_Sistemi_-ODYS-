using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Common;
using OzelDers.Web.Domain.Entities;

namespace OzelDers.UnitTests;

public class ApplicationDbContextTests
{
    private sealed class SampleAuditEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestAuditDbContext : ApplicationDbContext
    {
        public TestAuditDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SampleAuditEntity> SampleEntities => Set<SampleAuditEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SampleAuditEntity>();
        }
    }

    [Fact]
    public void ApplicationDbContext_CanBeConstructedWithInMemoryOptions()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("OdysPhase4TestDb")
            .Options;

        using var context = new ApplicationDbContext(options);

        Assert.NotNull(context.Model);
    }

    [Fact]
    public async Task SaveChangesAsync_PopulatesCreatedAtUtcAndUpdatedAtUtcOnBaseEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("OdysPhase4AuditTestDb")
            .Options;

        await using var context = new TestAuditDbContext(options);
        var entity = new SampleAuditEntity { Name = "Matematik", CreatedAtUtc = default };

        context.SampleEntities.Add(entity);
        await context.SaveChangesAsync();

        Assert.NotEqual(default, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);

        entity.Name = "İleri Matematik";
        await context.SaveChangesAsync();

        Assert.NotNull(entity.UpdatedAtUtc);
    }

    [Fact]
    public void Migrations_ContainsInitialCreateMigration()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=OzelDersDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True")
            .Options;

        using var context = new ApplicationDbContext(options);
        var migrations = context.Database.GetMigrations().ToList();

        Assert.Contains(migrations, m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Contains(migrations, m => m.EndsWith("_AddSubjectAndDefaultSubjectInfrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SqlServerDatabase_CanConnectAndVerifyTeacherRegistrationWithDefaultSubjectInRolledBackTransaction()
    {
        const string connectionString =
            "Server=localhost\\SQLEXPRESS;Database=OzelDersDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentityCore<OzelDers.Web.Domain.Entities.ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        using var provider = services.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<OzelDers.Web.Domain.Entities.ApplicationUser>>();

        Assert.True(await dbContext.Database.CanConnectAsync());

        // Mevcut verilere zarar vermemek için tüm canlı SQL Server doğrulamasını dış transaction içinde çalıştırıp geri alıyoruz
        await using var tx = await dbContext.Database.BeginTransactionAsync();

        var testEmail = $"sqlverify.{Guid.NewGuid():N}@odys.local";
        var teacher = new OzelDers.Web.Domain.Entities.ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            FirstName = "SqlTest",
            LastName = "Öğretmen",
            DisplayName = "SqlTest Öğretmen"
        };

        var createUserResult = await userManager.CreateAsync(teacher, "GuvenliSifre123");
        Assert.True(createUserResult.Succeeded);

        var subjects = new[]
        {
            new OzelDers.Web.Domain.Entities.Subject
            {
                TeacherId = teacher.Id,
                Name = "Matematik",
                IsDefault = true,
                IsArchived = false
            },
            new OzelDers.Web.Domain.Entities.Subject
            {
                TeacherId = teacher.Id,
                Name = "Geometri",
                IsDefault = false,
                IsArchived = false
            }
        };

        dbContext.Subjects.AddRange(subjects);
        await dbContext.SaveChangesAsync();

        var persistedSubjects = await dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.TeacherId == teacher.Id)
            .OrderBy(s => s.Id)
            .ToListAsync();

        Assert.Equal(2, persistedSubjects.Count);
        var defaultSubject = Assert.Single(persistedSubjects, s => s.IsDefault);
        Assert.Equal("Matematik", defaultSubject.Name);

        // Filtered unique index [IX_Subjects_TeacherId_IsDefault] doğrulaması:
        // Aynı öğretmene ikinci bir aktif varsayılan ders eklenmeye çalışıldığında SQL Server reddetmeli
        dbContext.Subjects.Add(new OzelDers.Web.Domain.Entities.Subject
        {
            TeacherId = teacher.Id,
            Name = "Fizik",
            IsDefault = true,
            IsArchived = false
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

        await tx.RollbackAsync();
    }
}
