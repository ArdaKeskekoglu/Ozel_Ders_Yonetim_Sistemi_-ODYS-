namespace OzelDers.Web.ViewModels.Subjects;

/// <summary>
/// Ders türleri listesi satır görünüm modeli (FR-022..FR-024, BR-16).
/// </summary>
public class SubjectListItemViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public bool IsDefault { get; init; }
    public bool IsArchived { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
