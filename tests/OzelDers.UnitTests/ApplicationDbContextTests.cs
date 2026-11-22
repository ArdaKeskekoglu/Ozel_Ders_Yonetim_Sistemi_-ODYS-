using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Common;

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
    }
}
