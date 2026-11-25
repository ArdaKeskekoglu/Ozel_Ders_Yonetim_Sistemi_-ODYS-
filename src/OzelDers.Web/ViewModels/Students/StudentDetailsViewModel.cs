using System.ComponentModel.DataAnnotations;
using OzelDers.Web.ViewModels.StudentSubjects;

namespace OzelDers.Web.ViewModels.Students;

/// <summary>
/// Öğrenci detay sayfasında özel notları hızlıca güncellemek için kullanılan form modeli (FR-021).
/// </summary>
public class UpdateStudentNotesViewModel
{
    public int StudentId { get; set; }

    [MaxLength(2000, ErrorMessage = "Özel notlar en fazla 2000 karakter olabilir.")]
    [Display(Name = "Öğretmen Özel Notları")]
    public string? Notes { get; set; }
}

/// <summary>
/// Öğrenci detay sayfası görünüm modeli (FR-018..FR-021, FR-025..FR-029).
/// Öğrencinin kimlik/iletişim bilgilerini, özel notlarını, aldığı ders türlerini,
/// saatlik ücretlerini ve haftalık ders hedeflerini bir arada taşır.
/// </summary>
public class StudentDetailsViewModel
{
    public int Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string? PhoneNumber { get; init; }

    public string? Email { get; init; }

    public bool IsArchived { get; init; }

    public string? Notes { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public string? TeacherDefaultSubjectName { get; init; }

    public int ActiveSubjectCount { get; init; }

    public int InactiveSubjectCount { get; init; }

    public int TotalWeeklySessions { get; init; }

    public int TotalWeeklyMinutes { get; init; }

    public decimal TotalEstimatedWeeklyFee { get; init; }

    public IReadOnlyList<StudentSubjectListItemViewModel> AssignedSubjects { get; init; } =
        Array.Empty<StudentSubjectListItemViewModel>();

    public bool HasAnySubjects => AssignedSubjects.Count > 0;
}
