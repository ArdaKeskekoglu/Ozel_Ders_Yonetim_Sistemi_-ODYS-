using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OzelDers.Web.Domain.Entities;

namespace OzelDers.Web.Data.Configurations;

public class StudentSubjectConfiguration : IEntityTypeConfiguration<StudentSubject>
{
    public void Configure(EntityTypeBuilder<StudentSubject> builder)
    {
        builder.ToTable("StudentSubjects");

        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.HourlyRate)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(ss => ss.DefaultDurationMinutes)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(ss => ss.WeeklyTargetCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(ss => ss.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(ss => ss.Student)
            .WithMany(s => s.StudentSubjects)
            .HasForeignKey(ss => ss.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ss => ss.Subject)
            .WithMany(s => s.StudentSubjects)
            .HasForeignKey(ss => ss.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bir öğrenciye aynı ders türü yalnızca bir kez bağlanabilir (pasif kayıt yeniden atanırsa aktifleştirilir)
        builder.HasIndex(ss => new { ss.StudentId, ss.SubjectId })
            .IsUnique()
            .HasDatabaseName("IX_StudentSubjects_StudentId_SubjectId");

        builder.HasIndex(ss => new { ss.StudentId, ss.IsActive })
            .HasDatabaseName("IX_StudentSubjects_StudentId_IsActive");
    }
}
