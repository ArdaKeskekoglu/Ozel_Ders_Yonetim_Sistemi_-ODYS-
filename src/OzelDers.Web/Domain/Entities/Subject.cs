using OzelDers.Web.Domain.Common;

namespace OzelDers.Web.Domain.Entities;

/// <summary>
/// Öğretmenin tanımladığı ders türü varlık modeli (FR-001, FR-022..FR-024, BR-16, DEC-14, NFR-007).
/// Her öğretmenin en az bir aktif dersi varken tam olarak bir aktif varsayılan dersi (IsDefault = true) bulunur.
/// </summary>
public class Subject : BaseEntity, ITeacherOwnedEntity
{
    public string TeacherId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsArchived { get; set; }

    public ApplicationUser? Teacher { get; set; }
}
