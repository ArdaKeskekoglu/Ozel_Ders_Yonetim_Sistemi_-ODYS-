using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Domain.Common;
using OzelDers.Web.Domain.Entities;

namespace OzelDers.Web.Data;

/// <summary>
/// Özel Ders Yönetim Sistemi (ÖDYS) ana Entity Framework Core ve ASP.NET Core Identity veritabanı bağlamı.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = utcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }

        foreach (var userEntry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (userEntry.State == EntityState.Added)
            {
                if (userEntry.Entity.CreatedAtUtc == default)
                {
                    userEntry.Entity.CreatedAtUtc = utcNow;
                }

                if (string.IsNullOrWhiteSpace(userEntry.Entity.DisplayName))
                {
                    userEntry.Entity.DisplayName = userEntry.Entity.FullName;
                }
            }
            else if (userEntry.State == EntityState.Modified)
            {
                userEntry.Entity.UpdatedAtUtc = utcNow;
            }
        }
    }
}
