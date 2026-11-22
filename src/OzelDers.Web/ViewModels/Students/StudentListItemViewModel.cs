namespace OzelDers.Web.ViewModels.Students;

/// <summary>
/// Öğrenci listesi satır/kart görünüm modeli (FR-013, FR-015).
/// </summary>
public class StudentListItemViewModel
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
}
