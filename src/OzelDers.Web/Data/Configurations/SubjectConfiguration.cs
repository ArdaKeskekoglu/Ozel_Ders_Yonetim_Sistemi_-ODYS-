using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OzelDers.Web.Domain.Entities;

namespace OzelDers.Web.Data.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TeacherId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(s => s.Teacher)
            .WithMany()
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Aynı öğretmende aynı ders adı tekrar edemez (arşivlenmiş ders yeniden eklendiğinde mevcut kayıt aktifleştirilir)
        builder.HasIndex(s => new { s.TeacherId, s.Name })
            .IsUnique()
            .HasDatabaseName("IX_Subjects_TeacherId_Name");

        // Öğretmen başına en fazla bir aktif varsayılan ders olabilir
        builder.HasIndex(s => s.TeacherId)
            .IsUnique()
            .HasFilter("[IsDefault] = 1 AND [IsArchived] = 0")
            .HasDatabaseName("IX_Subjects_TeacherId_IsDefault");

        builder.HasIndex(s => new { s.TeacherId, s.IsArchived })
            .HasDatabaseName("IX_Subjects_TeacherId_IsArchived");
    }
}
