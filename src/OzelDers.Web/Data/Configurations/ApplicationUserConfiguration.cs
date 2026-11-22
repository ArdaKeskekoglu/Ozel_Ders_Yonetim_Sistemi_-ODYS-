using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OzelDers.Web.Domain.Entities;

namespace OzelDers.Web.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("Turkey Standard Time");

        builder.Property(u => u.CurrencyCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("TRY");
    }
}
