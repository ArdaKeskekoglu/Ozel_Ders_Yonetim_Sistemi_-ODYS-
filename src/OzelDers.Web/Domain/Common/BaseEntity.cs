namespace OzelDers.Web.Domain.Common;

/// <summary>
/// Tüm domain varlıkları (Entity) için ortak kimlik ve zaman damgası alanlarını tanımlar.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
