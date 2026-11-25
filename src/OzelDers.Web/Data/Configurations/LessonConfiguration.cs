using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Domain.Enums;

namespace OzelDers.Web.Data.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.StartUtc)
            .IsRequired();

        builder.Property(l => l.EndUtc)
            .IsRequired();

        builder.Property(l => l.DurationMinutes)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(l => l.HourlyRateSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(l => l.SessionFeeSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(l => l.Status)
            .IsRequired()
            .HasDefaultValue(LessonStatus.Scheduled);

        builder.Property(l => l.Notes)
            .HasMaxLength(1000);

        builder.HasOne(l => l.StudentSubject)
            .WithMany(ss => ss.Lessons)
            .HasForeignKey(l => l.StudentSubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Öğrenci–ders türü bazında kronolojik ders sorguları için indeks
        builder.HasIndex(l => new { l.StudentSubjectId, l.StartUtc })
            .HasDatabaseName("IX_Lessons_StudentSubjectId_StartUtc");

        // Haftalık takvim aralık sorguları ve çakışma kontrolleri için indeks
        builder.HasIndex(l => new { l.StartUtc, l.EndUtc })
            .HasDatabaseName("IX_Lessons_StartUtc_EndUtc");

        // Durum bazlı filtreleme (örn. yoklama doğrulaması bekleyen Scheduled dersler) için indeks
        builder.HasIndex(l => l.Status)
            .HasDatabaseName("IX_Lessons_Status");
    }
}
