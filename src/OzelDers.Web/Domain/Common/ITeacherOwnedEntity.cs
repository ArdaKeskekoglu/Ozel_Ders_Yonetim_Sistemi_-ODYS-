namespace OzelDers.Web.Domain.Common;

/// <summary>
/// Bir öğretmene ait olan tüm domain varlıkları (Öğrenci, Ders Türü, Ders, Ödeme vb.)
/// için zorunlu sahiplik sözleşmesi (FR-007, FR-008, NFR-007).
/// </summary>
public interface ITeacherOwnedEntity
{
    string TeacherId { get; set; }
}
