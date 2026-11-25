using OzelDers.Web.Domain.Common;

namespace OzelDers.Web.Domain.Entities;

/// <summary>
/// Öğretmene ait öğrenci varlık modeli (FR-007, FR-010..FR-021, DEC-13, NFR-007).
/// Öğrenciler için kullanıcı hesabı yoktur; her öğrenci bir öğretmene (TeacherId) bağlıdır.
/// </summary>
public class Student : BaseEntity, ITeacherOwnedEntity
{
    public string TeacherId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public bool IsArchived { get; set; }

    public string? Notes { get; set; }

    public ApplicationUser? Teacher { get; set; }

    public ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
